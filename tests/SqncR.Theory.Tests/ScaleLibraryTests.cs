namespace SqncR.Theory.Tests;

public class ScaleLibraryTests
{
    [Theory]
    [InlineData("Major")]
    [InlineData("Natural Minor")]
    [InlineData("Minor")]
    [InlineData("Harmonic Minor")]
    [InlineData("Melodic Minor")]
    [InlineData("Pentatonic Major")]
    [InlineData("Pentatonic Minor")]
    [InlineData("Blues")]
    [InlineData("Whole Tone")]
    [InlineData("Diminished")]
    [InlineData("Chromatic")]
    [InlineData("Ionian")]
    [InlineData("Dorian")]
    [InlineData("Phrygian")]
    [InlineData("Lydian")]
    [InlineData("Mixolydian")]
    [InlineData("Aeolian")]
    [InlineData("Locrian")]
    public void Get_AllRegisteredScales_Succeed(string name)
    {
        var scale = ScaleLibrary.Get(name, 60);
        Assert.NotNull(scale);
        Assert.Equal(60, scale.RootNote);
    }

    [Fact]
    public void Get_CaseInsensitive()
    {
        var lower = ScaleLibrary.Get("major", 60);
        var upper = ScaleLibrary.Get("MAJOR", 60);
        Assert.Equal(lower.Intervals, upper.Intervals);
    }

    [Fact]
    public void Get_UnknownScale_Throws()
    {
        Assert.Throws<ArgumentException>(() => ScaleLibrary.Get("Nonexistent", 60));
    }

    [Fact]
    public void Exists_RegisteredScale_ReturnsTrue()
    {
        Assert.True(ScaleLibrary.Exists("Major"));
        Assert.True(ScaleLibrary.Exists("Dorian"));
        Assert.True(ScaleLibrary.Exists("Blues"));
    }

    [Fact]
    public void Exists_UnknownScale_ReturnsFalse()
    {
        Assert.False(ScaleLibrary.Exists("BebopDominant"));
    }

    [Fact]
    public void AvailableScales_ContainsAtLeast18Entries()
    {
        // 11 scales + 7 modes + "Minor" alias = 19
        Assert.True(ScaleLibrary.AvailableScales.Count >= 18);
    }

    [Fact]
    public void Get_Major_MatchesScaleFactory()
    {
        var fromLibrary = ScaleLibrary.Get("Major", 60);
        var fromFactory = Scale.Major(60);
        Assert.Equal(fromFactory.Intervals, fromLibrary.Intervals);
    }

    [Fact]
    public void Get_Dorian_MatchesModeFactory()
    {
        var fromLibrary = ScaleLibrary.Get("Dorian", 62);
        var fromFactory = Mode.Dorian(62);
        Assert.Equal(fromFactory.Intervals, fromLibrary.Intervals);
    }

    [Fact]
    public void Get_Minor_IsAliasForNaturalMinor()
    {
        var minor = ScaleLibrary.Get("Minor", 69);
        var naturalMinor = ScaleLibrary.Get("Natural Minor", 69);
        Assert.Equal(naturalMinor.Intervals, minor.Intervals);
    }
}
