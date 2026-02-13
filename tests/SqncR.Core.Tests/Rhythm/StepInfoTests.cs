using SqncR.Core.Rhythm;

namespace SqncR.Core.Tests.Rhythm;

public class StepInfoTests
{
    [Fact]
    public void Hit_ClampsVelocityTo127()
    {
        var step = StepInfo.Hit(200);
        Assert.Equal(127, step.Velocity);
        Assert.True(step.IsActive);
    }

    [Fact]
    public void Hit_ClampsProbabilityTo1()
    {
        var step = StepInfo.Hit(100, 1.5);
        Assert.Equal(1.0, step.Probability);
    }

    [Fact]
    public void Rest_IsInactiveWithZeroValues()
    {
        var step = StepInfo.Rest();
        Assert.False(step.IsActive);
        Assert.Equal(0, step.Velocity);
        Assert.Equal(0.0, step.Probability);
    }
}
