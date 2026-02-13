using SqncR.Core.Rhythm;

namespace SqncR.Core.Tests.Rhythm;

public class DrumMapTests
{
    [Fact]
    public void GeneralMidi_KickIs36()
    {
        Assert.Equal(36, DrumMap.GeneralMidi.GetMidiNote(DrumVoice.Kick));
    }

    [Fact]
    public void GeneralMidi_SnareIs38()
    {
        Assert.Equal(38, DrumMap.GeneralMidi.GetMidiNote(DrumVoice.Snare));
    }

    [Fact]
    public void GeneralMidi_ClosedHiHatIs42()
    {
        Assert.Equal(42, DrumMap.GeneralMidi.GetMidiNote(DrumVoice.ClosedHiHat));
    }

    [Fact]
    public void GeneralMidi_OpenHiHatIs46()
    {
        Assert.Equal(46, DrumMap.GeneralMidi.GetMidiNote(DrumVoice.OpenHiHat));
    }

    [Fact]
    public void GeneralMidi_CrashIs49()
    {
        Assert.Equal(49, DrumMap.GeneralMidi.GetMidiNote(DrumVoice.Crash));
    }

    [Fact]
    public void GeneralMidi_RideIs51()
    {
        Assert.Equal(51, DrumMap.GeneralMidi.GetMidiNote(DrumVoice.Ride));
    }

    [Fact]
    public void TR808_KickIs36()
    {
        Assert.Equal(36, DrumMap.TR808.GetMidiNote(DrumVoice.Kick));
    }

    [Fact]
    public void TR808_ContainsCowbell()
    {
        Assert.True(DrumMap.TR808.Contains(DrumVoice.Cowbell));
        Assert.Equal(56, DrumMap.TR808.GetMidiNote(DrumVoice.Cowbell));
    }

    [Fact]
    public void CustomMap_ReturnsCustomValues()
    {
        var custom = new DrumMap("test", new Dictionary<DrumVoice, int>
        {
            [DrumVoice.Kick] = 60,
            [DrumVoice.Snare] = 62
        });

        Assert.Equal(60, custom.GetMidiNote(DrumVoice.Kick));
        Assert.Equal(62, custom.GetMidiNote(DrumVoice.Snare));
    }

    [Fact]
    public void UnmappedVoice_Throws()
    {
        var sparse = new DrumMap("sparse", new Dictionary<DrumVoice, int>
        {
            [DrumVoice.Kick] = 36
        });

        Assert.Throws<ArgumentException>(() => sparse.GetMidiNote(DrumVoice.Crash));
    }
}
