namespace SqncR.Theory;

/// <summary>
/// The seven modes of the major scale, implemented as rotations of the major scale intervals.
/// Each mode starts from a different degree of the major scale.
/// </summary>
public static class Mode
{
    private static readonly string[] ModeNames =
    [
        "Ionian", "Dorian", "Phrygian", "Lydian",
        "Mixolydian", "Aeolian", "Locrian"
    ];

    private static readonly int[] MajorIntervals = [0, 2, 4, 5, 7, 9, 11];

    /// <summary>
    /// Creates a modal scale by rotating the major scale intervals.
    /// Mode index: 0=Ionian, 1=Dorian, 2=Phrygian, 3=Lydian, 4=Mixolydian, 5=Aeolian, 6=Locrian.
    /// </summary>
    /// <param name="root">MIDI note number for the root of the mode.</param>
    /// <param name="modeIndex">0-based mode index (0=Ionian through 6=Locrian).</param>
    public static Scale FromMajorScale(int root, int modeIndex)
    {
        if (modeIndex is < 0 or > 6)
            throw new ArgumentOutOfRangeException(nameof(modeIndex), "Must be 0–6.");

        var rotated = RotateIntervals(MajorIntervals, modeIndex);
        return new Scale(root, ModeNames[modeIndex], rotated);
    }

    public static Scale Ionian(int root) => FromMajorScale(root, 0);
    public static Scale Dorian(int root) => FromMajorScale(root, 1);
    public static Scale Phrygian(int root) => FromMajorScale(root, 2);
    public static Scale Lydian(int root) => FromMajorScale(root, 3);
    public static Scale Mixolydian(int root) => FromMajorScale(root, 4);
    public static Scale Aeolian(int root) => FromMajorScale(root, 5);
    public static Scale Locrian(int root) => FromMajorScale(root, 6);

    /// <summary>
    /// Returns all mode names in order (Ionian through Locrian).
    /// </summary>
    public static IReadOnlyList<string> AllNames => ModeNames;

    /// <summary>
    /// Rotates major scale intervals to produce a mode.
    /// E.g., Dorian (index 1): start from 2nd degree → intervals relative to new root.
    /// </summary>
    internal static int[] RotateIntervals(int[] intervals, int startDegree)
    {
        var len = intervals.Length;
        var rotated = new int[len];
        var offset = intervals[startDegree];

        for (var i = 0; i < len; i++)
        {
            rotated[i] = (intervals[(startDegree + i) % len] - offset + 12) % 12;
        }

        return rotated;
    }
}
