using System.Text.RegularExpressions;

namespace SqncR.Core;

public static class NoteParser
{
    private static readonly Dictionary<string, int> NoteOffsets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["C"] = 0,
        ["C#"] = 1, ["Db"] = 1,
        ["D"] = 2,
        ["D#"] = 3, ["Eb"] = 3,
        ["E"] = 4, ["Fb"] = 4,
        ["E#"] = 5, ["F"] = 5,
        ["F#"] = 6, ["Gb"] = 6,
        ["G"] = 7,
        ["G#"] = 8, ["Ab"] = 8,
        ["A"] = 9,
        ["A#"] = 10, ["Bb"] = 10,
        ["B"] = 11, ["Cb"] = 11,
        ["B#"] = 0
    };

    private static readonly Regex NoteRegex = new(@"^([A-Ga-g][#b]?)(-?\d+)$", RegexOptions.Compiled);

    /// <summary>
    /// Parse a note name like "C4", "F#2", "Bb5" to a MIDI number.
    /// C4 = 60, A4 = 69.
    /// </summary>
    public static int Parse(string noteName)
    {
        var match = NoteRegex.Match(noteName.Trim());
        if (!match.Success)
            throw new ArgumentException($"Invalid note name: '{noteName}'. Expected format like 'C4', 'F#2', 'Bb5'.");

        var notePart = match.Groups[1].Value;
        // Normalize: first letter uppercase, rest as-is
        notePart = char.ToUpper(notePart[0]) + notePart[1..];

        if (!NoteOffsets.TryGetValue(notePart, out var offset))
            throw new ArgumentException($"Unknown note: '{notePart}'");

        var octave = int.Parse(match.Groups[2].Value);

        // MIDI note = pitch class + (octave + 1) * 12
        // C4 = 0 + (4 + 1) * 12 = 60
        var midi = offset + ((octave + 1) * 12);

        if (midi < 0 || midi > 127)
            throw new ArgumentException($"Note '{noteName}' is outside MIDI range (0-127).");

        return midi;
    }

    /// <summary>
    /// Convert a MIDI number back to a note name.
    /// </summary>
    public static string ToNoteName(int midiNumber)
    {
        if (midiNumber < 0 || midiNumber > 127)
            throw new ArgumentOutOfRangeException(nameof(midiNumber), "Must be 0-127");

        var noteNames = new[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        var pitchClass = midiNumber % 12;
        var octave = (midiNumber / 12) - 1;

        return $"{noteNames[pitchClass]}{octave}";
    }
}
