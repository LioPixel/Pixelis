using Bliss.CSharp.Logging;
using Pixelis.CSharp;
using Pixelis.Server;

ushort slots = 8;
string levelName = "Level 1";
ushort port = 7777;
ushort maxRelayClients = 256;

if (args.Length >= 1 && string.Equals(args[0], "--relay", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length >= 2 && ushort.TryParse(args[1], out ushort parsedPort))
    {
        port = parsedPort;
    }

    if (args.Length >= 3 && ushort.TryParse(args[2], out ushort parsedMaxRelayClients))
    {
        maxRelayClients = parsedMaxRelayClients;
    }

    RelayServer relayServer = new();
    relayServer.Start(port, maxRelayClients);

    Console.WriteLine($"Pixelis relay running on UDP port {port} for up to {maxRelayClients} clients.");
    Console.WriteLine("Press Ctrl+C to stop.");

    bool relayRunning = true;
    Console.CancelKeyPress += (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        relayRunning = false;
    };

    while (relayRunning)
    {
        relayServer.Update();
        Thread.Sleep(16);
    }

    relayServer.Stop();
    return 0;
}

if (args.Length >= 1 && ushort.TryParse(args[0], out ushort parsedSlots))
{
    slots = parsedSlots;
}

if (args.Length >= 2 && !string.IsNullOrWhiteSpace(args[1]))
{
    levelName = args[1].Trim();
}

if (!NetworkManager.CreateDedicatedServer(slots, levelName, out string errorMessage))
{
    Console.Error.WriteLine(errorMessage);
    return 1;
}

Logger.Info($"[DEDICATED] Pixelis server running on port 7777 with {slots} slots for level '{levelName}'");
Console.WriteLine($"Pixelis.Server running on port 7777 with {slots} slots for level '{levelName}'.");
Console.WriteLine("Press Ctrl+C to stop.");

bool keepRunning = true;
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    keepRunning = false;
};

while (keepRunning)
{
    NetworkManager.Update();
    Thread.Sleep(16);
}

NetworkManager.Cleanup();
return 0;
