using SqncR.VcvRack.Models;

namespace SqncR.VcvRack;

/// <summary>
/// Preset VCV Rack patch templates for common synthesis configurations.
/// Each template returns a fully wired VcvPatch ready to save and load.
/// </summary>
public static class PatchTemplates
{
    /// <summary>
    /// Minimal viable patch: MIDI-CV → VCO → VCF → ADSR → VCA → Audio.
    /// </summary>
    public static VcvPatch BasicSynth()
    {
        return new PatchBuilder()
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
    }

    /// <summary>
    /// Ambient pad: MIDI-CV → VCO (saw) → VCF (low cutoff) → ADSR (slow attack/release) → VCA → Audio.
    /// LFO modulates the filter cutoff for movement.
    /// </summary>
    public static VcvPatch AmbientPad()
    {
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

        return new PatchBuilder()
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
    }

    /// <summary>
    /// Bass synth: MIDI-CV → VCO (square) → VCF (resonant) → ADSR (punchy) → VCA → Audio.
    /// </summary>
    public static VcvPatch BassSynth()
    {
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

        return new PatchBuilder()
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
    }
}
