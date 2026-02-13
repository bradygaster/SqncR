using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using ModelContextProtocol.Server;
using SqncR.Core;
using SqncR.Core.Generation;
using SqncR.Core.Rhythm;
using SqncR.Theory;

namespace SqncR.McpServer.Tools;

[McpServerToolType]
public static class GenerationTool
{
    internal static readonly ActivitySource ActivitySource = new("SqncR.McpServer");

    [McpServerTool(Name = "start_generation"), Description("Starts music generation with the specified parameters. Configures tempo, scale, pattern, and octave then begins playback.")]
    public static string StartGeneration(
        GenerationEngine engine,
        GenerationState state,
        [Description("Tempo in BPM")] double tempo = 120,
        [Description("Scale name (e.g. 'Pentatonic Minor', 'Dorian', 'Blues')")] string scale = "Pentatonic Minor",
        [Description("Root note (e.g. 'C4', 'F#3', 'Bb2')")] string rootNote = "C4",
        [Description("Drum pattern (rock, house, hip-hop, jazz, ambient)")] string pattern = "rock",
        [Description("Base octave for melody (0-8)")] int octave = 4,
        [Description("Variety level: conservative, moderate, adventurous, off")] string variety = "off")
    {
        using var activity = ActivitySource.StartActivity("mcp.start_generation");
        activity?.SetTag("mcp.tool", "start_generation");

        try
        {
            var midiRoot = NoteParser.Parse(rootNote);
            var scaleObj = ScaleLibrary.Get(scale, midiRoot);
            var patternObj = PatternLibrary.Get(pattern);

            engine.Commands.TryWrite(new GenerationCommand.SetTempo(tempo));
            engine.Commands.TryWrite(new GenerationCommand.SetScale(scaleObj));
            engine.Commands.TryWrite(new GenerationCommand.SetPattern(patternObj));
            engine.Commands.TryWrite(new GenerationCommand.SetOctave(octave));

            var varietySuffix = "";
            if (!string.Equals(variety, "off", StringComparison.OrdinalIgnoreCase))
            {
                var varietyLevel = ParseVarietyLevel(variety);
                if (varietyLevel.HasValue)
                {
                    engine.Commands.TryWrite(new GenerationCommand.SetVarietyLevel(varietyLevel.Value));
                    varietySuffix = $", variety {variety}";
                }
            }

            engine.Commands.TryWrite(new GenerationCommand.Start());

            return $"Started generation: {tempo} BPM, {scaleObj.Name} (root {rootNote}), {pattern} pattern, octave {octave}{varietySuffix}";
        }
        catch (ArgumentException ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    [McpServerTool(Name = "modify_generation"), Description("Modifies the current generation parameters without stopping playback. Only provided parameters are changed.")]
    public static string ModifyGeneration(
        GenerationEngine engine,
        GenerationState state,
        [Description("New tempo in BPM")] double? tempo = null,
        [Description("New scale name")] string? scale = null,
        [Description("New root note (e.g. 'C4')")] string? rootNote = null,
        [Description("New drum pattern")] string? pattern = null,
        [Description("New base octave")] int? octave = null,
        [Description("When true, transitions happen smoothly over bars instead of instantly")] bool smooth = false,
        [Description("Variety level: conservative, moderate, adventurous, off")] string? variety = null)
    {
        using var activity = ActivitySource.StartActivity("mcp.modify_generation");
        activity?.SetTag("mcp.tool", "modify_generation");

        var modified = new StringBuilder();

        try
        {
            if (tempo.HasValue)
            {
                if (smooth)
                    engine.Commands.TryWrite(new GenerationCommand.SetTempoSmooth(tempo.Value));
                else
                    engine.Commands.TryWrite(new GenerationCommand.SetTempo(tempo.Value));
                modified.AppendLine($"  Tempo → {tempo.Value} BPM{(smooth ? " (smooth)" : "")}");
            }

            if (!string.IsNullOrWhiteSpace(scale) || !string.IsNullOrWhiteSpace(rootNote))
            {
                var resolvedScale = scale ?? state.Scale.Name;
                var resolvedRoot = !string.IsNullOrWhiteSpace(rootNote)
                    ? NoteParser.Parse(rootNote)
                    : state.Scale.RootNote;
                var scaleObj = ScaleLibrary.Get(resolvedScale, resolvedRoot);
                if (smooth)
                    engine.Commands.TryWrite(new GenerationCommand.SetScaleSmooth(scaleObj));
                else
                    engine.Commands.TryWrite(new GenerationCommand.SetScale(scaleObj));
                modified.AppendLine($"  Scale → {scaleObj.Name}{(smooth ? " (smooth)" : "")}");
            }

            if (!string.IsNullOrWhiteSpace(pattern))
            {
                var patternObj = PatternLibrary.Get(pattern);
                engine.Commands.TryWrite(new GenerationCommand.SetPattern(patternObj));
                modified.AppendLine($"  Pattern → {pattern}");
            }

            if (octave.HasValue)
            {
                engine.Commands.TryWrite(new GenerationCommand.SetOctave(octave.Value));
                modified.AppendLine($"  Octave → {octave.Value}");
            }

            if (!string.IsNullOrWhiteSpace(variety))
            {
                if (string.Equals(variety, "off", StringComparison.OrdinalIgnoreCase))
                {
                    state.Variety = null;
                    modified.AppendLine("  Variety → off");
                }
                else
                {
                    var varietyLevel = ParseVarietyLevel(variety);
                    if (varietyLevel.HasValue)
                    {
                        engine.Commands.TryWrite(new GenerationCommand.SetVarietyLevel(varietyLevel.Value));
                        modified.AppendLine($"  Variety → {variety}");
                    }
                }
            }

            if (modified.Length == 0)
                return "No parameters provided. Nothing was modified.";

            return $"Modified generation:\n{modified.ToString().TrimEnd()}";
        }
        catch (ArgumentException ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    [McpServerTool(Name = "stop_generation"), Description("Stops music generation and silences all notes.")]
    public static string StopGeneration(GenerationEngine engine)
    {
        using var activity = ActivitySource.StartActivity("mcp.stop_generation");
        activity?.SetTag("mcp.tool", "stop_generation");

        engine.Commands.TryWrite(new GenerationCommand.Stop());

        return "Generation stopped.";
    }

    private static VarietyLevel? ParseVarietyLevel(string value) => value.ToLowerInvariant() switch
    {
        "conservative" => VarietyLevel.Conservative,
        "moderate" => VarietyLevel.Moderate,
        "adventurous" => VarietyLevel.Adventurous,
        _ => null
    };
}
