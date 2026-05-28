using System.Numerics;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Transformations;
using Pixelis.CSharp.Entities;
using Pixelis.CSharp.GUIs;
using Pixelis.CSharp.GUIs.Loading;
using Pixelis.CSharp.Levels;
using Pixelis.CSharp.Scenes;
using Riptide;
using Sparkle.CSharp.GUI;
using Sparkle.CSharp.Scenes;
using AsyncOperation = Sparkle.CSharp.Utils.Async.AsyncOperation;

namespace Pixelis.CSharp;

public static class NetworkManager
{
    private delegate void ChatCommandHandler(string[] args);

    public static Server? Server;
    public static Client? Client;
    
    // Track which player ID belongs to this client
    public static ushort LocalPlayerId;
    
    // Dictionary to track all networked players by their client ID
    public static Dictionary<ushort, Player> NetworkedPlayers = new();
    
    // Dictionary to track player usernames by their client ID
    public static Dictionary<ushort, string> PlayerUsernames = new(); 
    private static readonly HashSet<ushort> _announcedDisconnects = new();
    
    // Flag to prevent showing HostLeavedGui during level transitions
    private static bool _isLevelTransition = false;
    
    // Public getter for level transition flag
    public static bool IsLevelTransition => _isLevelTransition;
    
    // Track current level for all clients
    private static string _currentLevel = "";
    private static string _currentLevelPayload = "";
    
    // Connection callbacks
    private static Action? _onConnectionSuccess;
    private static Action<string>? _onConnectionFailed;
    public static event Action<string>? ChatMessageReceived;
    public static event Action? ChatClearedReceived;
    private static bool _chatInputBlocked;
    private const ushort InitialConnectionMessageId = 1;
    private const ushort PositionUpdateMessageId = 2;
    private const ushort SpawnPlayerMessageId = 3;
    private const ushort DespawnPlayerMessageId = 4;
    private const ushort ClientDisconnectMessageId = 5;
    private const ushort LevelCompletionMessageId = 6;
    private const ushort LevelTransitionMessageId = 7;
    private const ushort UsernameMessageId = 8;
    private const ushort ChatMessageId = 9;
    private const ushort ClearChatMessageId = 10;
    private const ushort PlayerDeathMessageId = 11;
    private static readonly Dictionary<string, ChatCommandHandler> _chatCommands = new(StringComparer.OrdinalIgnoreCase);

    static NetworkManager()
    {
        RegisterChatCommands();
    }

    public static void Update()
    {
        // Update server and client
        Server?.Update();
        Client?.Update();
    }

    public static bool CreateDedicatedServer(ushort slots, string levelName, out string errorMessage)
    {
        errorMessage = string.Empty;
        _currentLevel = levelName;
        _currentLevelPayload = CreateLevelPayload(levelName);
        _pendingUsername = string.Empty;

        return TryStartServer(slots, out errorMessage);
    }

    public static bool CreateServer(ushort slots, string levelName, string hostUsername, out string errorMessage)
    {
        errorMessage = string.Empty;
        _currentLevel = levelName;
        _currentLevelPayload = CreateLevelPayload(levelName);
        
        // Store the host username so it can be used when the host client connects
        _pendingUsername = hostUsername;

        if (!TryStartServer(slots, out errorMessage))
        {
            return false;
        }
        
        Client = new Client();
        Client.Connected += OnClientConnected;
        Client.ConnectionFailed += OnClientConnectionFailed;
        Client.Disconnected += OnClientDisconnected;
        Client.MessageReceived += HandleClientMessageReceived;
        Client.Connect("127.0.0.1:7777");
        Logger.Info($"[CLIENT] Host connecting to own server with username: {hostUsername}");
        return true;
    }

    private static bool TryStartServer(ushort slots, out string errorMessage)
    {
        errorMessage = string.Empty;
        NetworkedPlayers.Clear();
        PlayerUsernames.Clear();
        _announcedDisconnects.Clear();

        try
        {
            Server = new Server();
            Server.Start(7777, slots);
        }
        catch (Exception ex)
        {
            Logger.Error($"[SERVER] Failed to start server: {ex.Message}");

            Client = null;
            Server = null;
            errorMessage = ex is System.Net.Sockets.SocketException
                ? Localization.T("network.error.port_in_use")
                : Localization.T("network.error.server_start_failed");
            return false;
        }

        Server.MessageReceived += HandleServerMessageReceived;

        Logger.Info($"[SERVER] Server started on port 7777 with {slots} slots");

        Server.ClientConnected += (sender, args) =>
        {
            Logger.Info($"[SERVER] Client {args.Client.Id} connected");

            Message message = Message.Create(MessageSendMode.Reliable, InitialConnectionMessageId);
            message.AddString(_currentLevel);
            message.AddString(_currentLevelPayload);
            message.AddUShort(args.Client.Id);

            List<ushort> existingPlayerIds = GetRegisteredServerPlayerIds(args.Client.Id);

            Logger.Info($"[SERVER] Sending {existingPlayerIds.Count} existing players to client {args.Client.Id}");

            message.AddInt(existingPlayerIds.Count);
            foreach (ushort playerId in existingPlayerIds)
            {
                message.AddUShort(playerId);
                message.AddString(PlayerUsernames.ContainsKey(playerId) ? PlayerUsernames[playerId] : Localization.T("network.player.default_name"));
            }

            Server.Send(message, args.Client);
        };

        Server.ClientDisconnected += (sender, args) =>
        {
            Logger.Info($"[SERVER] Client {args.Client.Id} disconnected - preparing despawn");

            BroadcastLeaveIfNeeded(args.Client.Id);

            NetworkedPlayers.Remove(args.Client.Id);
            PlayerUsernames.Remove(args.Client.Id);

            Message despawnMessage = Message.Create(MessageSendMode.Reliable, DespawnPlayerMessageId);
            despawnMessage.AddUShort(args.Client.Id);

            Server.SendToAll(despawnMessage);
            Server.Update();

            Logger.Info($"[SERVER] Sent despawn message for player {args.Client.Id} to all remaining clients");
        };

        return true;
    }
    
    // Server-side message handler
    private static void HandleServerMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        ushort messageId = e.MessageId;
        
        switch (messageId)
        {
            case PositionUpdateMessageId: // Position update
                HandleServerPositionUpdate(e.Message, e.FromConnection.Id);
                break;
            case ClientDisconnectMessageId: // Client disconnect request
                HandleClientDisconnectRequest(e.Message, e.FromConnection.Id);
                break;
            case LevelCompletionMessageId: // Level completion
                HandleLevelCompletion(e.Message, e.FromConnection.Id);
                break;
            case UsernameMessageId: // Username from client
                HandleClientUsernameMessage(e.Message, e.FromConnection.Id);
                break;
            case ChatMessageId: // Chat message from client
                HandleClientChatMessage(e.Message, e.FromConnection.Id);
                break;
            case ClearChatMessageId: // Clear chat command from host
                HandleServerClearChat(e.FromConnection.Id);
                break;
            case PlayerDeathMessageId: // Player death notification
                HandlePlayerDeathMessage(e.FromConnection.Id);
                break;
        }
    }

    private static void HandleClientChatMessage(Message message, ushort fromClientId)
    {
        string chatText = message.GetString();
        string username = PlayerUsernames.TryGetValue(fromClientId, out string? name) ? name : $"{Localization.T("network.player.default_name")} {fromClientId}";
        string fullMessage = $"{username}: {chatText}";

        Logger.Info($"[SERVER] Chat from {fromClientId}: {chatText}");

        Message broadcastMessage = Message.Create(MessageSendMode.Reliable, ChatMessageId);
        broadcastMessage.AddString(fullMessage);
        Server?.SendToAll(broadcastMessage);
    }

    private static void HandleServerClearChat(ushort fromClientId)
    {
        if (Server == null || !Server.IsRunning || fromClientId != LocalPlayerId)
        {
            return;
        }

        Logger.Info("[SERVER] Host requested chat clear");

        Message clearMessage = Message.Create(MessageSendMode.Reliable, ClearChatMessageId);
        Server.SendToAll(clearMessage);
    }
    
    // Handle username message from client
    private static void HandleClientUsernameMessage(Message message, ushort fromClientId)
    {
        string username = message.GetString();
        
        Logger.Info($"[SERVER] Received username '{username}' from client {fromClientId}");
        
        // Store the username
        PlayerUsernames[fromClientId] = username;
        _announcedDisconnects.Remove(fromClientId);
        
        // Now notify all other clients about the new player with their username
        Message spawnMessage = Message.Create(MessageSendMode.Reliable, SpawnPlayerMessageId);
        spawnMessage.AddUShort(fromClientId);
        spawnMessage.AddString(username);
        Server.SendToAll(spawnMessage, fromClientId);

        BroadcastSystemChatMessage(Localization.F("chat.system.player_joined", username));
        
        Logger.Info($"[SERVER] Notified all clients about new player {fromClientId} ({username})");
    }

    private static void HandlePlayerDeathMessage(ushort fromClientId)
    {
        string username = PlayerUsernames.TryGetValue(fromClientId, out string? name) ? name : $"{Localization.T("network.player.default_name")} {fromClientId}";
        Logger.Info($"[SERVER] Death notification from {fromClientId} ({username})");
        BroadcastSystemChatMessage(Localization.F("chat.system.player_died", username));
    }
    
    // Handle level completion from a client
    private static void HandleLevelCompletion(Message message, ushort fromClientId)
    {
        string nextLevel = message.GetString();
        
        Logger.Info($"[SERVER] Player {fromClientId} completed level, transitioning all players to {nextLevel}");
        
        _currentLevel = nextLevel;
        _currentLevelPayload = CreateLevelPayload(nextLevel);
        
        // Remember all connected player IDs before transition
        List<ushort> connectedPlayers = GetRegisteredServerPlayerIds();
        Logger.Info($"[SERVER] Current players before transition: {string.Join(", ", connectedPlayers)}");
        
        // Send level transition message to ALL clients
        Message levelTransitionMessage = Message.Create(MessageSendMode.Reliable, 7);
        levelTransitionMessage.AddString(_currentLevel);
        levelTransitionMessage.AddString(_currentLevelPayload);
        Server.SendToAll(levelTransitionMessage);
        
        // Force server update to ensure message is sent
        Server.Update();
        
        Logger.Info($"[SERVER] Sent level transition to all clients: {nextLevel}");
        Logger.Info($"[SERVER] Players should recreate: {string.Join(", ", connectedPlayers)}");
    }
    
    // Handle when a client explicitly tells us they're disconnecting
    private static void HandleClientDisconnectRequest(Message message, ushort fromClientId)
    {
        ushort playerId = message.GetUShort();
        
        Logger.Info($"[SERVER] Client {fromClientId} (Player {playerId}) requested disconnect");
        
        foreach (var valuePair in NetworkedPlayers)
        {
            if (valuePair.Key == playerId)
            {
                SceneManager.ActiveScene?.RemoveEntity(valuePair.Value);
            }
        }
        
        // Remove from server's player list
        BroadcastLeaveIfNeeded(playerId);
        NetworkedPlayers.Remove(playerId);
        PlayerUsernames.Remove(playerId);
        
        // Notify ALL OTHER clients to remove this player
        Message despawnMessage = Message.Create(MessageSendMode.Reliable, 4);
        despawnMessage.AddUShort(playerId);
        
        // Send to all clients EXCEPT the one disconnecting
        Server.SendToAll(despawnMessage, fromClientId);
        
        // Force immediate send
        Server.Update();
        
        Logger.Info($"[SERVER] Sent despawn message for player {playerId} to all other clients");
    }
    
    // Client-side message handler - routes messages to appropriate handlers
    private static void HandleClientMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        ushort messageId = e.MessageId;
        
        switch (messageId)
        {
            case InitialConnectionMessageId: // Initial connection
                HandleInitialConnection(e.Message);
                break;
            case PositionUpdateMessageId: // Position update
                HandlePlayerPositionUpdate(e.Message);
                break;
            case SpawnPlayerMessageId: // Spawn player
                HandlePlayerSpawn(e.Message);
                break;
            case DespawnPlayerMessageId: // Despawn player
                HandlePlayerDespawn(e.Message);
                break;
            case LevelTransitionMessageId: // Level transition
                HandleLevelTransition(e.Message);
                break;
            case ChatMessageId: // Chat message
                HandleChatMessage(e.Message);
                break;
            case ClearChatMessageId: // Clear chat
                HandleClearChat();
                break;
            default:
                Logger.Warn($"[CLIENT] Unknown message ID: {messageId}");
                break;
        }
    }
    
    // Handle level transition message from server
    private static void HandleLevelTransition(Message message)
    {
        string levelName = message.GetString();
        string levelPayload = message.GetString();
        
        Logger.Info($"[CLIENT] Received level transition to {levelName}");
        
        _isLevelTransition = true;
        
        // Remember all player IDs and usernames (except local)
        Dictionary<ushort, string> remotePlayersWithUsernames = new Dictionary<ushort, string>();
        foreach (var kvp in NetworkedPlayers)
        {
            if (kvp.Key != LocalPlayerId)
            {
                string username = PlayerUsernames.ContainsKey(kvp.Key) ? PlayerUsernames[kvp.Key] : Localization.T("network.player.default_name");
                remotePlayersWithUsernames[kvp.Key] = username;
            }
        }
        
        Logger.Info($"[CLIENT] Remembered {remotePlayersWithUsernames.Count} remote players for recreation");
        
        // Clear all networked players from current scene
        foreach (var kvp in NetworkedPlayers.ToList())
        {
            if (SceneManager.ActiveScene != null)
            {
                SceneManager.ActiveScene.RemoveEntity(kvp.Value);
            }
        }
        NetworkedPlayers.Clear();

        AsyncOperation? operation = null;

        Scene? nextScene = CreateNetworkScene(levelName, levelPayload);
        if (nextScene != null)
        {
            operation = SceneManager.LoadSceneAsync(nextScene, new ProgressBarLoadingGui("Loading", Localization.T("gui.loading.joining_server")));
        }
        else
        {
            Logger.Error($"[CLIENT] Could not create level scene for {levelName}");
        }

        operation?.Completed += success =>
        {
            // Recreate all players in new level
            if (SceneManager.ActiveScene != null)
            {
                // Recreate local player
                string localUsername = PlayerUsernames.ContainsKey(LocalPlayerId) ? PlayerUsernames[LocalPlayerId] : Localization.T("network.player.default_name");
                Player localPlayer = new Player(new Transform() { Translation = new Vector3(0, -16 * 2, 0) }, true, localUsername);
                SceneManager.ActiveScene.AddEntity(localPlayer);
                NetworkedPlayers[LocalPlayerId] = localPlayer;
            
                Logger.Info($"[CLIENT] Recreated local player with ID {LocalPlayerId} ({localUsername}) in new level");
            
                // Recreate all remote players that were in the previous level
                foreach (var kvp in remotePlayersWithUsernames)
                {
                    Player remotePlayer = new Player(new Transform() { Translation = new Vector3(0, -16 * 2, 0) }, false, kvp.Value);
                    SceneManager.ActiveScene.AddEntity(remotePlayer);
                    NetworkedPlayers[kvp.Key] = remotePlayer;
                
                    Logger.Info($"[CLIENT] Recreated remote player with ID {kvp.Key} ({kvp.Value}) in new level");
                }
            }
        
            _isLevelTransition = false;
        
            Logger.Info($"[CLIENT] Level transition complete. Total players: {NetworkedPlayers.Count}");
        };
    }
    
    private static void HandleServerPositionUpdate(Message message, ushort fromClientId)
    {
        ushort playerId = message.GetUShort();
        float x = message.GetFloat();
        float y = message.GetFloat();
        float z = message.GetFloat();
        int poseType = message.GetInt();
        
        //Logger.Info($"[SERVER] Received position from client {fromClientId}: Player {playerId} at ({x:F2}, {y:F2})");
        
        // Broadcast to all OTHER clients
        Message broadcastMessage = Message.Create(MessageSendMode.Unreliable, 2);
        broadcastMessage.AddUShort(playerId);
        broadcastMessage.AddFloat(x);
        broadcastMessage.AddFloat(y);
        broadcastMessage.AddFloat(z);
        broadcastMessage.AddInt(poseType);
        Server.SendToAll(broadcastMessage, fromClientId);
        
        //Logger.Info($"[SERVER] Broadcasted player {playerId} position to all clients except {fromClientId}");
    }

    public static void JoinServer(string ip, string username)
    {
        Client = new Client();
        Client.Connected += OnClientConnected;
        Client.ConnectionFailed += OnClientConnectionFailed;
        Client.Disconnected += OnClientDisconnected;
        Client.MessageReceived += HandleClientMessageReceived;
        
        // Store the username temporarily - we'll send it after connection
        _pendingUsername = username;
        
        // Make sure to use the provided IP, not hardcoded localhost
        if (!ip.Contains(":"))
        {
            ip += ":7777"; // Add default port if not specified
        }
        Client.Connect(ip);
        Logger.Info($"[CLIENT] Connecting to server at {ip}");
    }
    
    private static string _pendingUsername = "";
    
    // Set callbacks for connection success/failure (used by JoinGui)
    public static void SetConnectionCallbacks(Action onSuccess, Action<string> onFailed)
    {
        _onConnectionSuccess = onSuccess;
        _onConnectionFailed = onFailed;
    }
    
    private static void OnClientConnected(object sender, EventArgs e)
    {
        Logger.Info("[CLIENT] Successfully connected to server!");
        
        // Call success callback if set
        _onConnectionSuccess?.Invoke();
        
        // Clear callbacks after use
        _onConnectionSuccess = null;
        _onConnectionFailed = null;
    }
    
    private static void OnClientConnectionFailed(object sender, EventArgs e)
    {
        Logger.Error("[CLIENT] Failed to connect to server!");
        
        // Call failure callback if set
        _onConnectionFailed?.Invoke(Localization.T("network.error.unable_to_reach_server"));
        
        // Clear callbacks after use
        _onConnectionSuccess = null;
        _onConnectionFailed = null;
    }
    
    private static void OnClientDisconnected(object sender, DisconnectedEventArgs e)
    {
        Logger.Warn($"[CLIENT] Disconnected from server! Reason: {e.Reason}");
        
        // Don't show disconnect GUI during level transitions
        if (_isLevelTransition)
        {
            Logger.Info("[CLIENT] Ignoring disconnect during level transition");
            return;
        }
        
        // Clean up all networked players
        foreach (var player in NetworkedPlayers.Values)
        {
            SceneManager.ActiveScene?.RemoveEntity(player);
        }
        NetworkedPlayers.Clear();
        PlayerUsernames.Clear();
        _announcedDisconnects.Clear();
     
        GuiManager.SetGui(new HostLeavedGui());
    }
    
    public static void Cleanup()
    {
        Logger.Info("[NETWORK] Starting cleanup...");
        
        // If we're a client (not hosting), send disconnect message BEFORE actually disconnecting
        if (Client != null && Client.IsConnected && (Server == null || !Server.IsRunning))
        {
            Logger.Info("[NETWORK] Client sending disconnect message to server");
            
            // Send explicit disconnect message to server
            Message disconnectMessage = Message.Create(MessageSendMode.Reliable, ClientDisconnectMessageId);
            disconnectMessage.AddUShort(LocalPlayerId);
            Client.Send(disconnectMessage);
            
            // Give time for the message to be sent
            System.Threading.Thread.Sleep(200);
            
            Logger.Info("[NETWORK] Client disconnecting from server");
            Client.Disconnect();
            
            // Give the disconnect message time to be processed
            System.Threading.Thread.Sleep(200);
        }
        
        // If we're hosting, we need to handle this carefully
        if (Server != null && Server.IsRunning)
        {
            // First, send disconnect message from our own client
            if (Client != null && Client.IsConnected)
            {
                Logger.Info("[NETWORK] Host client sending disconnect message");
                
                // Send explicit disconnect message
                Message disconnectMessage = Message.Create(MessageSendMode.Reliable, ClientDisconnectMessageId);
                disconnectMessage.AddUShort(LocalPlayerId);
                Client.Send(disconnectMessage);
                
                // Give time for message to be sent
                System.Threading.Thread.Sleep(200);
                
                Logger.Info("[NETWORK] Host client disconnecting from own server");
                Client.Disconnect();
                
                // Process the disconnect on the server side
                Server.Update();
                
                // Give time for the despawn message to be sent to other clients
                System.Threading.Thread.Sleep(200);
                
                // Force one more server update to ensure all messages are sent
                Server.Update();
                System.Threading.Thread.Sleep(100);
                
                Client = null;
            }
            
            Logger.Info("[NETWORK] Stopping server - this will disconnect all remaining clients");
            Server.Stop();
            Server = null;
        }
        
        // Clean up all networked players
        foreach (var player in NetworkedPlayers.Values)
        {
            SceneManager.ActiveScene?.RemoveEntity(player);
        }
        
        NetworkedPlayers.Clear();
        PlayerUsernames.Clear();
        
        Logger.Info("[NETWORK] Full cleanup completed");
    }
    
    // Message 1: Initial connection - receive scene and player ID
    private static void HandleInitialConnection(Message message)
    {
        Logger.Info("[CLIENT] HandleInitialConnection called!");
        
        string levelName = message.GetString();
        string levelPayload = message.GetString();
        LocalPlayerId = message.GetUShort();
        
        Logger.Info($"[CLIENT] Received level: {levelName}, LocalPlayerId: {LocalPlayerId}");
        
        // Send username to server now that we have our player ID
        if (Client != null && Client.IsConnected)
        {
            Message usernameMessage = Message.Create(MessageSendMode.Reliable, UsernameMessageId);
            usernameMessage.AddString(_pendingUsername);
            Client.Send(usernameMessage);
            
            // Store our own username
            PlayerUsernames[LocalPlayerId] = _pendingUsername;
            
            Logger.Info($"[CLIENT] Sent username '{_pendingUsername}' to server");
        }
        
        int existingPlayerCount = message.GetInt();
        Dictionary<ushort, string> existingPlayersWithUsernames = new Dictionary<ushort, string>();
        for (int i = 0; i < existingPlayerCount; i++)
        {
            ushort playerId = message.GetUShort();
            string username = message.GetString();
            existingPlayersWithUsernames[playerId] = username;
            PlayerUsernames[playerId] = username;
        }
        
        Logger.Info($"[CLIENT] Existing players: {existingPlayerCount}");

        AsyncOperation? operation = null;

        Scene? initialScene = CreateNetworkScene(levelName, levelPayload);
        if (initialScene != null)
        {
            operation = SceneManager.LoadSceneAsync(initialScene, new ProgressBarLoadingGui("Loading", Localization.T("gui.loading.joining_server")));
        }
        else
        {
            Logger.Error($"[CLIENT] Could not create level scene for {levelName}");
        }

        operation?.Completed += success =>
        {
            if (SceneManager.ActiveScene != null)
            {
                Logger.Info("[CLIENT] Scene loaded, creating players...");
            
                // Create local player with username
                Player localPlayer = new Player(new Transform() { Translation = new Vector3(0, -16 * 2, 0) }, true, _pendingUsername);
                SceneManager.ActiveScene.AddEntity(localPlayer);
                NetworkedPlayers[LocalPlayerId] = localPlayer;
            
                Logger.Info($"[CLIENT] Created local player with ID {LocalPlayerId} ({_pendingUsername})");
            
                // Create existing remote players with usernames
                foreach (var kvp in existingPlayersWithUsernames)
                {
                    Player remotePlayer = new Player(new Transform() { Translation = new Vector3(0, -16 * 2, 0) }, false, kvp.Value);
                    SceneManager.ActiveScene.AddEntity(remotePlayer);
                    NetworkedPlayers[kvp.Key] = remotePlayer;
                
                    Logger.Info($"[CLIENT] Created remote player with ID {kvp.Key} ({kvp.Value})");
                }
            }
            else
            {
                Logger.Error("[CLIENT] ActiveScene is null after SetScene!");
            }
        };
    }
    
    // Message 2: Player position update (CLIENT receives broadcast from server)
    private static void HandlePlayerPositionUpdate(Message message)
    {
        ushort playerId = message.GetUShort();
        float x = message.GetFloat();
        float y = message.GetFloat();
        float z = message.GetFloat();
        int poseType = message.GetInt();
        
        // Update the player position if it's not our local player
        if (playerId != LocalPlayerId)
        {
            if (NetworkedPlayers.ContainsKey(playerId))
            {
                NetworkedPlayers[playerId].NetworkedPosition = new Vector3(x, y, z);
                NetworkedPlayers[playerId].NetworkedPoseType = (PlayerPoseType)poseType;
                //Logger.Info($"[CLIENT RECV] Updated player {playerId} to ({x:F2}, {y:F2})");
            }
            else
            {
                //Logger.Warn($"[CLIENT RECV] Player {playerId} not in dictionary! Available: {string.Join(", ", NetworkedPlayers.Keys)}");
            }
        }
        else
        {
            //Logger.Info($"[CLIENT RECV] Ignoring update for local player {playerId}");
        }
    }
    
    // Message 3: Spawn new player
    private static void HandlePlayerSpawn(Message message)
    {
        ushort playerId = message.GetUShort();
        string username = message.GetString();
        
        Logger.Info($"[SPAWN] Received spawn request for player {playerId} ({username}). LocalPlayerId: {LocalPlayerId}");
        
        // Store the username
        PlayerUsernames[playerId] = username;
        
        if (playerId != LocalPlayerId && SceneManager.ActiveScene != null && !NetworkedPlayers.ContainsKey(playerId))
        {
            Player remotePlayer = new Player(new Transform() { Translation = new Vector3(0, -16 * 2, 0) }, false, username);
            SceneManager.ActiveScene.AddEntity(remotePlayer);
            NetworkedPlayers[playerId] = remotePlayer;
            
            Logger.Info($"[SPAWN] Created remote player {playerId} ({username})");
        }
        else
        {
            Logger.Warn($"[SPAWN] Skipped player {playerId} - Already exists or is local player");
        }
    }
    
    // Message 4: Despawn player
    private static void HandlePlayerDespawn(Message message)
    {
        ushort playerId = message.GetUShort();
        
        Logger.Info($"[DESPAWN] Received despawn message for player {playerId}");
        
        if (NetworkedPlayers.ContainsKey(playerId))
        {
            Player playerToRemove = NetworkedPlayers[playerId];
            
            // Remove from scene first
            if (SceneManager.ActiveScene != null)
            {
                SceneManager.ActiveScene.RemoveEntity(playerToRemove);
                Logger.Info($"[DESPAWN] Removed player {playerId} from scene");
            }
            
            NetworkedPlayers.Remove(playerId);
            PlayerUsernames.Remove(playerId);
            
            Logger.Info($"[DESPAWN] Successfully despawned and removed player {playerId}");
        }
        else
        {
            Logger.Warn($"[DESPAWN] Player {playerId} not found in NetworkedPlayers dictionary");
        }
    }

    private static void HandleChatMessage(Message message)
    {
        string chatText = message.GetString();
        ChatMessageReceived?.Invoke(chatText);
    }

    private static void HandleClearChat()
    {
        ChatClearedReceived?.Invoke();
    }

    private static string CreateLevelPayload(string levelName)
    {
        return CustomLevelStorage.ExportLevelPayload(levelName) ?? string.Empty;
    }

    private static Scene? CreateNetworkScene(string levelName, string levelPayload)
    {
        if (!string.IsNullOrWhiteSpace(levelPayload))
        {
            CustomLevelData? levelData = CustomLevelStorage.ImportLevelPayload(levelPayload);
            if (levelData != null)
            {
                return new CustomLevelScene(levelData, false);
            }
        }

        return LevelFactory.CreateByName(levelName);
    }

    private static List<ushort> GetRegisteredServerPlayerIds(ushort? excludePlayerId = null)
    {
        IEnumerable<ushort> playerIds = PlayerUsernames.Keys;

        if (excludePlayerId.HasValue)
        {
            playerIds = playerIds.Where(id => id != excludePlayerId.Value);
        }

        return playerIds
            .OrderBy(id => id)
            .ToList();
    }
    
    // Send player position update
    public static void SendPlayerPosition(Vector3 position, PlayerPoseType poseType)
    {
        if (Client != null && Client.IsConnected)
        {
            Message message = Message.Create(MessageSendMode.Unreliable, PositionUpdateMessageId);
            message.AddUShort(LocalPlayerId);
            message.AddFloat(position.X);
            message.AddFloat(position.Y);
            message.AddFloat(position.Z);
            message.AddInt((int)poseType);
            Client.Send(message);
        }
    }
    
    // NEW: Send level completion notification to server
    public static void NotifyLevelComplete(string nextLevel)
    {
        if (Client != null && Client.IsConnected)
        {
            Logger.Info($"[CLIENT] Notifying server of level completion, next level: {nextLevel}");
            
            Message message = Message.Create(MessageSendMode.Reliable, LevelCompletionMessageId);
            message.AddString(nextLevel);
            Client.Send(message);
        }
    }

    public static void NotifyPlayerDied()
    {
        string username = ResolveLocalUsername();

        if (Client != null && Client.IsConnected)
        {
            Logger.Info($"[CLIENT] Reporting death for {username}");

            Message message = Message.Create(MessageSendMode.Reliable, PlayerDeathMessageId);
            Client.Send(message);
            return;
        }

        ChatMessageReceived?.Invoke(Localization.F("chat.system.player_died", username));
    }

    public static void SubmitChatInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        string trimmedInput = input.Trim();

        if (TryHandleChatCommand(trimmedInput))
        {
            return;
        }

        if (Client != null && Client.IsConnected)
        {
            Message message = Message.Create(MessageSendMode.Reliable, ChatMessageId);
            message.AddString(trimmedInput);
            Client.Send(message);
        }
        else
        {
            string username = ResolveLocalUsername();
            ChatMessageReceived?.Invoke($"{username}: {trimmedInput}");
        }
    }

    public static void SetChatInputBlocked(bool blocked)
    {
        _chatInputBlocked = blocked;
    }

    public static bool IsChatInputBlocked()
    {
        return _chatInputBlocked;
    }

    private static string ResolveLocalUsername()
    {
        if (PlayerUsernames.TryGetValue(LocalPlayerId, out string? username) && !string.IsNullOrWhiteSpace(username))
        {
            return username;
        }

        if (!string.IsNullOrWhiteSpace(_pendingUsername))
        {
            return _pendingUsername;
        }

        return Localization.T("network.player.local");
    }

    private static void BroadcastSystemChatMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        Message broadcastMessage = Message.Create(MessageSendMode.Reliable, ChatMessageId);
        broadcastMessage.AddString(message);
        Server?.SendToAll(broadcastMessage);
    }

    private static void BroadcastLeaveIfNeeded(ushort playerId)
    {
        if (_announcedDisconnects.Contains(playerId))
        {
            return;
        }

        string username = PlayerUsernames.TryGetValue(playerId, out string? name) ? name : $"{Localization.T("network.player.default_name")} {playerId}";
        _announcedDisconnects.Add(playerId);
        BroadcastSystemChatMessage(Localization.F("chat.system.player_left", username));
    }

    private static void RegisterChatCommands()
    {
        _chatCommands.Clear();

        // Add new chat commands here.
        RegisterChatCommand("clear", HandleClearCommand);
    }

    private static void RegisterChatCommand(string name, ChatCommandHandler handler)
    {
        _chatCommands[name] = handler;
    }

    private static bool TryHandleChatCommand(string input)
    {
        if (!input.StartsWith('/'))
        {
            return false;
        }

        string[] parts = input[1..]
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return true;
        }

        string commandName = parts[0];
        string[] args = parts.Skip(1).ToArray();

        if (!_chatCommands.TryGetValue(commandName, out ChatCommandHandler? handler))
        {
            ChatMessageReceived?.Invoke(Localization.F("chat.command.unknown", commandName));
            return true;
        }

        handler(args);
        return true;
    }

    private static void HandleClearCommand(string[] args)
    {
        if (Server != null && Server.IsRunning)
        {
            if (Client != null && Client.IsConnected)
            {
                Message message = Message.Create(MessageSendMode.Reliable, ClearChatMessageId);
                Client.Send(message);
            }
            else
            {
                ChatClearedReceived?.Invoke();
            }

            return;
        }

        ChatMessageReceived?.Invoke(Localization.T("chat.command.host_only"));
    }
}
