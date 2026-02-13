namespace SqncR.Theory.Tests;

public class IntervalTests
{
    [Theory]
    [InlineData(0, "Unison")]
    [InlineData(1, "Minor Second")]
    [InlineData(2, "Major Second")]
    [InlineData(3, "Minor Third")]
    [InlineData(4, "Major Third")]
    [InlineData(5, "Perfect Fourth")]
    [InlineData(6, "Tritone")]
    [InlineData(7, "Perfect Fifth")]
    [InlineData(8, "Minor Sixth")]
    [InlineData(9, "Major Sixth")]
    [InlineData(10, "Minor Seventh")]
    [InlineData(11, "Major Seventh")]
    public void GetName_ReturnsCorrectName(int semitones, string expected)
    {
        Assert.Equal(expected, Interval.GetName(semitones));
    }

    [Fact]
    public void Constants_HaveCorrectSemitoneValues()
    {
        Assert.Equal(0, Interval.Unison);
        Assert.Equal(1, Interval.MinorSecond);
        Assert.Equal(2, Interval.MajorSecond);
        Assert.Equal(3, Interval.MinorThird);
        Assert.Equal(4, Interval.MajorThird);
        Assert.Equal(5, Interval.PerfectFourth);
        Assert.Equal(6, Interval.Tritone);
        Assert.Equal(7, Interval.PerfectFifth);
        Assert.Equal(8, Interval.MinorSixth);
        Assert.Equal(9, Interval.MajorSixth);
        Assert.Equal(10, Interval.MinorSeventh);
        Assert.Equal(11, Interval.MajorSeventh);
        Assert.Equal(12, Interval.Octave);
    }

    [Theory]
    [InlineData(12, "Unison")]   // Octave wraps
    [InlineData(13, "Minor Second")]
    [InlineData(24, "Unison")]
    public void GetName_WrapsAtOctave(int semitones, string expected)
    {
        Assert.Equal(expected, Interval.GetName(semitones));
    }
}
