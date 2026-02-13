using System.Collections.Frozen;

namespace SqncR.Core.Rhythm;

/// <summary>
/// Maps drum voices to MIDI note numbers. Default is General MIDI channel 10.
/// Immutable — create custom maps via the constructor.
/// </summary>
public sealed class DrumMap
{
    private readonly FrozenDictionary<DrumVoice, int> _map;

    /// <summary>Human-readable name for this map.</summary>
    public string Name { get; }

    public DrumMap(string name, IDictionary<DrumVoice, int> mappings)
    {
        Name = name;
        _map = mappings.ToFrozenDictionary();
    }

    /// <summary>Get the MIDI note number for a drum voice.</summary>
    public int GetMidiNote(DrumVoice voice)
    {
        if (_map.TryGetValue(voice, out var note))
            return note;
        throw new ArgumentException($"Drum voice '{voice}' is not mapped in '{Name}'.");
    }

    /// <summary>Check if a voice is mapped.</summary>
    public bool Contains(DrumVoice voice) => _map.ContainsKey(voice);

    /// <summary>All mapped voices.</summary>
    public IEnumerable<DrumVoice> Voices => _map.Keys;

    /// <summary>General MIDI standard drum map (channel 10).</summary>
    public static DrumMap GeneralMidi { get; } = new("General MIDI", new Dictionary<DrumVoice, int>
    {
        [DrumVoice.Kick] = 36,
        [DrumVoice.Snare] = 38,
        [DrumVoice.SideStick] = 37,
        [DrumVoice.Clap] = 39,
        [DrumVoice.ClosedHiHat] = 42,
        [DrumVoice.OpenHiHat] = 46,
        [DrumVoice.PedalHiHat] = 44,
        [DrumVoice.LowTom] = 45,
        [DrumVoice.MidTom] = 47,
        [DrumVoice.HighTom] = 50,
        [DrumVoice.Crash] = 49,
        [DrumVoice.Ride] = 51,
        [DrumVoice.Cowbell] = 56,
        [DrumVoice.Tambourine] = 54,
        [DrumVoice.RimShot] = 37
    });

    /// <summary>Roland TR-808 mapping — the classic drum machine.</summary>
    public static DrumMap TR808 { get; } = new("TR-808", new Dictionary<DrumVoice, int>
    {
        [DrumVoice.Kick] = 36,
        [DrumVoice.Snare] = 38,
        [DrumVoice.Clap] = 39,
        [DrumVoice.ClosedHiHat] = 42,
        [DrumVoice.OpenHiHat] = 46,
        [DrumVoice.LowTom] = 43,
        [DrumVoice.MidTom] = 47,
        [DrumVoice.HighTom] = 50,
        [DrumVoice.Crash] = 49,
        [DrumVoice.Ride] = 51,
        [DrumVoice.Cowbell] = 56,
        [DrumVoice.RimShot] = 37
    });
}
