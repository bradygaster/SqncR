using SqncR.VcvRack.Models;

namespace SqncR.VcvRack;

/// <summary>
/// Catalog of known VCV Rack Free modules with default configurations.
/// Each factory method returns a VcvModule template (ID 0, position 0,0)
/// that PatchBuilder will assign concrete IDs and positions.
/// </summary>
public static class ModuleLibrary
{
    /// <summary>
    /// Core MIDI-CV module — converts MIDI input to CV/Gate signals.
    /// </summary>
    public static VcvModule MidiCv() => new()
    {
        Plugin = "Core",
        Model = "MIDI-CV",
        OutputPorts = new Dictionary<string, int>
        {
            ["V/Oct"] = 0,
            ["Gate"] = 1,
            ["Velocity"] = 2,
            ["Aftertouch"] = 3,
            ["Pitch Wheel"] = 4,
            ["Mod Wheel"] = 5,
            ["Retrigger"] = 6,
            ["Clock"] = 7,
            ["Start"] = 8,
            ["Stop"] = 9,
            ["Continue"] = 10
        }
    };

    /// <summary>
    /// Fundamental VCO — voltage-controlled oscillator with multiple waveform outputs.
    /// </summary>
    public static VcvModule Vco() => new()
    {
        Plugin = "Fundamental",
        Model = "VCO",
        Params = new Dictionary<string, float>
        {
            ["Freq"] = 0f,
            ["Fine"] = 0f,
            ["PW"] = 0.5f
        },
        InputPorts = new Dictionary<string, int>
        {
            ["V/Oct"] = 0,
            ["FM"] = 1,
            ["PW"] = 2
        },
        OutputPorts = new Dictionary<string, int>
        {
            ["Sin"] = 0,
            ["Tri"] = 1,
            ["Saw"] = 2,
            ["Sqr"] = 3
        }
    };

    /// <summary>
    /// Fundamental VCF — voltage-controlled low-pass filter.
    /// </summary>
    public static VcvModule Vcf() => new()
    {
        Plugin = "Fundamental",
        Model = "VCF",
        Params = new Dictionary<string, float>
        {
            ["Freq"] = 0.5f,
            ["Res"] = 0f,
            ["Drive"] = 0f
        },
        InputPorts = new Dictionary<string, int>
        {
            ["In"] = 0,
            ["Freq CV"] = 1,
            ["Res CV"] = 2,
            ["Drive CV"] = 3
        },
        OutputPorts = new Dictionary<string, int>
        {
            ["LPF"] = 0,
            ["HPF"] = 1
        }
    };

    /// <summary>
    /// Fundamental VCA — voltage-controlled amplifier.
    /// </summary>
    public static VcvModule Vca() => new()
    {
        Plugin = "Fundamental",
        Model = "VCA-1",
        Params = new Dictionary<string, float>
        {
            ["Level"] = 1f
        },
        InputPorts = new Dictionary<string, int>
        {
            ["In"] = 0,
            ["CV"] = 1
        },
        OutputPorts = new Dictionary<string, int>
        {
            ["Out"] = 0
        }
    };

    /// <summary>
    /// Fundamental ADSR — envelope generator.
    /// </summary>
    public static VcvModule Adsr() => new()
    {
        Plugin = "Fundamental",
        Model = "ADSR",
        Params = new Dictionary<string, float>
        {
            ["Attack"] = 0.1f,
            ["Decay"] = 0.3f,
            ["Sustain"] = 0.7f,
            ["Release"] = 0.5f
        },
        InputPorts = new Dictionary<string, int>
        {
            ["Gate"] = 0,
            ["Retrig"] = 1
        },
        OutputPorts = new Dictionary<string, int>
        {
            ["Out"] = 0
        }
    };

    /// <summary>
    /// Fundamental LFO — low-frequency oscillator for modulation.
    /// </summary>
    public static VcvModule Lfo() => new()
    {
        Plugin = "Fundamental",
        Model = "LFO",
        Params = new Dictionary<string, float>
        {
            ["Freq"] = -2f,
            ["Offset"] = 0f
        },
        InputPorts = new Dictionary<string, int>
        {
            ["FM"] = 0,
            ["Reset"] = 1
        },
        OutputPorts = new Dictionary<string, int>
        {
            ["Sin"] = 0,
            ["Tri"] = 1,
            ["Saw"] = 2,
            ["Sqr"] = 3
        }
    };

    /// <summary>
    /// Core Audio-8 — audio output module.
    /// </summary>
    public static VcvModule AudioOutput() => new()
    {
        Plugin = "Core",
        Model = "AudioInterface",
        InputPorts = new Dictionary<string, int>
        {
            ["Input 1"] = 0,
            ["Input 2"] = 1,
            ["Input 3"] = 2,
            ["Input 4"] = 3,
            ["Input 5"] = 4,
            ["Input 6"] = 5,
            ["Input 7"] = 6,
            ["Input 8"] = 7
        }
    };

    /// <summary>
    /// Fundamental Mix — simple mixer module.
    /// </summary>
    public static VcvModule Mix() => new()
    {
        Plugin = "Fundamental",
        Model = "VCMixer",
        Params = new Dictionary<string, float>
        {
            ["Level 1"] = 1f,
            ["Level 2"] = 1f,
            ["Level 3"] = 1f,
            ["Level 4"] = 1f
        },
        InputPorts = new Dictionary<string, int>
        {
            ["Input 1"] = 0,
            ["Input 2"] = 1,
            ["Input 3"] = 2,
            ["Input 4"] = 3
        },
        OutputPorts = new Dictionary<string, int>
        {
            ["Mix"] = 0
        }
    };
}
