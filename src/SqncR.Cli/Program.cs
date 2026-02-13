using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SqncR.Midi;

// Configure OpenTelemetry tracing
var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("SqncR.Cli"))
    .AddSource("SqncR.Midi")
    .AddSource("SqncR.Playback")
    .AddSource("SqncR.Cli")
    .AddOtlpExporter(opt =>
    {
        opt.Endpoint = new Uri(
            Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
            ?? "http://localhost:4317");
    })
    .Build();

// Emit a test trace on startup
var cliActivitySource = new ActivitySource("SqncR.Cli");
using (var startupActivity = cliActivitySource.StartActivity("cli.startup"))
{
    startupActivity?.SetTag("cli.args", string.Join(" ", args));
    startupActivity?.SetTag("cli.version", "0.1.0");
}

// Simple arg parsing - no external dependencies
if (args.Length == 0)
{
    ShowHelp();
    tracerProvider?.Dispose();
    return 0;
}

var command = args[0].ToLowerInvariant();

var result = command switch
{
    "list-devices" => ListDevices(),
    "--help" or "-h" or "help" => ShowHelp(),
    _ => UnknownCommand(command)
};

tracerProvider?.Dispose();
return result;

static int ShowHelp()
{
    Console.WriteLine(@"
SqncR - AI-Native Generative Music for MIDI Devices

USAGE:
  sqncr <command> [options]

COMMANDS:
  list-devices              List available MIDI output devices
  help                      Show this help

NOTE:
  Generative music is controlled via the MCP server.
  See docs/mcp-integration.md for setup instructions.

EXAMPLES:
  sqncr list-devices
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
        return 0;
    }
    catch (Exception ex)
    {
        WriteError(ex.Message);
        return 1;
    }
}

static int UnknownCommand(string command)
{
    WriteError($"Unknown command: {command}");
    Console.WriteLine("Use 'sqncr help' for usage information.");
    return 0;
}

static void WriteError(string message)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Error: {message}");
    Console.ResetColor();
}
