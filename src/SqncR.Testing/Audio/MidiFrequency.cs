namespace SqncR.Testing.Audio;

/// <summary>
/// MIDI note number ↔ frequency conversion utilities.
/// Uses A4 = MIDI 69 = 440 Hz standard tuning.
/// </summary>
public static class MidiFrequency
{
    private static readonly string[] NoteNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];

    /// <summary>
    /// Convert a MIDI note number to frequency in Hz.
    /// Formula: 440 × 2^((note - 69) / 12)
    /// </summary>
    public static double MidiToFrequency(int midiNote)
    {
        return 440.0 * Math.Pow(2, (midiNote - 69) / 12.0);
    }

    /// <summary>
    /// Convert a frequency in Hz to the nearest MIDI note number.
    /// </summary>
    public static int FrequencyToMidi(double hz)
    {
        if (hz <= 0)
            throw new ArgumentOutOfRangeException(nameof(hz), "Frequency must be positive.");

        return (int)Math.Round(69 + 12 * Math.Log2(hz / 440.0));
    }

    /// <summary>
    /// Convert a frequency to its nearest note name (e.g., "C4", "A#3").
    /// </summary>
    public static string FrequencyToNoteName(double hz)
    {
        return MidiToNoteName(FrequencyToMidi(hz));
    }

    /// <summary>
    /// Convert a MIDI note number to its note name (e.g., "C4", "A#3").
    /// </summary>
    public static string MidiToNoteName(int midiNote)
    {
        var noteIndex = ((midiNote % 12) + 12) % 12;
        var octave = (midiNote / 12) - 1;
        return $"{NoteNames[noteIndex]}{octave}";
    }
}
