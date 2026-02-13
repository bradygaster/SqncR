namespace SqncR.Core.Generation;

/// <summary>
/// Plays chord tones (root, 3rd, 5th) in configurable arpeggio patterns.
/// </summary>
public sealed class ArpeggioGenerator : INoteGenerator
{
    private readonly ArpeggioPattern _pattern;
    private readonly Random _random;
    private int _stepIndex;

    public string Name => $"Arpeggio ({_pattern})";

    public ArpeggioGenerator(ArpeggioPattern pattern = ArpeggioPattern.Up, Random? random = null)
    {
        _pattern = pattern;
        _random = random ?? Random.Shared;
    }

    public int? NextNote(GenerationState state)
    {
        var chordTones = GetChordTones(state);
        if (chordTones.Count == 0) return null;

        int index = _pattern switch
        {
            ArpeggioPattern.Up => GetUpIndex(chordTones.Count),
            ArpeggioPattern.Down => GetDownIndex(chordTones.Count),
            ArpeggioPattern.UpDown => GetUpDownIndex(chordTones.Count),
            ArpeggioPattern.Random => _random.Next(chordTones.Count),
            _ => 0
        };

        _stepIndex++;
        return chordTones[index];
    }

    /// <summary>
    /// Extracts chord tones (root, 3rd, 5th) from the current scale and octave.
    /// For scales with fewer than 5 notes, uses available degrees.
    /// </summary>
    private static IReadOnlyList<int> GetChordTones(GenerationState state)
    {
        var scaleNotes = state.Scale.GetNotesInOctave(state.Octave);
        if (scaleNotes.Count == 0) return [];

        var tones = new List<int>();

        // Root (degree 0)
        tones.Add(scaleNotes[0]);

        // Third (degree 2 in the scale array, which is the 3rd scale degree)
        if (scaleNotes.Count > 2)
            tones.Add(scaleNotes[2]);

        // Fifth (degree 4 in the scale array, which is the 5th scale degree)
        if (scaleNotes.Count > 4)
            tones.Add(scaleNotes[4]);

        return tones;
    }

    private int GetUpIndex(int count)
    {
        return _stepIndex % count;
    }

    private int GetDownIndex(int count)
    {
        // Down pattern: root→5th→3rd→root (reversed order)
        int reversedStep = (count - 1) - (_stepIndex % count);
        // But spec says root→5th→3rd→root, so: indices 0,2,1,0,...
        // For 3 tones: cycle is [0, 2, 1]
        int[] downPattern = BuildDownPattern(count);
        return downPattern[_stepIndex % downPattern.Length];
    }

    private int GetUpDownIndex(int count)
    {
        // Up then down: 0,1,2,1,0,1,2,1,...
        int cycleLength = count > 1 ? (count - 1) * 2 : 1;
        int pos = _stepIndex % cycleLength;
        return pos < count ? pos : cycleLength - pos;
    }

    private static int[] BuildDownPattern(int count)
    {
        // root→last→...→second→root = [0, count-1, count-2, ..., 1]
        var pattern = new int[count];
        pattern[0] = 0;
        for (int i = 1; i < count; i++)
            pattern[i] = count - i;
        return pattern;
    }
}

/// <summary>
/// Arpeggio playback direction patterns.
/// </summary>
public enum ArpeggioPattern
{
    Up,
    Down,
    UpDown,
    Random
}
