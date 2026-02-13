using SqncR.Testing.Audio;

namespace SqncR.Testing.Tests;

public class MidiFrequencyTests
{
    [Fact]
    public void A4_MidiNote69_Returns440Hz()
    {
        var frequency = MidiFrequency.MidiToFrequency(69);
        Assert.Equal(440.0, frequency, precision: 2);
    }

    [Fact]
    public void C4_MidiNote60_Returns261_63Hz()
    {
        var frequency = MidiFrequency.MidiToFrequency(60);
        Assert.Equal(261.63, frequency, precision: 1);
    }

    [Fact]
    public void RoundTrip_MidiToFrequencyToMidi()
    {
        for (var midiNote = 21; midiNote <= 108; midiNote++)
        {
            var frequency = MidiFrequency.MidiToFrequency(midiNote);
            var roundTripped = MidiFrequency.FrequencyToMidi(frequency);
            Assert.Equal(midiNote, roundTripped);
        }
    }

    [Fact]
    public void MidiToNoteName_CorrectNames()
    {
        Assert.Equal("C4", MidiFrequency.MidiToNoteName(60));
        Assert.Equal("A4", MidiFrequency.MidiToNoteName(69));
        Assert.Equal("C#4", MidiFrequency.MidiToNoteName(61));
        Assert.Equal("B3", MidiFrequency.MidiToNoteName(59));
        Assert.Equal("G4", MidiFrequency.MidiToNoteName(67));
    }

    [Fact]
    public void FrequencyToNoteName_440Hz_ReturnsA4()
    {
        Assert.Equal("A4", MidiFrequency.FrequencyToNoteName(440.0));
    }

    [Fact]
    public void FrequencyToMidi_NegativeHz_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MidiFrequency.FrequencyToMidi(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => MidiFrequency.FrequencyToMidi(0));
    }

    [Fact]
    public void MiddleC_MidiNote60_OctaveIsCorrect()
    {
        // MIDI note 0 should be C-1 (by convention)
        Assert.Equal("C-1", MidiFrequency.MidiToNoteName(0));
        Assert.Equal("A-1", MidiFrequency.MidiToNoteName(9));
    }
}
