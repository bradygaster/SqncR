using SqncR.Core.Rhythm;

namespace SqncR.Core.Tests.Rhythm;

public class PatternLibraryTests
{
    [Theory]
    [InlineData("rock")]
    [InlineData("house")]
    [InlineData("hip-hop")]
    [InlineData("jazz")]
    [InlineData("ambient")]
    public void Get_ReturnsValidPattern(string name)
    {
        var pattern = PatternLibrary.Get(name);

        Assert.NotNull(pattern);
        Assert.Equal(name, pattern.Name);
        Assert.NotEmpty(pattern.Layers);
    }

    [Fact]
    public void Get_CaseInsensitive()
    {
        var lower = PatternLibrary.Get("house");
        var upper = PatternLibrary.Get("HOUSE");

        Assert.Equal(lower.Name, upper.Name);
    }

    [Fact]
    public void Get_UnknownPattern_Throws()
    {
        Assert.Throws<ArgumentException>(() => PatternLibrary.Get("polka"));
    }

    [Fact]
    public void House_HasFourOnTheFloorKick()
    {
        var house = PatternLibrary.Get("house");
        var kickLayer = house.Layers.First(l => l.Voice == DrumVoice.Kick);
        var activeSteps = kickLayer.Pattern.GetActiveSteps();

        Assert.Equal([0, 4, 8, 12], activeSteps);
    }

    [Fact]
    public void Rock_HasBackbeatSnare()
    {
        var rock = PatternLibrary.Get("rock");
        var snareLayer = rock.Layers.First(l => l.Voice == DrumVoice.Snare);
        var activeSteps = snareLayer.Pattern.GetActiveSteps();

        Assert.Equal([4, 12], activeSteps);
    }

    [Fact]
    public void AllPatterns_CanLoadIntoSequencer()
    {
        foreach (var name in PatternLibrary.Names)
        {
            var pattern = PatternLibrary.Get(name);
            var seq = pattern.ToSequencer();
            var events = seq.GetMeasureEvents();

            Assert.NotEmpty(events);
        }
    }

    [Fact]
    public void Names_ContainsAllFivePatterns()
    {
        var names = PatternLibrary.Names.ToList();
        Assert.Contains("rock", names);
        Assert.Contains("house", names);
        Assert.Contains("hip-hop", names);
        Assert.Contains("jazz", names);
        Assert.Contains("ambient", names);
    }
}
