using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using ModelContextProtocol.Server;
using SqncR.Midi;

namespace SqncR.McpServer.Tools;

[McpServerToolType]
public static class DevicesTool
{
    internal static readonly ActivitySource ActivitySource = new("SqncR.McpServer");

    [McpServerTool(Name = "list_devices"), Description("Lists available MIDI output devices on this system.")]
    public static string ListDevices(MidiService midiService)
    {
        using var activity = ActivitySource.StartActivity("mcp.list_devices");
        activity?.SetTag("mcp.tool", "list_devices");

        var devices = midiService.ListOutputDevices();

        if (devices.Count == 0)
        {
            activity?.SetTag("midi.device_count", 0);
            return "No MIDI output devices found. Ensure a MIDI device is connected or a virtual MIDI driver (e.g. loopMIDI) is installed.";
        }

        activity?.SetTag("midi.device_count", devices.Count);

        var sb = new StringBuilder();
        sb.AppendLine($"Found {devices.Count} MIDI output device(s):");
        foreach (var device in devices)
        {
            sb.AppendLine($"  [{device.Index}] {device.Name}");
        }

        return sb.ToString().TrimEnd();
    }
}
