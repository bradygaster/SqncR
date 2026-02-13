using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using ModelContextProtocol.Server;
using SqncR.Core.Generation;

namespace SqncR.McpServer.Tools;

[McpServerToolType]
public static class StatusTool
{
    internal static readonly ActivitySource ActivitySource = new("SqncR.McpServer");

    [McpServerTool(Name = "get_status"), Description("Returns the current state of the music generation engine including playback status, tempo, scale, pattern, and channels.")]
    public static string GetStatus(GenerationState state)
    {
        using var activity = ActivitySource.StartActivity("mcp.get_status");
        activity?.SetTag("mcp.tool", "get_status");

        var sb = new StringBuilder();
        sb.AppendLine($"Status: {(state.IsPlaying ? "▶ Playing" : "⏹ Stopped")}");
        sb.AppendLine($"Tempo: {state.Tempo} BPM");
        sb.AppendLine($"Scale: {state.Scale.Name}");
        sb.AppendLine($"Pattern: {state.DrumPattern?.Name ?? "(none)"}");
        sb.AppendLine($"Octave: {state.Octave}");
        sb.AppendLine($"Melodic Channel: {state.MelodicChannel}");
        sb.AppendLine($"Drum Channel: {state.DrumChannel}");

        return sb.ToString().TrimEnd();
    }
}
