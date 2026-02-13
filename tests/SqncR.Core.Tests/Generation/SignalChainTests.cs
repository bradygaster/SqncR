using SqncR.Core.Generation;
using SqncR.Core.Instruments;
using SqncR.Midi.Testing;
using Microsoft.Extensions.Logging.Abstractions;

namespace SqncR.Core.Tests.Generation;

public class SignalChainTests : IDisposable
{
    private readonly MockMidiOutput _midi = new();

    public void Dispose() => _midi.Dispose();

    private static Instrument MakeInstrument(string id, string name, InstrumentRole role, int channel) =>
        new()
        {
            Id = id,
            Name = name,
            Role = role,
            MidiChannel = channel,
            Type = InstrumentType.Hardware,
        };

    private static GenerationEngine CreateEngine(GenerationState state, IMidiOutput midi) =>
        new(state, midi, NullLogger<GenerationEngine>.Instance);

    private async Task<IReadOnlyList<CapturedMidiEvent>> RunEngine(
        GenerationState state, int durationMs)
    {
        using var engine = CreateEngine(state, _midi);
        await engine.StartAsync(CancellationToken.None);
        await engine.Commands.WriteAsync(new GenerationCommand.Start());
        await Task.Delay(durationMs);
        await engine.Commands.WriteAsync(new GenerationCommand.Stop());
        await Task.Delay(50); // let stop process
        await engine.StopAsync(CancellationToken.None);
        return _midi.Events;
    }

    // --- Test 1: Three Instruments Get Correct Channels ---

    [Fact]
    public async Task ThreeInstruments_GetCorrectChannels()
    {
        var state = new GenerationState { Tempo = 240 };
        state.Instruments.Add(MakeInstrument("bass", "Bass Synth", InstrumentRole.Bass, 1));
        state.Instruments.Add(MakeInstrument("pad", "Pad Synth", InstrumentRole.Pad, 2));
        state.Instruments.Add(MakeInstrument("lead", "Lead Synth", InstrumentRole.Lead, 3));

        var events = await RunEngine(state, 2000);

        var noteOns = events.Where(e => e.Type == MidiEventType.NoteOn).ToList();
        var channelsUsed = noteOns.Select(e => e.Channel).Distinct().ToHashSet();

        Assert.Contains(1, channelsUsed);
        Assert.Contains(2, channelsUsed);
        Assert.Contains(3, channelsUsed);

        // No notes on other channels (except potential AllNotesOff on melodic/drum defaults)
        var unexpectedChannels = noteOns
            .Where(e => e.Channel != 1 && e.Channel != 2 && e.Channel != 3)
            .Select(e => e.Channel)
            .Distinct()
            .ToList();
        Assert.Empty(unexpectedChannels);
    }

    // --- Test 2: Bass Gets Low Notes ---

    [Fact]
    public async Task BassInstrument_GetsLowNotes()
    {
        var state = new GenerationState { Tempo = 240 };
        state.Instruments.Add(MakeInstrument("bass", "Bass Synth", InstrumentRole.Bass, 1));

        var events = await RunEngine(state, 1500);

        var bassNotes = events
            .Where(e => e.Type == MidiEventType.NoteOn && e.Channel == 1)
            .Select(e => e.Note)
            .ToList();

        Assert.NotEmpty(bassNotes);
        Assert.All(bassNotes, note => Assert.True(note < 60,
            $"Bass note {note} should be < 60 (below middle C)"));
    }

    // --- Test 3: Lead Gets High Notes ---

    [Fact]
    public async Task LeadInstrument_GetsHighNotes()
    {
        var state = new GenerationState { Tempo = 240 };
        state.Instruments.Add(MakeInstrument("lead", "Lead Synth", InstrumentRole.Lead, 5));

        var events = await RunEngine(state, 1500);

        var leadNotes = events
            .Where(e => e.Type == MidiEventType.NoteOn && e.Channel == 5)
            .Select(e => e.Note)
            .ToList();

        Assert.NotEmpty(leadNotes);

        int highNoteCount = leadNotes.Count(n => n >= 60);
        double highRatio = (double)highNoteCount / leadNotes.Count;
        Assert.True(highRatio >= 0.8,
            $"Expected majority (≥80%) of lead notes ≥ 60, got {highRatio:P0} ({highNoteCount}/{leadNotes.Count})");
    }

    // --- Test 4: Channel Isolation — No Crosstalk ---

    [Fact]
    public async Task ChannelIsolation_NoCrosstalk()
    {
        var state = new GenerationState { Tempo = 240 };
        state.Instruments.Add(MakeInstrument("bass", "Bass Synth", InstrumentRole.Bass, 1));
        state.Instruments.Add(MakeInstrument("pad", "Pad Synth", InstrumentRole.Pad, 2));

        var events = await RunEngine(state, 1500);

        var ch1Notes = events
            .Where(e => e.Type == MidiEventType.NoteOn && e.Channel == 1)
            .Select(e => e.Note)
            .ToList();

        var ch2Notes = events
            .Where(e => e.Type == MidiEventType.NoteOn && e.Channel == 2)
            .Select(e => e.Note)
            .ToList();

        Assert.NotEmpty(ch1Notes);
        Assert.NotEmpty(ch2Notes);

        // Channel 1 notes should NOT appear on channel 2 and vice versa
        // (same note number on different channels is fine — we check the events are separated)
        var ch1Set = events
            .Where(e => e.Type == MidiEventType.NoteOn && e.Channel == 1)
            .Select(e => (e.Channel, e.Note, e.Timestamp))
            .ToList();
        var ch2Set = events
            .Where(e => e.Type == MidiEventType.NoteOn && e.Channel == 2)
            .Select(e => (e.Channel, e.Note, e.Timestamp))
            .ToList();

        // Each event is tagged with the correct channel — no event has the wrong channel
        Assert.All(ch1Set, e => Assert.Equal(1, e.Channel));
        Assert.All(ch2Set, e => Assert.Equal(2, e.Channel));

        // Bass notes (ch1) should be predominantly low, pad notes (ch2) should be higher
        double avgCh1 = ch1Notes.Average();
        double avgCh2 = ch2Notes.Average();
        Assert.True(avgCh1 < avgCh2,
            $"Bass avg ({avgCh1:F0}) should be lower than pad avg ({avgCh2:F0})");
    }

    // --- Test 5: Add Instrument Mid-Session ---

    [Fact]
    public async Task AddInstrumentMidSession_NotesAppearAfterAddition()
    {
        var state = new GenerationState { Tempo = 240 };
        state.Instruments.Add(MakeInstrument("bass", "Bass Synth", InstrumentRole.Bass, 1));

        using var engine = CreateEngine(state, _midi);
        await engine.StartAsync(CancellationToken.None);
        await engine.Commands.WriteAsync(new GenerationCommand.Start());

        // Run with bass only for 500ms
        await Task.Delay(500);
        var preAddTimestamp = _midi.Events.LastOrDefault()?.Timestamp ?? TimeSpan.Zero;

        // Add lead instrument
        await engine.Commands.WriteAsync(
            new GenerationCommand.AddInstrument(
                MakeInstrument("lead", "Lead Synth", InstrumentRole.Lead, 5)));

        // Run for 500ms more
        await Task.Delay(500);

        await engine.Commands.WriteAsync(new GenerationCommand.Stop());
        await Task.Delay(50);
        await engine.StopAsync(CancellationToken.None);

        var events = _midi.Events;

        // Lead notes (ch 5) should appear only after the addition point
        var leadNotes = events
            .Where(e => e.Type == MidiEventType.NoteOn && e.Channel == 5)
            .ToList();

        Assert.NotEmpty(leadNotes);

        // All lead notes should have timestamps after preAddTimestamp
        Assert.All(leadNotes, e => Assert.True(e.Timestamp >= preAddTimestamp,
            $"Lead note at {e.Timestamp} appeared before add at {preAddTimestamp}"));
    }

    // --- Test 6: Remove Instrument Mid-Session ---

    [Fact]
    public async Task RemoveInstrumentMidSession_NotesStopAfterRemoval()
    {
        var state = new GenerationState { Tempo = 240 };
        state.Instruments.Add(MakeInstrument("bass", "Bass Synth", InstrumentRole.Bass, 1));
        state.Instruments.Add(MakeInstrument("lead", "Lead Synth", InstrumentRole.Lead, 5));

        using var engine = CreateEngine(state, _midi);
        await engine.StartAsync(CancellationToken.None);
        await engine.Commands.WriteAsync(new GenerationCommand.Start());

        // Run with both for 500ms
        await Task.Delay(500);

        // Remove lead
        await engine.Commands.WriteAsync(
            new GenerationCommand.RemoveInstrument("lead"));

        // Mark the removal time
        await Task.Delay(50); // let command process
        var removalTimestamp = _midi.Events.LastOrDefault()?.Timestamp ?? TimeSpan.Zero;

        // Run for 500ms more
        await Task.Delay(500);

        await engine.Commands.WriteAsync(new GenerationCommand.Stop());
        await Task.Delay(50);
        await engine.StopAsync(CancellationToken.None);

        var events = _midi.Events;

        // Lead notes should exist before removal
        var leadNotesBefore = events
            .Where(e => e.Type == MidiEventType.NoteOn && e.Channel == 5
                        && e.Timestamp <= removalTimestamp)
            .ToList();
        Assert.NotEmpty(leadNotesBefore);

        // No new NoteOn events on lead channel after removal
        var leadNotesAfter = events
            .Where(e => e.Type == MidiEventType.NoteOn && e.Channel == 5
                        && e.Timestamp > removalTimestamp)
            .ToList();
        Assert.Empty(leadNotesAfter);
    }

    // --- Test 7: All Instruments Silent After Stop ---

    [Fact]
    public async Task AllInstrumentsSilentAfterStop()
    {
        var state = new GenerationState { Tempo = 240 };
        state.Instruments.Add(MakeInstrument("bass", "Bass Synth", InstrumentRole.Bass, 1));
        state.Instruments.Add(MakeInstrument("pad", "Pad Synth", InstrumentRole.Pad, 2));
        state.Instruments.Add(MakeInstrument("lead", "Lead Synth", InstrumentRole.Lead, 3));

        using var engine = CreateEngine(state, _midi);
        await engine.StartAsync(CancellationToken.None);
        await engine.Commands.WriteAsync(new GenerationCommand.Start());
        await Task.Delay(500);

        // Stop playback
        await engine.Commands.WriteAsync(new GenerationCommand.Stop());
        await Task.Delay(100); // let stop process

        // Record the stop point
        var stopTimestamp = _midi.Events.LastOrDefault()?.Timestamp ?? TimeSpan.Zero;

        // Wait a bit more to confirm silence
        await Task.Delay(300);

        await engine.StopAsync(CancellationToken.None);

        // No NoteOn events after stop
        var notesAfterStop = _midi.Events
            .Where(e => e.Type == MidiEventType.NoteOn && e.Timestamp > stopTimestamp)
            .ToList();
        Assert.Empty(notesAfterStop);
    }
}
