using System.ComponentModel;
using System.Diagnostics;
using ModelContextProtocol.Server;
using SqncR.Midi;

namespace SqncR.McpServer.Tools;

[McpServerToolType]
public static class OpenDeviceTool
{
    internal static readonly ActivitySource ActivitySource = new("SqncR.McpServer");

    [McpServerTool(Name = "open_device"), Description("Opens a MIDI output device by index or name. Use list_devices first to see available devices.")]
    public static string OpenDevice(
        MidiService midiService,
        [Description("Device index from list_devices")] int? deviceIndex = null,
        [Description("Device name (partial match, case-insensitive)")] string? deviceName = null)
    {
        using var activity = ActivitySource.StartActivity("mcp.open_device");
        activity?.SetTag("mcp.tool", "open_device");

        try
        {
            if (deviceIndex.HasValue)
            {
                midiService.OpenDevice(deviceIndex.Value);
                activity?.SetTag("midi.device_index", deviceIndex.Value);
            }
            else if (!string.IsNullOrWhiteSpace(deviceName))
            {
                midiService.OpenDevice(deviceName);
                activity?.SetTag("midi.device_name", deviceName);
            }
            else
            {
                return "Error: Provide either deviceIndex or deviceName.";
            }

            return $"Opened MIDI device: {midiService.CurrentDeviceName}";
        }
        catch (ArgumentException ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}
