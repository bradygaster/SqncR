using System.Diagnostics;
using SqncR.VcvRack.Models;

namespace SqncR.VcvRack;

/// <summary>
/// Preset VCV Rack patch templates for common synthesis configurations.
/// Each template returns a fully wired VcvPatch ready to save and load.
/// </summary>
public static class PatchTemplates
{
    internal static readonly ActivitySource ActivitySource = new("SqncR.VcvRack");
    /// <summary>
    /// Minimal viable patch: MIDI-CV → VCO → VCF → ADSR → VCA → Audio.
    /// </summary>
    public static VcvPatch BasicSynth()
    {
        using var activity = ActivitySource.StartActivity("vcvrack.template.basic_synth");
        activity?.SetTag("vcvrack.template", "BasicSynth");

        var patch = new PatchBuilder()
            .AddModule(ModuleLibrary.MidiCv(), out var midi)
            .AddModule(ModuleLibrary.Vco(), out var vco)
            .AddModule(ModuleLibrary.Vcf(), out var vcf)
            .AddModule(ModuleLibrary.Adsr(), out var adsr)
            .AddModule(ModuleLibrary.Vca(), out var vca)
            .AddModule(ModuleLibrary.AudioOutput(), out var audio)
            .Cable(midi, "V/Oct", vco, "V/Oct")
            .Cable(midi, "Gate", adsr, "Gate")
            .Cable(vco, "Saw", vcf, "In")
            .Cable(adsr, "Out", vca, "CV")
            .Cable(vcf, "LPF", vca, "In")
            .Cable(vca, "Out", audio, "Input 1")
            .Build();

        activity?.SetTag("vcvrack.module_count", patch.Modules.Count);
        activity?.SetTag("vcvrack.cable_count", patch.Cables.Count);

        return patch;
    }

    /// <summary>
    /// Ambient pad: MIDI-CV → VCO (saw) → VCF (low cutoff) → ADSR (slow attack/release) → VCA → Audio.
    /// LFO modulates the filter cutoff for movement.
    /// </summary>
    public static VcvPatch AmbientPad()
    {
        using var activity = ActivitySource.StartActivity("vcvrack.template.ambient_pad");
        activity?.SetTag("vcvrack.template", "AmbientPad");
        var adsr = ModuleLibrary.Adsr() with
        {
            Params = new Dictionary<string, float>
            {
                ["Attack"] = 1.5f,
                ["Decay"] = 0.5f,
                ["Sustain"] = 0.8f,
                ["Release"] = 2.0f
            }
        };

        var vcf = ModuleLibrary.Vcf() with
        {
            Params = new Dictionary<string, float>
            {
                ["Freq"] = 0.3f,
                ["Res"] = 0.2f,
                ["Drive"] = 0f
            }
        };

        var patch = new PatchBuilder()
            .AddModule(ModuleLibrary.MidiCv(), out var midi)
            .AddModule(ModuleLibrary.Vco(), out var vco)
            .AddModule(vcf, out var vcfMod)
            .AddModule(adsr, out var adsrMod)
            .AddModule(ModuleLibrary.Vca(), out var vca)
            .AddModule(ModuleLibrary.Lfo(), out var lfo)
            .AddModule(ModuleLibrary.AudioOutput(), out var audio)
            .Cable(midi, "V/Oct", vco, "V/Oct")
            .Cable(midi, "Gate", adsrMod, "Gate")
            .Cable(vco, "Saw", vcfMod, "In")
            .Cable(lfo, "Sin", vcfMod, "Freq CV")
            .Cable(adsrMod, "Out", vca, "CV")
            .Cable(vcfMod, "LPF", vca, "In")
            .Cable(vca, "Out", audio, "Input 1")
            .Build();

        activity?.SetTag("vcvrack.module_count", patch.Modules.Count);
        activity?.SetTag("vcvrack.cable_count", patch.Cables.Count);

        return patch;
    }

    /// <summary>
    /// Bass synth: MIDI-CV → VCO (square) → VCF (resonant) → ADSR (punchy) → VCA → Audio.
    /// </summary>
    public static VcvPatch BassSynth()
    {
        using var activity = ActivitySource.StartActivity("vcvrack.template.bass_synth");
        activity?.SetTag("vcvrack.template", "BassSynth");

        var adsr = ModuleLibrary.Adsr() with
        {
            Params = new Dictionary<string, float>
            {
                ["Attack"] = 0.01f,
                ["Decay"] = 0.2f,
                ["Sustain"] = 0.4f,
                ["Release"] = 0.1f
            }
        };

        var vcf = ModuleLibrary.Vcf() with
        {
            Params = new Dictionary<string, float>
            {
                ["Freq"] = 0.4f,
                ["Res"] = 0.6f,
                ["Drive"] = 0.3f
            }
        };

        var patch = new PatchBuilder()
            .AddModule(ModuleLibrary.MidiCv(), out var midi)
            .AddModule(ModuleLibrary.Vco(), out var vco)
            .AddModule(vcf, out var vcfMod)
            .AddModule(adsr, out var adsrMod)
            .AddModule(ModuleLibrary.Vca(), out var vca)
            .AddModule(ModuleLibrary.AudioOutput(), out var audio)
            .Cable(midi, "V/Oct", vco, "V/Oct")
            .Cable(midi, "Gate", adsrMod, "Gate")
            .Cable(vco, "Sqr", vcfMod, "In")
            .Cable(adsrMod, "Out", vca, "CV")
            .Cable(vcfMod, "LPF", vca, "In")
            .Cable(vca, "Out", audio, "Input 1")
            .Build();

        activity?.SetTag("vcvrack.module_count", patch.Modules.Count);
        activity?.SetTag("vcvrack.cable_count", patch.Cables.Count);

        return patch;
    }

    /// <summary>
    /// 4-voice drum sequencer: MIDI-Gate drives kick, snare, hi-hat, and percussion
    /// through parallel ADSR → VCA chains into a mixer.
    /// </summary>
    public static VcvPatch TremorPatch()
    {
        using var activity = ActivitySource.StartActivity("vcvrack.template.tremor");
        activity?.SetTag("vcvrack.template", "TremorPatch");

        var kickEnv = ModuleLibrary.Adsr() with
        {
            Params = new Dictionary<string, float>
            {
                ["Attack"] = 0.01f, ["Decay"] = 0.15f, ["Sustain"] = 0f, ["Release"] = 0.05f
            }
        };

        var snareEnv = ModuleLibrary.Adsr() with
        {
            Params = new Dictionary<string, float>
            {
                ["Attack"] = 0.005f, ["Decay"] = 0.1f, ["Sustain"] = 0f, ["Release"] = 0.03f
            }
        };

        var hatEnv = ModuleLibrary.Adsr() with
        {
            Params = new Dictionary<string, float>
            {
                ["Attack"] = 0.001f, ["Decay"] = 0.05f, ["Sustain"] = 0f, ["Release"] = 0.02f
            }
        };

        var percEnv = ModuleLibrary.Adsr() with
        {
            Params = new Dictionary<string, float>
            {
                ["Attack"] = 0.01f, ["Decay"] = 0.3f, ["Sustain"] = 0.2f, ["Release"] = 0.15f
            }
        };

        var patch = new PatchBuilder()
            .AddModule(ModuleLibrary.MidiGate(), out var midiGate)
            .AddModule(kickEnv, out var kickAdsr)
            .AddModule(snareEnv, out var snareAdsr)
            .AddModule(hatEnv, out var hatAdsr)
            .AddModule(percEnv, out var percAdsr)
            .AddModule(ModuleLibrary.Vca(), out var kickVca)
            .AddModule(ModuleLibrary.Vca(), out var snareVca)
            .AddModule(ModuleLibrary.Vca(), out var hatVca)
            .AddModule(ModuleLibrary.Vca(), out var percVca)
            .AddModule(ModuleLibrary.Vco(), out var kickVco)
            .AddModule(ModuleLibrary.Noise(), out var noise)
            .AddModule(ModuleLibrary.Vcf(), out var hatVcf)
            .AddModule(ModuleLibrary.Vco(), out var percVco)
            .AddModule(ModuleLibrary.Mix(), out var mix)
            .AddModule(ModuleLibrary.AudioOutput(), out var audio)
            // Kick: Gate 1 → ADSR → VCA ← VCO (square)
            .Cable(midiGate, "Gate 1", kickAdsr, "Gate")
            .Cable(kickAdsr, "Out", kickVca, "CV")
            .Cable(kickVco, "Sqr", kickVca, "In")
            // Snare: Gate 2 → ADSR → VCA ← Noise (white)
            .Cable(midiGate, "Gate 2", snareAdsr, "Gate")
            .Cable(snareAdsr, "Out", snareVca, "CV")
            .Cable(noise, "White", snareVca, "In")
            // Hi-hat: Gate 3 → ADSR → VCA ← Noise → VCF (HPF)
            .Cable(midiGate, "Gate 3", hatAdsr, "Gate")
            .Cable(hatAdsr, "Out", hatVca, "CV")
            .Cable(noise, "White", hatVcf, "In")
            .Cable(hatVcf, "HPF", hatVca, "In")
            // Percussion: Gate 4 → ADSR → VCA ← VCO (triangle)
            .Cable(midiGate, "Gate 4", percAdsr, "Gate")
            .Cable(percAdsr, "Out", percVca, "CV")
            .Cable(percVco, "Tri", percVca, "In")
            // All voices → mixer → audio
            .Cable(kickVca, "Out", mix, "Input 1")
            .Cable(snareVca, "Out", mix, "Input 2")
            .Cable(hatVca, "Out", mix, "Input 3")
            .Cable(percVca, "Out", mix, "Input 4")
            .Cable(mix, "Mix", audio, "Input 1")
            .Build();

        activity?.SetTag("vcvrack.module_count", patch.Modules.Count);
        activity?.SetTag("vcvrack.cable_count", patch.Cables.Count);

        return patch;
    }

    /// <summary>
    /// Euclidean rhythm: MIDI-CV → VCO (saw) → VCF → VCA → Audio with LFO filter modulation.
    /// </summary>
    public static VcvPatch TidegatePatch()
    {
        using var activity = ActivitySource.StartActivity("vcvrack.template.tidegate");
        activity?.SetTag("vcvrack.template", "TidegatePatch");

        var vcf = ModuleLibrary.Vcf() with
        {
            Params = new Dictionary<string, float>
            {
                ["Freq"] = 0.45f, ["Res"] = 0.4f, ["Drive"] = 0.1f
            }
        };

        var patch = new PatchBuilder()
            .AddModule(ModuleLibrary.MidiCv(), out var midi)
            .AddModule(ModuleLibrary.Vco(), out var vco)
            .AddModule(ModuleLibrary.Adsr(), out var adsr)
            .AddModule(ModuleLibrary.Vca(), out var vca)
            .AddModule(vcf, out var vcfMod)
            .AddModule(ModuleLibrary.Lfo(), out var lfo)
            .AddModule(ModuleLibrary.AudioOutput(), out var audio)
            .Cable(midi, "V/Oct", vco, "V/Oct")
            .Cable(midi, "Gate", adsr, "Gate")
            .Cable(vco, "Saw", vcfMod, "In")
            .Cable(vcfMod, "LPF", vca, "In")
            .Cable(adsr, "Out", vca, "CV")
            .Cable(vca, "Out", audio, "Input 1")
            .Cable(lfo, "Sin", vcfMod, "Freq CV")
            .Build();

        activity?.SetTag("vcvrack.module_count", patch.Modules.Count);
        activity?.SetTag("vcvrack.cable_count", patch.Cables.Count);

        return patch;
    }

    /// <summary>
    /// Wavetable pad: dual VCO (saw + sub square) → VCF → VCA with slow LFO drift
    /// and vibrato modulation for evolving pad textures.
    /// </summary>
    public static VcvPatch DriftwavePatch()
    {
        using var activity = ActivitySource.StartActivity("vcvrack.template.driftwave");
        activity?.SetTag("vcvrack.template", "DriftwavePatch");

        var adsr = ModuleLibrary.Adsr() with
        {
            Params = new Dictionary<string, float>
            {
                ["Attack"] = 1.5f, ["Decay"] = 0.5f, ["Sustain"] = 0.8f, ["Release"] = 3.0f
            }
        };

        var vcf = ModuleLibrary.Vcf() with
        {
            Params = new Dictionary<string, float>
            {
                ["Freq"] = 0.25f, ["Res"] = 0.15f, ["Drive"] = 0f
            }
        };

        var driftLfo = ModuleLibrary.Lfo() with
        {
            Params = new Dictionary<string, float> { ["Freq"] = -3f, ["Offset"] = 0f }
        };

        var vibLfo = ModuleLibrary.Lfo() with
        {
            Params = new Dictionary<string, float> { ["Freq"] = -4f, ["Offset"] = 0f }
        };

        var patch = new PatchBuilder()
            .AddModule(ModuleLibrary.MidiCv(), out var midi)
            .AddModule(ModuleLibrary.Vco(), out var vco)
            .AddModule(ModuleLibrary.Vco(), out var subVco)
            .AddModule(adsr, out var adsrMod)
            .AddModule(vcf, out var vcfMod)
            .AddModule(ModuleLibrary.Vca(), out var vca)
            .AddModule(driftLfo, out var lfo1)
            .AddModule(vibLfo, out var lfo2)
            .AddModule(ModuleLibrary.Mix(), out var oscMix)
            .AddModule(ModuleLibrary.AudioOutput(), out var audio)
            .Cable(midi, "V/Oct", vco, "V/Oct")
            .Cable(midi, "V/Oct", subVco, "V/Oct")
            .Cable(midi, "Gate", adsrMod, "Gate")
            .Cable(vco, "Saw", oscMix, "Input 1")
            .Cable(subVco, "Sqr", oscMix, "Input 2")
            .Cable(oscMix, "Mix", vcfMod, "In")
            .Cable(vcfMod, "LPF", vca, "In")
            .Cable(adsrMod, "Out", vca, "CV")
            .Cable(vca, "Out", audio, "Input 1")
            .Cable(lfo1, "Sin", vcfMod, "Freq CV")
            .Cable(lfo2, "Sin", vco, "FM")
            .Build();

        activity?.SetTag("vcvrack.module_count", patch.Modules.Count);
        activity?.SetTag("vcvrack.cable_count", patch.Cables.Count);

        return patch;
    }

    /// <summary>
    /// Stereo delay: MIDI-CC controlled dual delay lines with independent time,
    /// feedback, and tone shaping routed to stereo audio outputs.
    /// </summary>
    public static VcvPatch RustlePatch()
    {
        using var activity = ActivitySource.StartActivity("vcvrack.template.rustle");
        activity?.SetTag("vcvrack.template", "RustlePatch");

        var delayLeft = ModuleLibrary.Delay() with
        {
            Params = new Dictionary<string, float>
            {
                ["Time"] = 0.375f, ["Feedback"] = 0.4f, ["Mix"] = 0.5f, ["Color"] = 0.5f
            }
        };

        var delayRight = ModuleLibrary.Delay() with
        {
            Params = new Dictionary<string, float>
            {
                ["Time"] = 0.5f, ["Feedback"] = 0.4f, ["Mix"] = 0.5f, ["Color"] = 0.5f
            }
        };

        var patch = new PatchBuilder()
            .AddModule(ModuleLibrary.MidiCc(), out var midiCc)
            .AddModule(delayLeft, out var delayL)
            .AddModule(delayRight, out var delayR)
            .AddModule(ModuleLibrary.Vca(), out var vcaL)
            .AddModule(ModuleLibrary.Vca(), out var vcaR)
            .AddModule(ModuleLibrary.Vcf(), out var vcf)
            .AddModule(ModuleLibrary.AudioOutput(), out var audio)
            // Left: Delay → VCF (tone) → VCA → Audio L
            .Cable(delayL, "Out", vcf, "In")
            .Cable(vcf, "LPF", vcaL, "In")
            // Right: Delay → VCA → Audio R
            .Cable(delayR, "Out", vcaR, "In")
            // MIDI-CC control: CC1 → delay time, CC2 → feedback, CC3 → tone
            .Cable(midiCc, "CC 1", delayL, "Time CV")
            .Cable(midiCc, "CC 2", vcaL, "CV")
            .Cable(midiCc, "CC 3", vcf, "Freq CV")
            // Stereo output
            .Cable(vcaL, "Out", audio, "Input 1")
            .Cable(vcaR, "Out", audio, "Input 2")
            .Build();

        activity?.SetTag("vcvrack.module_count", patch.Modules.Count);
        activity?.SetTag("vcvrack.cable_count", patch.Cables.Count);

        return patch;
    }

    /// <summary>
    /// Combined jam session wiring Tremor (drums), Tidegate (melody),
    /// Driftwave (pad), and Rustle (stereo delay) into a master mix.
    /// </summary>
    public static VcvPatch BradyJamSession()
    {
        using var activity = ActivitySource.StartActivity("vcvrack.template.brady_jam_session");
        activity?.SetTag("vcvrack.template", "BradyJamSession");

        // --- Tremor (drums) custom envelopes ---
        var kickEnv = ModuleLibrary.Adsr() with
        {
            Params = new Dictionary<string, float>
            {
                ["Attack"] = 0.01f, ["Decay"] = 0.15f, ["Sustain"] = 0f, ["Release"] = 0.05f
            }
        };
        var snareEnv = ModuleLibrary.Adsr() with
        {
            Params = new Dictionary<string, float>
            {
                ["Attack"] = 0.005f, ["Decay"] = 0.1f, ["Sustain"] = 0f, ["Release"] = 0.03f
            }
        };
        var hatEnv = ModuleLibrary.Adsr() with
        {
            Params = new Dictionary<string, float>
            {
                ["Attack"] = 0.001f, ["Decay"] = 0.05f, ["Sustain"] = 0f, ["Release"] = 0.02f
            }
        };
        var percEnv = ModuleLibrary.Adsr() with
        {
            Params = new Dictionary<string, float>
            {
                ["Attack"] = 0.01f, ["Decay"] = 0.3f, ["Sustain"] = 0.2f, ["Release"] = 0.15f
            }
        };

        // --- Tidegate custom VCF ---
        var tideFilter = ModuleLibrary.Vcf() with
        {
            Params = new Dictionary<string, float>
            {
                ["Freq"] = 0.45f, ["Res"] = 0.4f, ["Drive"] = 0.1f
            }
        };

        // --- Driftwave customizations ---
        var driftEnv = ModuleLibrary.Adsr() with
        {
            Params = new Dictionary<string, float>
            {
                ["Attack"] = 1.5f, ["Decay"] = 0.5f, ["Sustain"] = 0.8f, ["Release"] = 3.0f
            }
        };
        var driftFilter = ModuleLibrary.Vcf() with
        {
            Params = new Dictionary<string, float>
            {
                ["Freq"] = 0.25f, ["Res"] = 0.15f, ["Drive"] = 0f
            }
        };
        var driftLfo1 = ModuleLibrary.Lfo() with
        {
            Params = new Dictionary<string, float> { ["Freq"] = -3f, ["Offset"] = 0f }
        };
        var driftLfo2 = ModuleLibrary.Lfo() with
        {
            Params = new Dictionary<string, float> { ["Freq"] = -4f, ["Offset"] = 0f }
        };

        // --- Rustle delay customizations ---
        var delayLeft = ModuleLibrary.Delay() with
        {
            Params = new Dictionary<string, float>
            {
                ["Time"] = 0.375f, ["Feedback"] = 0.4f, ["Mix"] = 0.5f, ["Color"] = 0.5f
            }
        };
        var delayRight = ModuleLibrary.Delay() with
        {
            Params = new Dictionary<string, float>
            {
                ["Time"] = 0.5f, ["Feedback"] = 0.4f, ["Mix"] = 0.5f, ["Color"] = 0.5f
            }
        };

        var patch = new PatchBuilder()
            // ---- Tremor section (drums, Ch10) ----
            .AddModule(ModuleLibrary.MidiGate(), out var midiGate)
            .AddModule(kickEnv, out var kickAdsr)
            .AddModule(snareEnv, out var snareAdsr)
            .AddModule(hatEnv, out var hatAdsr)
            .AddModule(percEnv, out var percAdsr)
            .AddModule(ModuleLibrary.Vca(), out var kickVca)
            .AddModule(ModuleLibrary.Vca(), out var snareVca)
            .AddModule(ModuleLibrary.Vca(), out var hatVca)
            .AddModule(ModuleLibrary.Vca(), out var percVca)
            .AddModule(ModuleLibrary.Vco(), out var kickVco)
            .AddModule(ModuleLibrary.Noise(), out var noise)
            .AddModule(ModuleLibrary.Vcf(), out var hatVcf)
            .AddModule(ModuleLibrary.Vco(), out var percVco)
            .AddModule(ModuleLibrary.Mix(), out var drumMix)
            // ---- Tidegate section (melody, Ch2) ----
            .AddModule(ModuleLibrary.MidiCv(), out var tideMidi)
            .AddModule(ModuleLibrary.Vco(), out var tideVco)
            .AddModule(ModuleLibrary.Adsr(), out var tideAdsr)
            .AddModule(ModuleLibrary.Vca(), out var tideVca)
            .AddModule(tideFilter, out var tideVcf)
            .AddModule(ModuleLibrary.Lfo(), out var tideLfo)
            // ---- Driftwave section (pad, Ch3) ----
            .AddModule(ModuleLibrary.MidiCv(), out var driftMidi)
            .AddModule(ModuleLibrary.Vco(), out var driftVco)
            .AddModule(ModuleLibrary.Vco(), out var driftSubVco)
            .AddModule(driftEnv, out var driftAdsr)
            .AddModule(driftFilter, out var driftVcf)
            .AddModule(ModuleLibrary.Vca(), out var driftVca)
            .AddModule(driftLfo1, out var driftLfoSlow)
            .AddModule(driftLfo2, out var driftLfoVib)
            .AddModule(ModuleLibrary.Mix(), out var driftOscMix)
            // ---- Rustle section (stereo delay, Ch4) ----
            .AddModule(ModuleLibrary.MidiCc(), out var rustleMidiCc)
            .AddModule(delayLeft, out var rustleDelayL)
            .AddModule(delayRight, out var rustleDelayR)
            .AddModule(ModuleLibrary.Vca(), out var rustleVcaL)
            .AddModule(ModuleLibrary.Vca(), out var rustleVcaR)
            .AddModule(ModuleLibrary.Vcf(), out var rustleVcf)
            // ---- Master output ----
            .AddModule(ModuleLibrary.Mix(), out var masterMix)
            .AddModule(ModuleLibrary.AudioOutput(), out var audio)

            // === Tremor wiring ===
            .Cable(midiGate, "Gate 1", kickAdsr, "Gate")
            .Cable(kickAdsr, "Out", kickVca, "CV")
            .Cable(kickVco, "Sqr", kickVca, "In")
            .Cable(midiGate, "Gate 2", snareAdsr, "Gate")
            .Cable(snareAdsr, "Out", snareVca, "CV")
            .Cable(noise, "White", snareVca, "In")
            .Cable(midiGate, "Gate 3", hatAdsr, "Gate")
            .Cable(hatAdsr, "Out", hatVca, "CV")
            .Cable(noise, "White", hatVcf, "In")
            .Cable(hatVcf, "HPF", hatVca, "In")
            .Cable(midiGate, "Gate 4", percAdsr, "Gate")
            .Cable(percAdsr, "Out", percVca, "CV")
            .Cable(percVco, "Tri", percVca, "In")
            .Cable(kickVca, "Out", drumMix, "Input 1")
            .Cable(snareVca, "Out", drumMix, "Input 2")
            .Cable(hatVca, "Out", drumMix, "Input 3")
            .Cable(percVca, "Out", drumMix, "Input 4")

            // === Tidegate wiring ===
            .Cable(tideMidi, "V/Oct", tideVco, "V/Oct")
            .Cable(tideMidi, "Gate", tideAdsr, "Gate")
            .Cable(tideVco, "Saw", tideVcf, "In")
            .Cable(tideVcf, "LPF", tideVca, "In")
            .Cable(tideAdsr, "Out", tideVca, "CV")
            .Cable(tideLfo, "Sin", tideVcf, "Freq CV")

            // === Driftwave wiring ===
            .Cable(driftMidi, "V/Oct", driftVco, "V/Oct")
            .Cable(driftMidi, "V/Oct", driftSubVco, "V/Oct")
            .Cable(driftMidi, "Gate", driftAdsr, "Gate")
            .Cable(driftVco, "Saw", driftOscMix, "Input 1")
            .Cable(driftSubVco, "Sqr", driftOscMix, "Input 2")
            .Cable(driftOscMix, "Mix", driftVcf, "In")
            .Cable(driftVcf, "LPF", driftVca, "In")
            .Cable(driftAdsr, "Out", driftVca, "CV")
            .Cable(driftLfoSlow, "Sin", driftVcf, "Freq CV")
            .Cable(driftLfoVib, "Sin", driftVco, "FM")

            // === Rustle wiring (receives Driftwave + Tidegate) ===
            .Cable(driftVca, "Out", rustleDelayL, "In")
            .Cable(tideVca, "Out", rustleDelayR, "In")
            .Cable(rustleDelayL, "Out", rustleVcf, "In")
            .Cable(rustleVcf, "LPF", rustleVcaL, "In")
            .Cable(rustleDelayR, "Out", rustleVcaR, "In")
            .Cable(rustleMidiCc, "CC 1", rustleDelayL, "Time CV")
            .Cable(rustleMidiCc, "CC 2", rustleVcaL, "CV")
            .Cable(rustleMidiCc, "CC 3", rustleVcf, "Freq CV")

            // === Master routing ===
            .Cable(drumMix, "Mix", masterMix, "Input 1")
            .Cable(tideVca, "Out", masterMix, "Input 2")
            .Cable(driftVca, "Out", masterMix, "Input 3")
            .Cable(rustleVcaL, "Out", masterMix, "Input 4")
            .Cable(masterMix, "Mix", audio, "Input 1")
            .Cable(rustleVcaR, "Out", audio, "Input 2")
            .Build();

        activity?.SetTag("vcvrack.module_count", patch.Modules.Count);
        activity?.SetTag("vcvrack.cable_count", patch.Cables.Count);

        return patch;
    }
}
