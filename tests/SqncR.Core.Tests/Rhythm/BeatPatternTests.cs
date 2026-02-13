using SqncR.Core.Rhythm;

namespace SqncR.Core.Tests.Rhythm;

public class BeatPatternTests
{
    [Fact]
    public void FourOnTheFloor_HasKicksOnQuarterNotes()
    {
        var pattern = BeatPattern.FourOnTheFloor();
        var active = pattern.GetActiveSteps();

        Assert.Equal(16, pattern.StepsPerMeasure);
        Assert.Equal([0, 4, 8, 12], active);
    }

    [Fact]
    public void FourOnTheFloor_HasCorrectVelocity()
    {
        var pattern = BeatPattern.FourOnTheFloor();

        Assert.Equal(110, pattern.Steps[0].Velocity);
        Assert.Equal(110, pattern.Steps[4].Velocity);
    }

    [Fact]
    public void HalfTime_KickOnOneSnareOnThree()
    {
        var pattern = BeatPattern.HalfTime();
        var active = pattern.GetActiveSteps();

        Assert.Equal([0, 8], active);
        Assert.Equal(110, pattern.Steps[0].Velocity);  // kick
        Assert.Equal(100, pattern.Steps[8].Velocity);   // snare
    }

    [Fact]
    public void OffBeat_HitsEvenSteps()
    {
        var pattern = BeatPattern.OffBeat();
        var active = pattern.GetActiveSteps();

        Assert.Equal([1, 3, 5, 7, 9, 11, 13, 15], active);
    }

    [Fact]
    public void Straight_AllStepsActive()
    {
        var pattern = BeatPattern.Straight();

        Assert.Equal(16, pattern.GetActiveSteps().Count);
        Assert.All(pattern.Steps, step => Assert.True(step.IsActive));
    }

    [Fact]
    public void Backbeat_HitsOnTwoAndFour()
    {
        var pattern = BeatPattern.Backbeat();
        var active = pattern.GetActiveSteps();

        Assert.Equal([4, 12], active);
    }

    [Fact]
    public void WithStep_ReturnsNewPatternWithModifiedStep()
    {
        var original = BeatPattern.FourOnTheFloor();
        var modified = original.WithStep(2, StepInfo.Hit(90));

        Assert.False(original.Steps[2].IsActive);
        Assert.True(modified.Steps[2].IsActive);
        Assert.Equal(90, modified.Steps[2].Velocity);
    }

    [Fact]
    public void Constructor_ThrowsOnMismatchedStepCount()
    {
        var steps = new StepInfo[8];
        Assert.Throws<ArgumentException>(() => new BeatPattern(16, steps));
    }

    [Fact]
    public void Constructor_ThrowsOnZeroSteps()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BeatPattern(0, Array.Empty<StepInfo>()));
    }

    [Fact]
    public void FourOnTheFloor_Custom8Steps()
    {
        var pattern = BeatPattern.FourOnTheFloor(8);
        var active = pattern.GetActiveSteps();

        Assert.Equal(8, pattern.StepsPerMeasure);
        Assert.Equal([0, 2, 4, 6], active);
    }

    [Fact]
    public void GetActiveSteps_EmptyPattern_ReturnsEmpty()
    {
        var steps = new StepInfo[4];
        for (int i = 0; i < 4; i++) steps[i] = StepInfo.Rest();
        var pattern = new BeatPattern(4, steps);

        Assert.Empty(pattern.GetActiveSteps());
    }
}
