namespace SqncR.Theory;

/// <summary>
/// Registry of common scales and modes. Provides name-based lookup.
/// All names are case-insensitive.
/// </summary>
public static class ScaleLibrary
{
    private static readonly Dictionary<string, Func<int, Scale>> Registry =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Diatonic scales
            ["Major"] = Scale.Major,
            ["Natural Minor"] = Scale.Minor,
            ["Minor"] = Scale.Minor,
            ["Harmonic Minor"] = Scale.HarmonicMinor,
            ["Melodic Minor"] = Scale.MelodicMinor,

            // Pentatonic / Blues
            ["Pentatonic Major"] = Scale.PentatonicMajor,
            ["Pentatonic Minor"] = Scale.PentatonicMinor,
            ["Blues"] = Scale.Blues,

            // Symmetric scales
            ["Whole Tone"] = Scale.WholeTone,
            ["Diminished"] = Scale.DiminishedHalfWhole,
            ["Chromatic"] = Scale.Chromatic,

            // Modes
            ["Ionian"] = Mode.Ionian,
            ["Dorian"] = Mode.Dorian,
            ["Phrygian"] = Mode.Phrygian,
            ["Lydian"] = Mode.Lydian,
            ["Mixolydian"] = Mode.Mixolydian,
            ["Aeolian"] = Mode.Aeolian,
            ["Locrian"] = Mode.Locrian,
        };

    /// <summary>
    /// Retrieves a scale by name and root note.
    /// </summary>
    /// <param name="name">Scale or mode name (case-insensitive), e.g. "Major", "Dorian", "Blues".</param>
    /// <param name="rootNote">MIDI note number for the root.</param>
    /// <returns>A Scale instance.</returns>
    /// <exception cref="ArgumentException">If the scale name is not recognized.</exception>
    public static Scale Get(string name, int rootNote)
    {
        if (!Registry.TryGetValue(name, out var factory))
            throw new ArgumentException($"Unknown scale: '{name}'. Available: {string.Join(", ", Registry.Keys)}");

        return factory(rootNote);
    }

    /// <summary>
    /// Returns true if the given scale name is registered.
    /// </summary>
    public static bool Exists(string name) => Registry.ContainsKey(name);

    /// <summary>
    /// Returns all registered scale/mode names.
    /// </summary>
    public static IReadOnlyCollection<string> AvailableScales => Registry.Keys;
}
