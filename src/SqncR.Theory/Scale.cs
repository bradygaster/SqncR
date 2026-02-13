namespace SqncR.Theory;

/// <summary>
/// A musical scale defined by a root note (MIDI number) and a set of intervals (semitones from root).
/// Immutable value type. All pitch-class math is mod-12.
/// </summary>
/// <param name="RootNote">MIDI number of the root note (0–127). The pitch class is RootNote % 12.</param>
/// <param name="Name">Human-readable scale name, e.g. "C Major".</param>
/// <param name="Intervals">Semitone offsets from root. Must start with 0. Ascending, unique, all &lt; 12.</param>
public sealed record Scale(int RootNote, string Name, IReadOnlyList<int> Intervals)
{
    // --- Common interval patterns (pitch class offsets) ---

    private static readonly int[] MajorIntervals = [0, 2, 4, 5, 7, 9, 11];
    private static readonly int[] NaturalMinorIntervals = [0, 2, 3, 5, 7, 8, 10];
    private static readonly int[] HarmonicMinorIntervals = [0, 2, 3, 5, 7, 8, 11];
    private static readonly int[] MelodicMinorIntervals = [0, 2, 3, 5, 7, 9, 11];
    private static readonly int[] PentatonicMajorIntervals = [0, 2, 4, 7, 9];
    private static readonly int[] PentatonicMinorIntervals = [0, 3, 5, 7, 10];
    private static readonly int[] BluesIntervals = [0, 3, 5, 6, 7, 10];
    private static readonly int[] WholeToneIntervals = [0, 2, 4, 6, 8, 10];
    private static readonly int[] DiminishedHalfWholeIntervals = [0, 1, 3, 4, 6, 7, 9, 10];
    private static readonly int[] ChromaticIntervals = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];

    // --- Factory methods ---

    public static Scale Major(int root) => new(root, "Major", MajorIntervals);
    public static Scale Minor(int root) => new(root, "Natural Minor", NaturalMinorIntervals);
    public static Scale HarmonicMinor(int root) => new(root, "Harmonic Minor", HarmonicMinorIntervals);
    public static Scale MelodicMinor(int root) => new(root, "Melodic Minor", MelodicMinorIntervals);
    public static Scale PentatonicMajor(int root) => new(root, "Pentatonic Major", PentatonicMajorIntervals);
    public static Scale PentatonicMinor(int root) => new(root, "Pentatonic Minor", PentatonicMinorIntervals);
    public static Scale Blues(int root) => new(root, "Blues", BluesIntervals);
    public static Scale WholeTone(int root) => new(root, "Whole Tone", WholeToneIntervals);
    public static Scale DiminishedHalfWhole(int root) => new(root, "Diminished", DiminishedHalfWholeIntervals);
    public static Scale Chromatic(int root) => new(root, "Chromatic", ChromaticIntervals);

    /// <summary>
    /// The pitch class (0–11) of the root note.
    /// </summary>
    public int RootPitchClass => RootNote % 12;

    /// <summary>
    /// Returns all MIDI note numbers in this scale for a given octave (-1 to 9).
    /// Octave follows the C4=60 convention: octave N starts at (N+1)*12.
    /// </summary>
    public IReadOnlyList<int> GetNotesInOctave(int octave)
    {
        var octaveBase = (octave + 1) * 12;
        var notes = new List<int>();

        foreach (var interval in Intervals)
        {
            var midi = octaveBase + RootPitchClass + interval;
            if (midi is >= 0 and <= 127)
                notes.Add(midi);
        }

        return notes;
    }

    /// <summary>
    /// Returns true if the given MIDI note belongs to this scale (in any octave).
    /// Compares pitch class only.
    /// </summary>
    public bool ContainsNote(int midiNote)
    {
        var pitchClass = ((midiNote % 12) - RootPitchClass + 12) % 12;
        return Intervals.Contains(pitchClass);
    }

    /// <summary>
    /// Snaps a MIDI note to the nearest note in this scale.
    /// If equidistant, rounds down.
    /// </summary>
    public int GetNearestScaleNote(int midiNote)
    {
        if (ContainsNote(midiNote))
            return midiNote;

        var below = midiNote;
        var above = midiNote;

        while (below >= 0 || above <= 127)
        {
            below--;
            if (below >= 0 && ContainsNote(below))
                return below;

            above++;
            if (above <= 127 && ContainsNote(above))
            {
                // Check if the note below is equidistant — prefer down
                if (below >= 0 && ContainsNote(below))
                    return (midiNote - below <= above - midiNote) ? below : above;
                return above;
            }
        }

        return midiNote; // Fallback (shouldn't happen with chromatic scale)
    }
}
