using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using ModelContextProtocol.Server;
using SqncR.Core.Generation;

namespace SqncR.McpServer.Tools;

[McpServerToolType]
public static class HealthTool
{
    internal static readonly ActivitySource ActivitySource = new("SqncR.McpServer");

    [McpServerTool(Name = "get_health"), Description("Returns a health snapshot of the generation engine including tick latency, active notes, memory usage, uptime, and missed ticks.")]
    public static string GetHealth(GenerationEngine engine)
    {
        using var activity = ActivitySource.StartActivity("mcp.get_health");
        activity?.SetTag("mcp.tool", "get_health");

        var snapshot = engine.HealthMonitor.GetHealth();
        return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool(Name = "all_notes_off"), Description("Panic button — immediately sends note-off for all active notes. Use if notes are stuck.")]
    public static string AllNotesOff(GenerationEngine engine)
    {
        using var activity = ActivitySource.StartActivity("mcp.all_notes_off");
        activity?.SetTag("mcp.tool", "all_notes_off");

        engine.Commands.TryWrite(new GenerationCommand.AllNotesOff());
        return "All notes off command sent.";
    }
}
