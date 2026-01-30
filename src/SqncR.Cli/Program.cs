using SqncR.Core;
using SqncR.Midi;

// Simple arg parsing - no external dependencies
if (args.Length == 0)
{
    ShowHelp();
    return 0;
}

var command = args[0].ToLowerInvariant();

return command switch
{
    "list-devices" => ListDevices(),
    "play" => await PlaySequence(args.Skip(1).ToArray()),
    "stop" => Stop(),
    "--help" or "-h" or "help" => ShowHelp(),
    _ => UnknownCommand(command)
};

static int ShowHelp()
{
    Console.WriteLine(@"
SqncR - AI-Native Generative Music for MIDI Devices

USAGE:
  sqncr <command> [options]

COMMANDS:
  list-devices              List available MIDI output devices
  play <file> [options]     Play a .sqnc.yaml sequence file
  stop                      Stop playback (placeholder)
  help                      Show this help

PLAY OPTIONS:
  --device, -d <n>          MIDI device index or name
  --loop, -l                Loop playback until Ctrl+C

EXAMPLES:
  sqncr list-devices
  sqncr play examples/chill-ambient.sqnc.yaml -d 0
  sqncr play my-song.sqnc.yaml --device ""Polyend Synth"" --loop
");
    return 0;
}

static int ListDevices()
{
    try
    {
        using var midi = new MidiService();
        var devices = midi.ListOutputDevices();

        if (devices.Count == 0)
        {
            Console.WriteLine("No MIDI output devices found.");
            Console.WriteLine();
            Console.WriteLine("Tips:");
            Console.WriteLine("  - Ensure your MIDI device is connected and powered on");
            Console.WriteLine("  - On Windows, install virtual MIDI (loopMIDI)");
            Console.WriteLine("  - Check that MIDI drivers are installed");
            return 1;
        }

        Console.WriteLine("MIDI Output Devices:");
        Console.WriteLine();
        foreach (var device in devices)
        {
            Console.WriteLine($"  [{device.Index}] {device.Name}");
        }
        Console.WriteLine();
        Console.WriteLine("Use --device <index> or --device \"<name>\" with play.");
        return 0;
    }
    catch (Exception ex)
    {
        WriteError(ex.Message);
        return 1;
    }
}

static async Task<int> PlaySequence(string[] args)
{
    // Parse args
    string? filePath = null;
    string? device = null;
    bool loop = false;

    for (int i = 0; i < args.Length; i++)
    {
        var arg = args[i];

        if (arg == "--device" || arg == "-d")
        {
            if (i + 1 < args.Length)
            {
                device = args[++i];
            }
        }
        else if (arg == "--loop" || arg == "-l")
        {
            loop = true;
        }
        else if (!arg.StartsWith("-"))
        {
            filePath = arg;
        }
    }

    if (string.IsNullOrEmpty(filePath))
    {
        WriteError("No file specified.");
        Console.WriteLine("Usage: sqncr play <file.sqnc.yaml> --device <n>");
        return 1;
    }

    try
    {
        // Resolve file path
        var fullPath = Path.GetFullPath(filePath);
        if (!File.Exists(fullPath))
        {
            WriteError($"File not found: {fullPath}");
            return 1;
        }

        using var midi = new MidiService();
        var devices = midi.ListOutputDevices();

        if (devices.Count == 0)
        {
            WriteError("No MIDI devices found. Connect a device and try again.");
            return 1;
        }

        // Device selection
        if (string.IsNullOrEmpty(device))
        {
            Console.WriteLine("No device specified. Available devices:");
            Console.WriteLine();
            foreach (var d in devices)
            {
                Console.WriteLine($"  [{d.Index}] {d.Name}");
            }
            Console.WriteLine();
            Console.WriteLine("Use --device <index> or --device \"<name>\"");
            return 1;
        }

        // Open device
        if (int.TryParse(device, out var deviceIndex))
            midi.OpenDevice(deviceIndex);
        else
            midi.OpenDevice(device);

        // Parse sequence
        var parser = new SequenceParser();
        var sequence = parser.Parse(fullPath);

        // Set up cancellation
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            Console.WriteLine();
            Console.WriteLine("Stopping...");
        };

        // Play
        var player = new SequencePlayer(midi);

        do
        {
            try
            {
                await player.PlayAsync(sequence, cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        } while (loop && !cts.Token.IsCancellationRequested);

        // Clean up
        for (int ch = 1; ch <= 16; ch++)
            midi.AllNotesOff(ch);

        return 0;
    }
    catch (Exception ex)
    {
        WriteError(ex.Message);
        return 1;
    }
}

static int Stop()
{
    Console.WriteLine("No background playback running.");
    Console.WriteLine("Use Ctrl+C to stop playback started with 'play'.");
    return 0;
}

static int UnknownCommand(string command)
{
    WriteError($"Unknown command: {command}");
    Console.WriteLine("Use 'sqncr help' for usage information.");
    return 1;
}

static void WriteError(string message)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Error: {message}");
    Console.ResetColor();
}
