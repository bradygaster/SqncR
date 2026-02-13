namespace SqncR.Theory.Tests;

public class ModeTests
{
    // --- Mode interval verification ---
    // Reference: each mode is a rotation of [0,2,4,5,7,9,11]

    [Fact]
    public void Ionian_HasMajorScaleIntervals()
    {
        var scale = Mode.Ionian(60);
        Assert.Equal([0, 2, 4, 5, 7, 9, 11], scale.Intervals);
        Assert.Equal("Ionian", scale.Name);
    }

    [Fact]
    public void Dorian_HasCorrectIntervals()
    {
        // Dorian: W H W W W H W → 0 2 3 5 7 9 10
        var scale = Mode.Dorian(62); // D
        Assert.Equal([0, 2, 3, 5, 7, 9, 10], scale.Intervals);
        Assert.Equal("Dorian", scale.Name);
    }

    [Fact]
    public void Phrygian_HasCorrectIntervals()
    {
        // Phrygian: H W W W H W W → 0 1 3 5 7 8 10
        var scale = Mode.Phrygian(64); // E
        Assert.Equal([0, 1, 3, 5, 7, 8, 10], scale.Intervals);
    }

    [Fact]
    public void Lydian_HasCorrectIntervals()
    {
        // Lydian: W W W H W W H → 0 2 4 6 7 9 11
        var scale = Mode.Lydian(65); // F
        Assert.Equal([0, 2, 4, 6, 7, 9, 11], scale.Intervals);
    }

    [Fact]
    public void Mixolydian_HasCorrectIntervals()
    {
        // Mixolydian: W W H W W H W → 0 2 4 5 7 9 10
        var scale = Mode.Mixolydian(67); // G
        Assert.Equal([0, 2, 4, 5, 7, 9, 10], scale.Intervals);
    }

    [Fact]
    public void Aeolian_EqualsNaturalMinor()
    {
        // Aeolian: W H W W H W W → 0 2 3 5 7 8 10
        var aeolian = Mode.Aeolian(69); // A
        var minor = Scale.Minor(69);
        Assert.Equal(minor.Intervals, aeolian.Intervals);
    }

    [Fact]
    public void Locrian_HasCorrectIntervals()
    {
        // Locrian: H W W H W W W → 0 1 3 5 6 8 10
        var scale = Mode.Locrian(71); // B
        Assert.Equal([0, 1, 3, 5, 6, 8, 10], scale.Intervals);
    }

    // --- Mode rotation produces correct notes ---
    [Fact]
    public void Dorian_OfD_ContainsSameNotesAsCMajor()
    {
        // D Dorian should contain exactly the white keys: C D E F G A B
        // Pitch classes: 2, 4, 5, 7, 9, 11, 0 (rooted on D=2)
        var dorian = Mode.Dorian(62); // D4

        // Verify it contains D E F G A B C
        Assert.True(dorian.ContainsNote(62));  // D
        Assert.True(dorian.ContainsNote(64));  // E
        Assert.True(dorian.ContainsNote(65));  // F
        Assert.True(dorian.ContainsNote(67));  // G
        Assert.True(dorian.ContainsNote(69));  // A
        Assert.True(dorian.ContainsNote(71));  // B
        Assert.True(dorian.ContainsNote(60));  // C

        // And does NOT contain sharps/flats
        Assert.False(dorian.ContainsNote(61)); // C#
        Assert.False(dorian.ContainsNote(63)); // Eb
        Assert.False(dorian.ContainsNote(66)); // F#
        Assert.False(dorian.ContainsNote(68)); // Ab
        Assert.False(dorian.ContainsNote(70)); // Bb
    }

    [Fact]
    public void Phrygian_OfE_ContainsSameNotesAsCMajor()
    {
        var phrygian = Mode.Phrygian(64); // E4
        // E Phrygian = E F G A B C D (all white keys, rooted on E)
        Assert.True(phrygian.ContainsNote(64));  // E
        Assert.True(phrygian.ContainsNote(65));  // F
        Assert.True(phrygian.ContainsNote(67));  // G
        Assert.True(phrygian.ContainsNote(69));  // A
        Assert.True(phrygian.ContainsNote(71));  // B
        Assert.True(phrygian.ContainsNote(60));  // C
        Assert.True(phrygian.ContainsNote(62));  // D
    }

    // --- Factory method parity ---
    [Fact]
    public void FromMajorScale_MatchesNamedMethods()
    {
        var root = 60;
        Assert.Equal(Mode.Ionian(root).Intervals, Mode.FromMajorScale(root, 0).Intervals);
        Assert.Equal(Mode.Dorian(root).Intervals, Mode.FromMajorScale(root, 1).Intervals);
        Assert.Equal(Mode.Phrygian(root).Intervals, Mode.FromMajorScale(root, 2).Intervals);
        Assert.Equal(Mode.Lydian(root).Intervals, Mode.FromMajorScale(root, 3).Intervals);
        Assert.Equal(Mode.Mixolydian(root).Intervals, Mode.FromMajorScale(root, 4).Intervals);
        Assert.Equal(Mode.Aeolian(root).Intervals, Mode.FromMajorScale(root, 5).Intervals);
        Assert.Equal(Mode.Locrian(root).Intervals, Mode.FromMajorScale(root, 6).Intervals);
    }

    [Fact]
    public void FromMajorScale_InvalidIndex_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Mode.FromMajorScale(60, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Mode.FromMajorScale(60, 7));
    }

    [Fact]
    public void AllNames_Returns7Modes()
    {
        Assert.Equal(7, Mode.AllNames.Count);
        Assert.Equal("Ionian", Mode.AllNames[0]);
        Assert.Equal("Locrian", Mode.AllNames[6]);
    }

    // --- All modes have 7 notes ---
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void AllModes_Have7Intervals(int modeIndex)
    {
        var scale = Mode.FromMajorScale(60, modeIndex);
        Assert.Equal(7, scale.Intervals.Count);
        Assert.Equal(0, scale.Intervals[0]); // All start with unison
    }

    // --- Cross-octave mode verification ---
    [Fact]
    public void Dorian_NotesInOctave3_AreCorrect()
    {
        var dorian = Mode.Dorian(50); // D3 = 50
        var notes = dorian.GetNotesInOctave(3);
        // D3=50, E3=52, F3=53, G3=55, A3=57, B3=59, C4=60
        Assert.Equal([50, 52, 53, 55, 57, 59, 60], notes);
    }
}
