using System.Numerics;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Transformations;
using Pixelis.CSharp.Entities;
using Pixelis.CSharp.GUIs;
using Pixelis.CSharp.GUIs.Loading;
using Pixelis.CSharp.Levels;
using Pixelis.CSharp.Scenes;
using Pixelis.CSharp.Scenes.Levels;
using Riptide;
using Sparkle.CSharp.GUI;
using Sparkle.CSharp.Scenes;
using AsyncOperation = Sparkle.CSharp.Utils.Async.AsyncOperation;

namespace Pixelis.CSharp;

public static class NetworkManager
{
    public static Server? Server;
    public static Client? Client;
    
    // Track which player ID belongs to this client
    public static ushort LocalPlayerId;
    
    // Dictionary to track all networked players by their client ID
    public static Dictionary<ushort, Player> NetworkedPlayers = new();
    
    // Dictionary to track player usernames by their client ID
    public static Dictionary<ushort, string> PlayerUsernames = new(); 
    
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
                ? "Port 7777 ist bereits belegt. Schliesse die andere Instanz zuerst."
                : "Server konnte nicht gestartet werden.";
            return false;
        }

        Server.MessageReceived += HandleServerMessageReceived;

        Logger.Info($"[SERVER] Server started on port 7777 with {slots} slots");

        Server.ClientConnected += (sender, args) =>
        {
            Logger.Info($"[SERVER] Client {args.Client.Id} connected");

            Message message = Message.Create(MessageSendMode.Reliable, 1);
            message.AddString(_currentLevel);
            message.AddString(_currentLevelPayload);
            message.AddUShort(args.Client.Id);

            List<ushort> existingPlayerIds = new List<ushort>();
            for (ushort i = 1; i < args.Client.Id; i++)
            {
                if (NetworkedPlayers.ContainsKey(i))
                {
                    existingPlayerIds.Add(i);
                }
            }

            Logger.Info($"[SERVER] Sending {existingPlayerIds.Count} existing players to client {args.Client.Id}");

            message.AddInt(existingPlayerIds.Count);
            foreach (ushort playerId in existingPlayerIds)
            {
                message.AddUShort(playerId);
                message.AddString(PlayerUsernames.ContainsKey(playerId) ? PlayerUsernames[playerId] : "Player");
            }

            Server.Send(message, args.Client);
        };

        Server.ClientDisconnected += (sender, args) =>
        {
            Logger.Info($"[SERVER] Client {args.Client.Id} disconnected - preparing despawn");

            NetworkedPlayers.Remove(args.Client.Id);
            PlayerUsernames.Remove(args.Client.Id);

            Message despawnMessage = Message.Create(MessageSendMode.Reliable, 4);
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
            case 2: // Position update
                HandleServerPositionUpdate(e.Message, e.FromConnection.Id);
                break;
            case 5: // Client disconnect request
                HandleClientDisconnectRequest(e.Message, e.FromConnection.Id);
                break;
            case 6: // Level completion
                HandleLevelCompletion(e.Message, e.FromConnection.Id);
                break;
            case 8: // Username from client
                HandleClientUsernameMessage(e.Message, e.FromConnection.Id);
                break;
            case 9: // Chat message from client
                HandleClientChatMessage(e.Message, e.FromConnection.Id);
                break;
            case 10: // Clear chat command from host
                HandleServerClearChat(e.FromConnection.Id);
                break;
        }
    }

    private static void HandleClientChatMessage(Message message, ushort fromClientId)
    {
        string chatText = message.GetString();
        string username = PlayerUsernames.TryGetValue(fromClientId, out string? name) ? name : $"Player {fromClientId}";
        string fullMessage = $"{username}: {chatText}";

        Logger.Info($"[SERVER] Chat from {fromClientId}: {chatText}");

        Message broadcastMessage = Message.Create(MessageSendMode.Reliable, 9);
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

        Message clearMessage = Message.Create(MessageSendMode.Reliable, 10);
        Server.SendToAll(clearMessage);
    }
    
    // Handle username message from client
    private static void HandleClientUsernameMessage(Message message, ushort fromClientId)
    {
        string username = message.GetString();
        
        Logger.Info($"[SERVER] Received username '{username}' from client {fromClientId}");
        
        // Store the username
        PlayerUsernames[fromClientId] = username;
        
        // Now notify all other clients about the new player with their username
        Message spawnMessage = Message.Create(MessageSendMode.Reliable, 3);
        spawnMessage.AddUShort(fromClientId);
        spawnMessage.AddString(username);
        Server.SendToAll(spawnMessage, fromClientId);
        
        Logger.Info($"[SERVER] Notified all clients about new player {fromClientId} ({username})");
    }
    
    // Handle level completion from a client
    private static void HandleLevelCompletion(Message message, ushort fromClientId)
    {
        string nextLevel = message.GetString();
        
        Logger.Info($"[SERVER] Player {fromClientId} completed level, transitioning all players to {nextLevel}");
        
        _currentLevel = nextLevel;
        _currentLevelPayload = CreateLevelPayload(nextLevel);
        
        // Remember all connected player IDs before transition
        List<ushort> connectedPlayers = new List<ushort>(NetworkedPlayers.Keys);
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
            case 1: // Initial connection
                HandleInitialConnection(e.Message);
                break;
            case 2: // Position update
                HandlePlayerPositionUpdate(e.Message);
                break;
            case 3: // Spawn player
                HandlePlayerSpawn(e.Message);
                break;
            case 4: // Despawn player
                HandlePlayerDespawn(e.Message);
                break;
            case 7: // Level transition
                HandleLevelTransition(e.Message);
                break;
            case 9: // Chat message
                HandleChatMessage(e.Message);
                break;
            case 10: // Clear chat
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
                string username = PlayerUsernames.ContainsKey(kvp.Key) ? PlayerUsernames[kvp.Key] : "Player";
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
            kvp.Value.Dispose();
        }
        NetworkedPlayers.Clear();

        AsyncOperation? operation = null;

        Scene? nextScene = CreateNetworkScene(levelName, levelPayload);
        if (nextScene != null)
        {
            operation = SceneManager.LoadSceneAsync(nextScene, new ProgressBarLoadingGui("Loading", "Joining Server!"));
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
                string localUsername = PlayerUsernames.ContainsKey(LocalPlayerId) ? PlayerUsernames[LocalPlayerId] : "Player";
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
        _onConnectionFailed?.Invoke("Unable to reach server");
        
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
            player.Dispose();
        }
        NetworkedPlayers.Clear();
        PlayerUsernames.Clear();
     
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
            Message disconnectMessage = Message.Create(MessageSendMode.Reliable, 5);
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
                Message disconnectMessage = Message.Create(MessageSendMode.Reliable, 5);
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
            player.Dispose();
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
            Message usernameMessage = Message.Create(MessageSendMode.Reliable, 8);
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
            operation = SceneManager.LoadSceneAsync(initialScene, new ProgressBarLoadingGui("Loading", "Joining Server!"));
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
            
            // Then dispose and remove from dictionary
            playerToRemove.Dispose();
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
    
    // Send player position update
    public static void SendPlayerPosition(Vector3 position, PlayerPoseType poseType)
    {
        if (Client != null && Client.IsConnected)
        {
            Message message = Message.Create(MessageSendMode.Unreliable, 2);
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
            
            Message message = Message.Create(MessageSendMode.Reliable, 6);
            message.AddString(nextLevel);
            Client.Send(message);
        }
    }

    public static void SubmitChatInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        if (string.Equals(input.Trim(), "/clear", StringComparison.OrdinalIgnoreCase))
        {
            if (Server != null && Server.IsRunning)
            {
                if (Client != null && Client.IsConnected)
                {
                    Message message = Message.Create(MessageSendMode.Reliable, 10);
                    Client.Send(message);
                }
                else
                {
                    ChatClearedReceived?.Invoke();
                }
            }
            else
            {
                ChatMessageReceived?.Invoke("Error: Only the host can execute this command.");
            }
            return;
        }

        if (Client != null && Client.IsConnected)
        {
            Message message = Message.Create(MessageSendMode.Reliable, 9);
            message.AddString(input);
            Client.Send(message);
        }
        else
        {
            string username = ResolveLocalUsername();
            ChatMessageReceived?.Invoke($"{username}: {input}");
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

        return "Local";
    }
}
