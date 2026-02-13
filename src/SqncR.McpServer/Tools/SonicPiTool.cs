using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using ModelContextProtocol.Server;
using SqncR.Core;
using SqncR.SonicPi;

namespace SqncR.McpServer.Tools;

[McpServerToolType]
public static class SonicPiTool
{
    internal static readonly ActivitySource ActivitySource = new("SqncR.McpServer");

    [McpServerTool(Name = "setup_software_synth"), Description("Sets up a Sonic Pi software synthesizer. Creates an instrument with the specified synth engine and optional effects chain, then activates it.")]
    public static string SetupSoftwareSynth(
        OscClient oscClient,
        [Description("Sonic Pi synth name (e.g. 'prophet', 'tb303', 'blade', 'piano', 'dark_ambience')")] string synthName = "prophet",
        [Description("Comma-separated FX chain (e.g. 'reverb,echo')")] string? effects = null,
        [Description("Display name for this instrument")] string? name = null)
    {
        using var activity = ActivitySource.StartActivity("mcp.setup_software_synth");
        activity?.SetTag("mcp.tool", "setup_software_synth");
        activity?.SetTag("sonicpi.synth", synthName);

        try
        {
            var displayName = name ?? synthName;
            var instrument = new SonicPiInstrument(displayName, synthName);

            if (!string.IsNullOrWhiteSpace(effects))
            {
                foreach (var fx in effects.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    instrument.FxChain[fx] = 0.5;
                }
            }

            instrument.Activate(oscClient);

            var sb = new StringBuilder();
            sb.AppendLine($"✓ Synth '{displayName}' activated (engine: {synthName})");
            if (instrument.FxChain.Count > 0)
                sb.AppendLine($"  FX: {string.Join(", ", instrument.FxChain.Keys)}");
            sb.AppendLine($"  Available synths: {string.Join(", ", RubyCodeGenerator.BuiltInSynths)}");

            return sb.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            return $"Error setting up synth: {ex.Message}";
        }
    }

    [McpServerTool(Name = "play_sonic_pi_note"), Description("Plays a single note on Sonic Pi. Accepts note names (C4, F#3) or MIDI numbers.")]
    public static string PlaySonicPiNote(
        OscClient oscClient,
        [Description("Note name (e.g. 'C4', 'F#3') or MIDI number (e.g. '60')")] string note,
        [Description("Duration in seconds")] double duration = 1.0,
        [Description("Velocity (0-127)")] int velocity = 80)
    {
        using var activity = ActivitySource.StartActivity("mcp.play_sonic_pi_note");
        activity?.SetTag("mcp.tool", "play_sonic_pi_note");
        activity?.SetTag("sonicpi.note", note);

        try
        {
            int midiNote = int.TryParse(note, out var parsed)
                ? parsed
                : NoteParser.Parse(note);

            if (midiNote < 0 || midiNote > 127)
                return $"Error: MIDI note {midiNote} is outside valid range (0-127).";

            velocity = Math.Clamp(velocity, 0, 127);

            var code = RubyCodeGenerator.GeneratePlayNote(midiNote, duration, velocity);
            oscClient.SendCode(code);

            return $"♪ Playing {NoteParser.ToNoteName(midiNote)} (MIDI {midiNote}) for {duration.ToString("G", CultureInfo.InvariantCulture)}s at velocity {velocity}";
        }
        catch (ArgumentException ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    [McpServerTool(Name = "sonic_pi_live_loop"), Description("Creates and sends a live_loop to Sonic Pi that cycles through a sequence of notes at the specified BPM.")]
    public static string SonicPiLiveLoop(
        OscClient oscClient,
        [Description("Name for the live_loop")] string loopName,
        [Description("Comma-separated note names (e.g. 'C4,E4,G4,C5')")] string notes,
        [Description("Sonic Pi synth name")] string synthName = "prophet",
        [Description("Tempo in BPM")] double bpm = 120,
        [Description("Comma-separated FX (e.g. 'reverb,echo')")] string? effects = null)
    {
        using var activity = ActivitySource.StartActivity("mcp.sonic_pi_live_loop");
        activity?.SetTag("mcp.tool", "sonic_pi_live_loop");
        activity?.SetTag("sonicpi.loop", loopName);

        try
        {
            var noteNames = notes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var midiNotes = new int[noteNames.Length];
            for (int i = 0; i < noteNames.Length; i++)
            {
                midiNotes[i] = int.TryParse(noteNames[i], out var parsed)
                    ? parsed
                    : NoteParser.Parse(noteNames[i]);
            }

            Dictionary<string, double>? fx = null;
            if (!string.IsNullOrWhiteSpace(effects))
            {
                fx = new Dictionary<string, double>();
                foreach (var fxName in effects.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    fx[fxName] = 0.5;
                }
            }

            var code = RubyCodeGenerator.GenerateLiveLoop(loopName, synthName, midiNotes, bpm, fx);
            oscClient.SendCode(code);

            return $"🔁 Live loop '{loopName}' started: {noteNames.Length} notes at {bpm.ToString("G", CultureInfo.InvariantCulture)} BPM using {synthName}" +
                   (fx != null ? $" with FX: {string.Join(", ", fx.Keys)}" : "");
        }
        catch (ArgumentException ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    [McpServerTool(Name = "stop_sonic_pi"), Description("Stops all running Sonic Pi code and silences all sound.")]
    public static string StopSonicPi(OscClient oscClient)
    {
        using var activity = ActivitySource.StartActivity("mcp.stop_sonic_pi");
        activity?.SetTag("mcp.tool", "stop_sonic_pi");

        try
        {
            oscClient.StopAll();
            return "⏹ Sonic Pi stopped — all jobs halted.";
        }
        catch (Exception ex)
        {
            return $"Error stopping Sonic Pi: {ex.Message}";
        }
    }

    [McpServerTool(Name = "sonic_pi_status"), Description("Checks whether Sonic Pi appears to be running and reachable via OSC.")]
    public static string SonicPiStatus(OscClient oscClient)
    {
        using var activity = ActivitySource.StartActivity("mcp.sonic_pi_status");
        activity?.SetTag("mcp.tool", "sonic_pi_status");

        var available = oscClient.IsAvailable();
        return available
            ? $"✓ Sonic Pi reachable at {oscClient.Host}:{oscClient.Port}"
            : $"✗ Sonic Pi not detected at {oscClient.Host}:{oscClient.Port} — is it running?";
    }
}
