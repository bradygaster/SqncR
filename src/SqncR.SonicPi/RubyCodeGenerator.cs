using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace SqncR.SonicPi;

/// <summary>
/// Generates Ruby code strings for Sonic Pi evaluation.
/// All output is valid Sonic Pi Ruby DSL.
/// </summary>
public static class RubyCodeGenerator
{
    internal static readonly ActivitySource ActivitySource = new("SqncR.SonicPi");
    /// <summary>
    /// Built-in Sonic Pi synth names.
    /// </summary>
    public static readonly IReadOnlyList<string> BuiltInSynths = new[]
    {
        "beep", "prophet", "tb303", "dark_ambience", "blade", "hollow", "pluck", "piano"
    };

    /// <summary>
    /// Built-in Sonic Pi FX names.
    /// </summary>
    public static readonly IReadOnlyList<string> BuiltInFx = new[]
    {
        "reverb", "echo", "flanger", "lpf", "hpf", "distortion", "wobble"
    };

    /// <summary>
    /// Generates a use_synth statement.
    /// Example: use_synth :prophet
    /// </summary>
    public static string GenerateSynthSetup(string synthName, Dictionary<string, object>? parameters = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(synthName);

        using var activity = ActivitySource.StartActivity("sonicpi.generate_synth_setup");
        activity?.SetTag("sonicpi.synth_name", synthName);
        var sw = Stopwatch.StartNew();

        var sb = new StringBuilder();
        sb.AppendLine($"use_synth :{synthName}");

        if (parameters is { Count: > 0 })
        {
            foreach (var (key, value) in parameters)
            {
                sb.AppendLine($"set :{key}, {FormatValue(value)}");
            }
        }

        var result = sb.ToString().TrimEnd();

        sw.Stop();
        SonicPiMetrics.CodeGenerationTime.Record(sw.Elapsed.TotalMicroseconds);

        return result;
    }

    /// <summary>
    /// Generates a complete live_loop block with synth, notes, BPM, and optional FX.
    /// </summary>
    public static string GenerateLiveLoop(
        string name,
        string synthName,
        int[] notes,
        double bpm,
        Dictionary<string, double>? fx = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(synthName);
        ArgumentNullException.ThrowIfNull(notes);

        using var activity = ActivitySource.StartActivity("sonicpi.generate_live_loop");
        activity?.SetTag("sonicpi.synth_name", synthName);
        activity?.SetTag("sonicpi.note_count", notes.Length);
        activity?.SetTag("sonicpi.fx_count", fx?.Count ?? 0);
        var sw = Stopwatch.StartNew();

        var sb = new StringBuilder();
        sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"use_bpm {bpm}"));
        sb.AppendLine();
        sb.AppendLine($"live_loop :{name} do");

        var indent = "  ";
        var fxIndent = indent;

        // Wrap in FX blocks if specified
        if (fx is { Count: > 0 })
        {
            foreach (var (fxName, fxValue) in fx)
            {
                sb.AppendLine($"{fxIndent}with_fx :{fxName}, mix: {FormatDouble(fxValue)} do");
                fxIndent += "  ";
            }
        }

        sb.AppendLine($"{fxIndent}use_synth :{synthName}");

        var notesStr = string.Join(", ", notes);
        sb.AppendLine($"{fxIndent}notes = [{notesStr}]");
        sb.AppendLine($"{fxIndent}notes.each do |n|");
        sb.AppendLine($"{fxIndent}  play n");
        sb.AppendLine($"{fxIndent}  sleep 0.5");
        sb.AppendLine($"{fxIndent}end");

        // Close FX blocks
        if (fx is { Count: > 0 })
        {
            for (int i = fx.Count - 1; i >= 0; i--)
            {
                fxIndent = indent + new string(' ', i * 2);
                sb.AppendLine($"{fxIndent}end");
            }
        }

        sb.AppendLine("end");

        var result = sb.ToString().TrimEnd();

        sw.Stop();
        SonicPiMetrics.CodeGenerationTime.Record(sw.Elapsed.TotalMicroseconds);

        return result;
    }

    /// <summary>
    /// Generates a play note statement with MIDI note, duration, and velocity (converted to amp 0.0–1.0).
    /// </summary>
    public static string GeneratePlayNote(int midiNote, double duration, int velocity)
    {
        var amp = Math.Clamp(velocity / 127.0, 0.0, 1.0);
        return $"play {midiNote}, sustain: {FormatDouble(duration)}, amp: {FormatDouble(amp)}";
    }

    /// <summary>
    /// Generates nested with_fx blocks wrapping a yield placeholder.
    /// </summary>
    public static string GenerateFxChain(Dictionary<string, double> effects)
    {
        ArgumentNullException.ThrowIfNull(effects);

        if (effects.Count == 0)
            return "# no effects";

        var sb = new StringBuilder();
        var indent = "";
        foreach (var (fxName, mix) in effects)
        {
            sb.AppendLine($"{indent}with_fx :{fxName}, mix: {FormatDouble(mix)} do");
            indent += "  ";
        }

        sb.AppendLine($"{indent}# play notes here");

        for (int i = effects.Count - 1; i >= 0; i--)
        {
            indent = new string(' ', i * 2);
            sb.AppendLine($"{indent}end");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Generates a sustained drone tone using a live_loop.
    /// </summary>
    public static string GenerateDrone(string synthName, int midiNote, Dictionary<string, double>? parameters = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(synthName);

        var sb = new StringBuilder();
        sb.AppendLine("live_loop :drone do");
        sb.AppendLine($"  use_synth :{synthName}");

        var paramStr = new StringBuilder();
        paramStr.Append($"  play {midiNote}, sustain: 8");
        if (parameters is { Count: > 0 })
        {
            foreach (var (key, value) in parameters)
            {
                paramStr.Append($", {key}: {FormatDouble(value)}");
            }
        }
        sb.AppendLine(paramStr.ToString());
        sb.AppendLine("  sleep 8");
        sb.AppendLine("end");

        return sb.ToString().TrimEnd();
    }

    private static string FormatDouble(double value) =>
        value.ToString("G", CultureInfo.InvariantCulture);

    private static string FormatValue(object value) => value switch
    {
        double d => FormatDouble(d),
        float f => FormatDouble(f),
        int i => i.ToString(CultureInfo.InvariantCulture),
        string s => $"\"{s}\"",
        _ => value.ToString() ?? ""
    };
}
