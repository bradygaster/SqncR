using SqncR.Core.Rhythm;

namespace SqncR.Core.Tests.Rhythm;

public class SwingProfileTests
{
    [Fact]
    public void Straight_NoOffset()
    {
        var swing = SwingProfile.Straight;

        // Even step stays put
        Assert.Equal(0, swing.ApplySwing(0, 0, 120));
        // Odd step also stays put with 0 swing
        Assert.Equal(120, swing.ApplySwing(1, 120, 120));
    }

    [Fact]
    public void FullSwing_OddStepsDelayed()
    {
        var swing = SwingProfile.Full;
        int ticksPerStep = 120;

        // Step 0 (even) — no offset
        Assert.Equal(0, swing.ApplySwing(0, 0, ticksPerStep));

        // Step 1 (odd) — full triplet offset = 120/3 = 40 ticks late
        long adjusted = swing.ApplySwing(1, 120, ticksPerStep);
        Assert.Equal(160, adjusted);
    }

    [Fact]
    public void MediumSwing_HalfOffset()
    {
        var swing = SwingProfile.Medium;
        int ticksPerStep = 120;

        // Step 1: half of max offset (120/3 * 0.5 = 20)
        long adjusted = swing.ApplySwing(1, 120, ticksPerStep);
        Assert.Equal(140, adjusted);
    }

    [Fact]
    public void EvenSteps_NeverSwung()
    {
        var swing = SwingProfile.Full;
        int ticksPerStep = 120;

        // Steps 0, 2, 4 should never be offset
        Assert.Equal(0, swing.ApplySwing(0, 0, ticksPerStep));
        Assert.Equal(240, swing.ApplySwing(2, 240, ticksPerStep));
        Assert.Equal(480, swing.ApplySwing(4, 480, ticksPerStep));
    }

    [Fact]
    public void SwingAmount_ClampedToRange()
    {
        var over = new SwingProfile(2.0);
        Assert.Equal(1.0, over.SwingAmount);

        var under = new SwingProfile(-0.5);
        Assert.Equal(0.0, under.SwingAmount);
    }
}
