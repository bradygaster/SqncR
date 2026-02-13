namespace SqncR.Theory;

/// <summary>
/// Named musical intervals expressed as semitone distances.
/// These are the building blocks for scales and chords.
/// </summary>
public static class Interval
{
    public const int Unison = 0;
    public const int MinorSecond = 1;
    public const int MajorSecond = 2;
    public const int MinorThird = 3;
    public const int MajorThird = 4;
    public const int PerfectFourth = 5;
    public const int Tritone = 6;
    public const int PerfectFifth = 7;
    public const int MinorSixth = 8;
    public const int MajorSixth = 9;
    public const int MinorSeventh = 10;
    public const int MajorSeventh = 11;
    public const int Octave = 12;

    /// <summary>
    /// Returns the name of an interval given a semitone count (0–12).
    /// </summary>
    public static string GetName(int semitones) => (semitones % 12) switch
    {
        0 => "Unison",
        1 => "Minor Second",
        2 => "Major Second",
        3 => "Minor Third",
        4 => "Major Third",
        5 => "Perfect Fourth",
        6 => "Tritone",
        7 => "Perfect Fifth",
        8 => "Minor Sixth",
        9 => "Major Sixth",
        10 => "Minor Seventh",
        11 => "Major Seventh",
        _ => "Unknown"
    };
}
