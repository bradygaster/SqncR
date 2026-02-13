namespace SqncR.SonicPi.Tests;

public class SonicPiInstrumentTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var instrument = new SonicPiInstrument("lead", "prophet", 2);

        Assert.Equal("lead", instrument.Name);
        Assert.Equal("prophet", instrument.SynthName);
        Assert.Equal(2, instrument.Channel);
        Assert.False(instrument.IsActive);
        Assert.Empty(instrument.FxChain);
    }

    [Fact]
    public void Constructor_DefaultChannel_Is1()
    {
        var instrument = new SonicPiInstrument("bass", "tb303");

        Assert.Equal(1, instrument.Channel);
    }

    [Fact]
    public void Constructor_NullOrEmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new SonicPiInstrument("", "prophet"));
        Assert.Throws<ArgumentNullException>(() => new SonicPiInstrument(null!, "prophet"));
    }

    [Fact]
    public void Constructor_NullOrEmptySynthName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new SonicPiInstrument("lead", ""));
        Assert.Throws<ArgumentNullException>(() => new SonicPiInstrument("lead", null!));
    }

    [Fact]
    public void Activate_NullClient_ThrowsArgumentNullException()
    {
        var instrument = new SonicPiInstrument("lead", "prophet");

        Assert.Throws<ArgumentNullException>(() => instrument.Activate(null!));
    }

    [Fact]
    public void Deactivate_NullClient_ThrowsArgumentNullException()
    {
        var instrument = new SonicPiInstrument("lead", "prophet");

        Assert.Throws<ArgumentNullException>(() => instrument.Deactivate(null!));
    }
}
