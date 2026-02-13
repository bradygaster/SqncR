using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using ModelContextProtocol.Server;
using SqncR.VcvRack;
using SqncR.VcvRack.Models;

namespace SqncR.McpServer.Tools;

[McpServerToolType]
public static class VcvRackTool
{
    internal static readonly ActivitySource ActivitySource = new("SqncR.McpServer");

    [McpServerTool(Name = "generate_patch"), Description("Generates a VCV Rack patch from a template and saves it as a .vcv file. Returns the file path and module list.")]
    public static string GeneratePatch(
        [Description("Template name: 'basic', 'ambient', or 'bass'")] string template = "basic",
        [Description("Display name for the patch")] string? name = null,
        [Description("Output file path for the .vcv file (defaults to temp directory)")] string? outputPath = null)
    {
        using var activity = ActivitySource.StartActivity("mcp.generate_patch");
        activity?.SetTag("mcp.tool", "generate_patch");
        activity?.SetTag("vcvrack.template", template);

        VcvPatch patch;
        try
        {
            patch = template.ToLowerInvariant() switch
            {
                "basic" => PatchTemplates.BasicSynth(),
                "ambient" => PatchTemplates.AmbientPad(),
                "bass" => PatchTemplates.BassSynth(),
                _ => throw new ArgumentException($"Unknown template '{template}'. Available: basic, ambient, bass")
            };
        }
        catch (ArgumentException ex)
        {
            return $"Error: {ex.Message}";
        }

        var fileName = $"{name ?? $"SqncR-{template}"}.vcv";
        var filePath = outputPath ?? Path.Combine(Path.GetTempPath(), fileName);

        try
        {
            patch.SaveAs(filePath);
        }
        catch (Exception ex)
        {
            return $"Error saving patch: {ex.Message}";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Patch saved: {filePath}");
        sb.AppendLine($"Template: {template}");
        sb.AppendLine($"Modules ({patch.Modules.Count}):");
        foreach (var module in patch.Modules)
        {
            sb.AppendLine($"  - {module.Model} ({module.Plugin})");
        }
        sb.AppendLine($"Cables: {patch.Cables.Count}");

        activity?.SetTag("vcvrack.output_path", filePath);
        activity?.SetTag("vcvrack.module_count", patch.Modules.Count);

        return sb.ToString().TrimEnd();
    }

    [McpServerTool(Name = "launch_vcv_rack"), Description("Launches VCV Rack 2 with a patch file. Requires VCV Rack to be installed.")]
    public static async Task<string> LaunchVcvRack(
        VcvRackLauncher launcher,
        [Description("Path to the .vcv patch file to load")] string patchPath,
        [Description("Run headless (no GUI)")] bool headless = true)
    {
        using var activity = ActivitySource.StartActivity("mcp.launch_vcv_rack");
        activity?.SetTag("mcp.tool", "launch_vcv_rack");

        try
        {
            await launcher.LaunchAsync(patchPath, headless);
            return $"VCV Rack launched with patch: {patchPath} (headless: {headless})";
        }
        catch (FileNotFoundException ex)
        {
            return $"Error: {ex.Message}";
        }
        catch (InvalidOperationException ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    [McpServerTool(Name = "stop_vcv_rack"), Description("Stops the running VCV Rack process.")]
    public static async Task<string> StopVcvRack(VcvRackLauncher launcher)
    {
        using var activity = ActivitySource.StartActivity("mcp.stop_vcv_rack");
        activity?.SetTag("mcp.tool", "stop_vcv_rack");

        if (!launcher.IsRunning)
            return "VCV Rack is not running.";

        await launcher.StopAsync();
        return "VCV Rack stopped.";
    }

    [McpServerTool(Name = "vcv_rack_status"), Description("Returns whether VCV Rack is currently running and its configuration.")]
    public static string VcvRackStatus(VcvRackLauncher launcher)
    {
        using var activity = ActivitySource.StartActivity("mcp.vcv_rack_status");
        activity?.SetTag("mcp.tool", "vcv_rack_status");

        var sb = new StringBuilder();
        sb.AppendLine($"VCV Rack: {(launcher.IsRunning ? "▶ Running" : "⏹ Stopped")}");
        sb.AppendLine($"MIDI Port: {launcher.MidiPortName}");

        return sb.ToString().TrimEnd();
    }

    [McpServerTool(Name = "list_templates"), Description("Lists available VCV Rack patch templates with descriptions.")]
    public static string ListTemplates()
    {
        using var activity = ActivitySource.StartActivity("mcp.list_templates");
        activity?.SetTag("mcp.tool", "list_templates");

        var sb = new StringBuilder();
        sb.AppendLine("Available VCV Rack patch templates:");
        sb.AppendLine();
        sb.AppendLine("  basic   — Minimal synth: MIDI-CV → VCO → VCF → ADSR → VCA → Audio");
        sb.AppendLine("  ambient — Ambient pad with slow attack/release and LFO-modulated filter");
        sb.AppendLine("  bass    — Punchy bass synth with resonant filter and square wave");

        return sb.ToString().TrimEnd();
    }
}
