using Bliss.CSharp.Logging;
using Pixelis.CSharp;

ushort slots = 8;
string levelName = "Level 1";

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
