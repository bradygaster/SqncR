using SqncR.Midi;
using SqncR.Midi.Testing;

namespace SqncR.Midi.Tests;

public class MockMidiOutputTests
{
    [Fact]
    public void CapturesNoteOnEvent()
    {
        using var mock = new MockMidiOutput();

        mock.SendNoteOn(1, 60, 100);

        var evt = Assert.Single(mock.Events);
        Assert.Equal(MidiEventType.NoteOn, evt.Type);
        Assert.Equal(1, evt.Channel);
        Assert.Equal(60, evt.Note);
        Assert.Equal(100, evt.Velocity);
    }

    [Fact]
    public void CapturesNoteOffEvent()
    {
        using var mock = new MockMidiOutput();

        mock.SendNoteOff(2, 64);

        var evt = Assert.Single(mock.Events);
        Assert.Equal(MidiEventType.NoteOff, evt.Type);
        Assert.Equal(2, evt.Channel);
        Assert.Equal(64, evt.Note);
        Assert.Equal(0, evt.Velocity);
    }

    [Fact]
    public void CapturesAllNotesOffEvent()
    {
        using var mock = new MockMidiOutput();

        mock.AllNotesOff(3);

        var evt = Assert.Single(mock.Events);
        Assert.Equal(MidiEventType.AllNotesOff, evt.Type);
        Assert.Equal(3, evt.Channel);
    }

    [Fact]
    public void EventOrderingIsPreserved()
    {
        using var mock = new MockMidiOutput();

        mock.SendNoteOn(1, 60, 100);
        mock.SendNoteOn(1, 64, 90);
        mock.SendNoteOff(1, 60);
        mock.SendNoteOff(1, 64);

        Assert.Equal(4, mock.Events.Count);
        Assert.Equal(MidiEventType.NoteOn, mock.Events[0].Type);
        Assert.Equal(60, mock.Events[0].Note);
        Assert.Equal(MidiEventType.NoteOn, mock.Events[1].Type);
        Assert.Equal(64, mock.Events[1].Note);
        Assert.Equal(MidiEventType.NoteOff, mock.Events[2].Type);
        Assert.Equal(60, mock.Events[2].Note);
        Assert.Equal(MidiEventType.NoteOff, mock.Events[3].Type);
        Assert.Equal(64, mock.Events[3].Note);
    }

    [Fact]
    public async Task TimestampsAreRelativeToFirstEvent()
    {
        using var mock = new MockMidiOutput();

        mock.SendNoteOn(1, 60, 100);
        await Task.Delay(50);
        mock.SendNoteOff(1, 60);

        Assert.Equal(2, mock.Events.Count);
        // First event timestamp should be very close to zero
        Assert.True(mock.Events[0].Timestamp < TimeSpan.FromMilliseconds(5),
            $"First event timestamp {mock.Events[0].Timestamp} should be near zero");
        // Second event should be at least 40ms after first (allowing for scheduling jitter)
        Assert.True(mock.Events[1].Timestamp >= TimeSpan.FromMilliseconds(40),
            $"Second event timestamp {mock.Events[1].Timestamp} should be >= 40ms");
    }

    [Fact]
    public void ChannelSeparation_EventsOnDifferentChannels()
    {
        using var mock = new MockMidiOutput();

        mock.SendNoteOn(1, 60, 100);
        mock.SendNoteOn(2, 72, 80);
        mock.SendNoteOff(1, 60);
        mock.SendNoteOff(2, 72);

        var ch1Events = mock.Events.Where(e => e.Channel == 1).ToList();
        var ch2Events = mock.Events.Where(e => e.Channel == 2).ToList();

        Assert.Equal(2, ch1Events.Count);
        Assert.Equal(2, ch2Events.Count);

        Assert.Equal(60, ch1Events[0].Note);
        Assert.Equal(MidiEventType.NoteOn, ch1Events[0].Type);
        Assert.Equal(60, ch1Events[1].Note);
        Assert.Equal(MidiEventType.NoteOff, ch1Events[1].Type);

        Assert.Equal(72, ch2Events[0].Note);
        Assert.Equal(MidiEventType.NoteOn, ch2Events[0].Type);
        Assert.Equal(72, ch2Events[1].Note);
        Assert.Equal(MidiEventType.NoteOff, ch2Events[1].Type);
    }

    [Fact]
    public void ImplementsIMidiOutputInterface()
    {
        using IMidiOutput mock = new MockMidiOutput();

        mock.SendNoteOn(1, 60, 100);
        mock.SendNoteOff(1, 60);
        mock.AllNotesOff(1);

        Assert.Equal("MockDevice", mock.CurrentDeviceName);
    }

    [Fact]
    public void MidiServiceImplementsIMidiOutput()
    {
        // Compile-time proof: MidiService satisfies IMidiOutput
        Assert.True(typeof(IMidiOutput).IsAssignableFrom(typeof(MidiService)));
    }

    [Fact]
    public void ResetClearsEventsAndTimer()
    {
        using var mock = new MockMidiOutput();

        mock.SendNoteOn(1, 60, 100);
        Assert.Single(mock.Events);

        mock.Reset();

        Assert.Empty(mock.Events);

        // After reset, new events start from near-zero timestamp
        mock.SendNoteOn(1, 72, 90);
        Assert.Single(mock.Events);
        Assert.True(mock.Events[0].Timestamp < TimeSpan.FromMilliseconds(5));
    }

    [Fact]
    public void CurrentDeviceNameDefaultsToMockDevice()
    {
        using var mock = new MockMidiOutput();
        Assert.Equal("MockDevice", mock.CurrentDeviceName);
    }

    [Fact]
    public void CurrentDeviceNameIsSettable()
    {
        using var mock = new MockMidiOutput();
        mock.CurrentDeviceName = "TestSynth";
        Assert.Equal("TestSynth", mock.CurrentDeviceName);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(16, 127, 127)]
    [InlineData(1, 60, 100)]
    [InlineData(10, 36, 110)]
    public void CapturesVariousNoteOnParameters(int channel, int note, int velocity)
    {
        using var mock = new MockMidiOutput();
        mock.SendNoteOn(channel, note, velocity);

        var evt = Assert.Single(mock.Events);
        Assert.Equal(channel, evt.Channel);
        Assert.Equal(note, evt.Note);
        Assert.Equal(velocity, evt.Velocity);
    }

    [Fact]
    public async Task ThreadSafety_ConcurrentWritesAreCaptured()
    {
        using var mock = new MockMidiOutput();
        const int eventsPerThread = 100;
        const int threadCount = 4;

        var tasks = Enumerable.Range(0, threadCount).Select(threadIdx =>
            Task.Run(() =>
            {
                for (int i = 0; i < eventsPerThread; i++)
                {
                    mock.SendNoteOn(threadIdx + 1, i % 128, 80);
                }
            })).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(threadCount * eventsPerThread, mock.Events.Count);
    }

    [Fact]
    public void AllNotesOff_MultipleChannels()
    {
        using var mock = new MockMidiOutput();

        mock.AllNotesOff(1);
        mock.AllNotesOff(2);
        mock.AllNotesOff(10);

        Assert.Equal(3, mock.Events.Count);
        Assert.All(mock.Events, e => Assert.Equal(MidiEventType.AllNotesOff, e.Type));
        Assert.Equal(1, mock.Events[0].Channel);
        Assert.Equal(2, mock.Events[1].Channel);
        Assert.Equal(10, mock.Events[2].Channel);
    }
}
