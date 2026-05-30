using Riptide;

namespace Pixelis.Server;

internal sealed class RelayServer
{
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
    private const ushort PingRequestMessageId = 12;
    private const ushort PingResponseMessageId = 13;
    private const ushort DirectedPingProbeMessageId = 14;
    private const ushort DirectedPingAckMessageId = 15;
    private const ushort DirectedPingResultMessageId = 16;
    private const ushort DirectedPingRequestMessageId = 17;
    private const ushort UsernameRejectedMessageId = 18;
    private const ushort UsernameAcceptedMessageId = 19;
    private const ushort RelayCreateRoomMessageId = 100;
    private const ushort RelayJoinRoomMessageId = 101;
    private const ushort RelayRoomCreatedMessageId = 102;
    private const ushort RelayRoomJoinRejectedMessageId = 103;

    private readonly Riptide.Server _server = new();
    private readonly Dictionary<string, RelayRoom> _rooms = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<ushort, RelayRoom> _roomsByClientId = new();
    private readonly Random _random = new();

    public void Start(ushort port, ushort maxClients)
    {
        _server.ClientDisconnected += OnClientDisconnected;
        _server.MessageReceived += OnMessageReceived;
        _server.Start(port, maxClients);
    }

    public void Update()
    {
        _server.Update();
    }

    public void Stop()
    {
        _server.Stop();
    }

    private void OnMessageReceived(object? sender, MessageReceivedEventArgs args)
    {
        switch (args.MessageId)
        {
            case RelayCreateRoomMessageId:
                HandleCreateRoom(args.Message, args.FromConnection.Id);
                break;
            case RelayJoinRoomMessageId:
                HandleJoinRoom(args.Message, args.FromConnection.Id);
                break;
            default:
                HandleRoomMessage(args.MessageId, args.Message, args.FromConnection.Id);
                break;
        }
    }

    private void HandleCreateRoom(Message message, ushort clientId)
    {
        ushort slots = message.GetUShort();
        if (slots < 2)
        {
            slots = 2;
        }
        string levelName = message.GetString();
        string levelPayload = message.GetString();
        string hostUsername = NormalizeUsername(message.GetString());
        string roomCode = GenerateRoomCode();

        RelayRoom room = new(roomCode, clientId, slots, levelName, levelPayload);
        _rooms[roomCode] = room;
        _roomsByClientId[clientId] = room;

        SendRoomCreated(clientId, roomCode);
        SendInitialConnection(room, clientId);

        Console.WriteLine($"[RELAY] Room {roomCode} created by client {clientId} ({hostUsername}), slots {slots}, level {levelName}");
    }

    private void HandleJoinRoom(Message message, ushort clientId)
    {
        string roomCode = message.GetString().Trim().ToUpperInvariant();
        string username = NormalizeUsername(message.GetString());

        if (!_rooms.TryGetValue(roomCode, out RelayRoom? room))
        {
            RejectJoin(clientId, "Room nicht gefunden.");
            return;
        }

        if (room.ClientIds.Count >= room.Slots)
        {
            RejectJoin(clientId, "Room ist voll.");
            return;
        }

        _roomsByClientId[clientId] = room;
        SendInitialConnection(room, clientId);

        Console.WriteLine($"[RELAY] Client {clientId} ({username}) joined room {roomCode}");
    }

    private void HandleRoomMessage(ushort messageId, Message message, ushort fromClientId)
    {
        if (!_roomsByClientId.TryGetValue(fromClientId, out RelayRoom? room))
        {
            return;
        }

        switch (messageId)
        {
            case UsernameMessageId:
                HandleUsername(room, message, fromClientId);
                break;
            case PositionUpdateMessageId:
                RelayPosition(room, message, fromClientId);
                break;
            case ClientDisconnectMessageId:
                RemoveClientFromRoom(fromClientId, explicitDisconnect: true);
                break;
            case LevelCompletionMessageId:
                HandleLevelCompletion(room, message);
                break;
            case ChatMessageId:
                HandleChat(room, message, fromClientId);
                break;
            case ClearChatMessageId:
                if (room.HostClientId == fromClientId)
                {
                    Broadcast(room, Message.Create(MessageSendMode.Reliable, ClearChatMessageId));
                }
                break;
            case PlayerDeathMessageId:
                BroadcastSystemChat(room, $"{GetUsername(room, fromClientId)} ist gestorben.");
                break;
            case PingRequestMessageId:
                HandlePingRequest(message, fromClientId);
                break;
            case DirectedPingAckMessageId:
                HandleDirectedPingAck(message, fromClientId);
                break;
            case DirectedPingRequestMessageId:
                HandleDirectedPingRequest(room, message, fromClientId);
                break;
        }
    }

    private void SendInitialConnection(RelayRoom room, ushort clientId)
    {
        Message initial = Message.Create(MessageSendMode.Reliable, InitialConnectionMessageId);
        initial.AddString(room.CurrentLevel);
        initial.AddString(room.CurrentLevelPayload);
        initial.AddUShort(clientId);

        List<ushort> existingPlayerIds = room.PlayerUsernames.Keys
            .Where(id => id != clientId)
            .OrderBy(id => id)
            .ToList();

        initial.AddInt(existingPlayerIds.Count);
        foreach (ushort playerId in existingPlayerIds)
        {
            initial.AddUShort(playerId);
            initial.AddString(room.PlayerUsernames[playerId]);
        }

        _server.Send(initial, clientId);
    }

    private void HandleUsername(RelayRoom room, Message message, ushort clientId)
    {
        string username = NormalizeUsername(message.GetString());
        if (room.PlayerUsernames.Any(kvp => kvp.Key != clientId && string.Equals(kvp.Value, username, StringComparison.OrdinalIgnoreCase)))
        {
            Message rejected = Message.Create(MessageSendMode.Reliable, UsernameRejectedMessageId);
            rejected.AddString($"Name '{username}' ist bereits vergeben.");
            _server.Send(rejected, clientId);
            _server.DisconnectClient(clientId);
            return;
        }

        room.ClientIds.Add(clientId);
        room.PlayerUsernames[clientId] = username;

        _server.Send(Message.Create(MessageSendMode.Reliable, UsernameAcceptedMessageId), clientId);

        Message spawn = Message.Create(MessageSendMode.Reliable, SpawnPlayerMessageId);
        spawn.AddUShort(clientId);
        spawn.AddString(username);
        Broadcast(room, spawn, exceptClientId: clientId);

        BroadcastSystemChat(room, $"{username} ist dem Server beigetreten.");
    }

    private void RelayPosition(RelayRoom room, Message message, ushort fromClientId)
    {
        if (!room.PlayerUsernames.ContainsKey(fromClientId))
        {
            return;
        }

        ushort playerId = message.GetUShort();
        float x = message.GetFloat();
        float y = message.GetFloat();
        float z = message.GetFloat();
        int poseType = message.GetInt();

        Message broadcast = Message.Create(MessageSendMode.Unreliable, PositionUpdateMessageId);
        broadcast.AddUShort(playerId);
        broadcast.AddFloat(x);
        broadcast.AddFloat(y);
        broadcast.AddFloat(z);
        broadcast.AddInt(poseType);
        Broadcast(room, broadcast, exceptClientId: fromClientId);
    }

    private void HandleLevelCompletion(RelayRoom room, Message message)
    {
        room.CurrentLevel = message.GetString();
        room.CurrentLevelPayload = string.Empty;

        Message transition = Message.Create(MessageSendMode.Reliable, LevelTransitionMessageId);
        transition.AddString(room.CurrentLevel);
        transition.AddString(room.CurrentLevelPayload);
        Broadcast(room, transition);
    }

    private void HandleChat(RelayRoom room, Message message, ushort fromClientId)
    {
        string text = message.GetString();
        Message broadcast = Message.Create(MessageSendMode.Reliable, ChatMessageId);
        broadcast.AddString($"{GetUsername(room, fromClientId)}: {text}");
        Broadcast(room, broadcast);
    }

    private void HandlePingRequest(Message message, ushort fromClientId)
    {
        int token = message.GetInt();
        Message response = Message.Create(MessageSendMode.Reliable, PingResponseMessageId);
        response.AddInt(token);
        _server.Send(response, fromClientId);
    }

    private void HandleDirectedPingAck(Message message, ushort fromClientId)
    {
        ushort requesterId = message.GetUShort();
        int token = message.GetInt();
        string targetName = _roomsByClientId.TryGetValue(fromClientId, out RelayRoom? room)
            ? GetUsername(room, fromClientId)
            : $"Player {fromClientId}";

        Message result = Message.Create(MessageSendMode.Reliable, DirectedPingResultMessageId);
        result.AddInt(token);
        result.AddString(targetName);
        _server.Send(result, requesterId);
    }

    private void HandleDirectedPingRequest(RelayRoom room, Message message, ushort requesterId)
    {
        ushort targetClientId = message.GetUShort();
        int token = message.GetInt();

        if (!room.ClientIds.Contains(targetClientId))
        {
            return;
        }

        Message probe = Message.Create(MessageSendMode.Reliable, DirectedPingProbeMessageId);
        probe.AddUShort(requesterId);
        probe.AddInt(token);
        _server.Send(probe, targetClientId);
    }

    private void OnClientDisconnected(object? sender, ServerDisconnectedEventArgs args)
    {
        RemoveClientFromRoom(args.Client.Id, explicitDisconnect: false);
    }

    private void RemoveClientFromRoom(ushort clientId, bool explicitDisconnect)
    {
        if (!_roomsByClientId.TryGetValue(clientId, out RelayRoom? room))
        {
            return;
        }

        _roomsByClientId.Remove(clientId);
        room.ClientIds.Remove(clientId);

        bool hadUsername = room.PlayerUsernames.Remove(clientId, out string? username);
        if (hadUsername)
        {
            BroadcastSystemChat(room, $"{username} hat den Server verlassen.");

            Message despawn = Message.Create(MessageSendMode.Reliable, DespawnPlayerMessageId);
            despawn.AddUShort(clientId);
            Broadcast(room, despawn);
        }

        if (room.HostClientId == clientId || room.ClientIds.Count == 0)
        {
            foreach (ushort remainingClientId in room.ClientIds.ToList())
            {
                _roomsByClientId.Remove(remainingClientId);
                _server.DisconnectClient(remainingClientId);
            }

            _rooms.Remove(room.Code);
            Console.WriteLine($"[RELAY] Room {room.Code} closed.");
            return;
        }

        if (explicitDisconnect)
        {
            _server.DisconnectClient(clientId);
        }
    }

    private void BroadcastSystemChat(RelayRoom room, string text)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ChatMessageId);
        message.AddString(text);
        Broadcast(room, message);
    }

    private void Broadcast(RelayRoom room, Message message, ushort? exceptClientId = null)
    {
        foreach (ushort clientId in room.ClientIds)
        {
            if (exceptClientId.HasValue && clientId == exceptClientId.Value)
            {
                continue;
            }

            _server.Send(message, clientId);
        }
    }

    private void SendRoomCreated(ushort clientId, string roomCode)
    {
        Message message = Message.Create(MessageSendMode.Reliable, RelayRoomCreatedMessageId);
        message.AddString(roomCode);
        _server.Send(message, clientId);
    }

    private void RejectJoin(ushort clientId, string reason)
    {
        Message rejected = Message.Create(MessageSendMode.Reliable, RelayRoomJoinRejectedMessageId);
        rejected.AddString(reason);
        _server.Send(rejected, clientId);
        _server.DisconnectClient(clientId);
    }

    private string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        for (int attempt = 0; attempt < 100; attempt++)
        {
            string code = new(Enumerable.Range(0, 6)
                .Select(_ => chars[_random.Next(chars.Length)])
                .ToArray());

            if (!_rooms.ContainsKey(code))
            {
                return code;
            }
        }

        throw new InvalidOperationException("Could not generate unique room code.");
    }

    private static string NormalizeUsername(string username)
    {
        username = username.Trim();
        return string.IsNullOrWhiteSpace(username) ? "Player" : username;
    }

    private static string GetUsername(RelayRoom room, ushort clientId)
    {
        return room.PlayerUsernames.TryGetValue(clientId, out string? username) ? username : $"Player {clientId}";
    }

    private sealed class RelayRoom
    {
        public RelayRoom(string code, ushort hostClientId, ushort slots, string currentLevel, string currentLevelPayload)
        {
            Code = code;
            HostClientId = hostClientId;
            Slots = slots;
            CurrentLevel = currentLevel;
            CurrentLevelPayload = currentLevelPayload;
        }

        public string Code { get; }
        public ushort HostClientId { get; }
        public ushort Slots { get; }
        public string CurrentLevel { get; set; }
        public string CurrentLevelPayload { get; set; }
        public HashSet<ushort> ClientIds { get; } = new();
        public Dictionary<ushort, string> PlayerUsernames { get; } = new();
    }
}
