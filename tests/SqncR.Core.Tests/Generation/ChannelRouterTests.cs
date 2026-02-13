using SqncR.Core.Generation;
using SqncR.Core.Instruments;
using SqncR.Theory;

namespace SqncR.Core.Tests.Generation;

public class ChannelRouterTests
{
    private static GenerationState CreateState(Scale? scale = null)
    {
        var state = new GenerationState
        {
            Scale = scale ?? Scale.PentatonicMinor(60), // C4 pentatonic minor
            Octave = 4
        };
        return state;
    }

    private static Instrument MakeInstrument(string id, InstrumentRole role, int channel,
        int minNote = 0, int maxNote = 127) =>
        new()
        {
            Id = id,
            Name = id,
            Type = InstrumentType.Hardware,
            Role = role,
            MidiChannel = channel,
            Capabilities = new InstrumentCapabilities { MinNote = minNote, MaxNote = maxNote }
        };

    [Fact]
    public void EmptyRegistry_ReturnsEmptyPlan()
    {
        var state = CreateState();
        var router = new ChannelRouter(state, new Random(42));

        var plan = router.GeneratePlan(0, 480, null, 0);

        Assert.Empty(plan.Notes);
    }

    [Fact]
    public void EmptyRegistry_GetActiveInstruments_ReturnsEmpty()
    {
        var state = CreateState();
        var router = new ChannelRouter(state);

        var instruments = router.GetActiveInstruments();

        Assert.Empty(instruments);
    }

    [Fact]
    public void BassRole_GeneratesLowNotes()
    {
        var state = CreateState(Scale.Major(60));
        state.Instruments.Add(MakeInstrument("bass1", InstrumentRole.Bass, 2));
        var router = new ChannelRouter(state, new Random(42));

        // Generate on beat boundaries (tickInMeasure = 0 is a quarter note boundary)
        var notes = new List<PlannedNote>();
        for (int beat = 0; beat < 20; beat++)
        {
            var plan = router.GeneratePlan(beat * 480, 480, null, beat * 480);
            notes.AddRange(plan.Notes);
        }

        Assert.NotEmpty(notes);

        // Bass notes should be in octave 2-3 range (MIDI 36-59)
        foreach (var note in notes)
        {
            Assert.InRange(note.Note, 0, 71); // octave 2-3 max upper bound with scale offsets
            Assert.Equal(2, note.Channel);
        }
    }

    [Fact]
    public void PadRole_GeneratesMidRangeNotes()
    {
        var state = CreateState(Scale.Major(60));
        state.Instruments.Add(MakeInstrument("pad1", InstrumentRole.Pad, 3));
        var router = new ChannelRouter(state, new Random(42));

        var notes = new List<PlannedNote>();
        // Pads change on half-note boundaries (960-tick intervals)
        for (int i = 0; i < 20; i++)
        {
            var plan = router.GeneratePlan(i * 960, 480, null, i * 960);
            notes.AddRange(plan.Notes);
        }

        Assert.NotEmpty(notes);

        // Pad notes should be in octave 3-5 range (MIDI ~48-83)
        foreach (var note in notes)
        {
            Assert.InRange(note.Note, 36, 95); // mid-range
            Assert.Equal(3, note.Channel);
        }
    }

    [Fact]
    public void LeadRole_UsesNoteGenerator()
    {
        var state = CreateState(Scale.PentatonicMinor(60));
        state.Instruments.Add(MakeInstrument("lead1", InstrumentRole.Lead, 4));
        var router = new ChannelRouter(state, new Random(42));

        var notes = new List<PlannedNote>();
        for (int beat = 0; beat < 20; beat++)
        {
            var plan = router.GeneratePlan(beat * 480, 480, null, beat * 480);
            notes.AddRange(plan.Notes);
        }

        Assert.NotEmpty(notes);

        // Lead notes should be on channel 4
        foreach (var note in notes)
        {
            Assert.Equal(4, note.Channel);
        }
    }

    [Fact]
    public void MelodyRole_UsesNoteGenerator_SameAsLead()
    {
        var state = CreateState(Scale.PentatonicMinor(60));
        state.Instruments.Add(MakeInstrument("melody1", InstrumentRole.Melody, 5));
        var router = new ChannelRouter(state, new Random(42));

        var notes = new List<PlannedNote>();
        for (int beat = 0; beat < 20; beat++)
        {
            var plan = router.GeneratePlan(beat * 480, 480, null, beat * 480);
            notes.AddRange(plan.Notes);
        }

        Assert.NotEmpty(notes);

        foreach (var note in notes)
        {
            Assert.Equal(5, note.Channel);
        }
    }

    [Fact]
    public void DrumsRole_SkippedByRouter_HandledBySequencer()
    {
        var state = CreateState();
        state.Instruments.Add(MakeInstrument("drums1", InstrumentRole.Drums, 10));
        var router = new ChannelRouter(state, new Random(42));

        // Drums should produce no notes from the router (they use StepSequencer)
        var plan = router.GeneratePlan(0, 480, null, 0);
        Assert.Empty(plan.Notes);
    }

    [Fact]
    public void MultipleInstruments_MultiChannelOutput()
    {
        var state = CreateState(Scale.Major(60));
        state.Instruments.Add(MakeInstrument("bass1", InstrumentRole.Bass, 2));
        state.Instruments.Add(MakeInstrument("lead1", InstrumentRole.Lead, 4));
        var router = new ChannelRouter(state, new Random(42));

        var allNotes = new List<PlannedNote>();
        for (int beat = 0; beat < 20; beat++)
        {
            var plan = router.GeneratePlan(beat * 480, 480, null, beat * 480);
            allNotes.AddRange(plan.Notes);
        }

        // Should have notes on multiple channels
        var channels = allNotes.Select(n => n.Channel).Distinct().ToList();
        Assert.True(channels.Count >= 2, $"Expected at least 2 channels, got {channels.Count}: [{string.Join(", ", channels)}]");
        Assert.Contains(2, channels);
        Assert.Contains(4, channels);
    }

    [Fact]
    public void NoteRanges_RespectCapabilities()
    {
        var state = CreateState(Scale.Major(60));
        // Instrument with restricted range
        state.Instruments.Add(MakeInstrument("restricted-bass", InstrumentRole.Bass, 2,
            minNote: 40, maxNote: 55));
        var router = new ChannelRouter(state, new Random(42));

        var notes = new List<PlannedNote>();
        for (int beat = 0; beat < 30; beat++)
        {
            var plan = router.GeneratePlan(beat * 480, 480, null, beat * 480);
            notes.AddRange(plan.Notes);
        }

        Assert.NotEmpty(notes);

        foreach (var note in notes)
        {
            Assert.InRange(note.Note, 40, 55);
        }
    }

    [Fact]
    public void GetActiveInstruments_GroupsByRole()
    {
        var state = CreateState();
        state.Instruments.Add(MakeInstrument("bass1", InstrumentRole.Bass, 2));
        state.Instruments.Add(MakeInstrument("bass2", InstrumentRole.Bass, 3));
        state.Instruments.Add(MakeInstrument("lead1", InstrumentRole.Lead, 4));
        var router = new ChannelRouter(state);

        var grouped = router.GetActiveInstruments();

        Assert.Equal(2, grouped.Count); // Bass and Lead
        Assert.Equal(2, grouped[InstrumentRole.Bass].Count);
        Assert.Single(grouped[InstrumentRole.Lead]);
    }

    [Fact]
    public void RecordAndGetLastNote_Tracking()
    {
        var state = CreateState();
        var router = new ChannelRouter(state);

        Assert.Null(router.GetLastNote(2));

        router.RecordNotePlayed(2, 48);
        Assert.Equal(48, router.GetLastNote(2));

        router.ClearChannel(2);
        Assert.Null(router.GetLastNote(2));
    }

    [Fact]
    public void GetActiveChannels_ReturnsTrackedChannels()
    {
        var state = CreateState();
        var router = new ChannelRouter(state);

        router.RecordNotePlayed(2, 48);
        router.RecordNotePlayed(4, 72);

        var channels = router.GetActiveChannels();
        Assert.Equal(2, channels.Count);
        Assert.Contains(2, channels);
        Assert.Contains(4, channels);
    }

    [Fact]
    public void BassNotes_AreInScale()
    {
        var scale = Scale.Major(60);
        var state = CreateState(scale);
        state.Instruments.Add(MakeInstrument("bass1", InstrumentRole.Bass, 2));
        var router = new ChannelRouter(state, new Random(42));

        var notes = new List<PlannedNote>();
        for (int beat = 0; beat < 40; beat++)
        {
            var plan = router.GeneratePlan(beat * 480, 480, null, beat * 480);
            notes.AddRange(plan.Notes);
        }

        Assert.NotEmpty(notes);

        foreach (var note in notes)
        {
            Assert.True(scale.ContainsNote(note.Note),
                $"Bass note {note.Note} is not in scale {scale.Name}");
        }
    }

    [Fact]
    public void PlanNotes_HavePositiveDuration()
    {
        var state = CreateState(Scale.Major(60));
        state.Instruments.Add(MakeInstrument("bass1", InstrumentRole.Bass, 2));
        state.Instruments.Add(MakeInstrument("pad1", InstrumentRole.Pad, 3));
        state.Instruments.Add(MakeInstrument("lead1", InstrumentRole.Lead, 4));
        var router = new ChannelRouter(state, new Random(42));

        var notes = new List<PlannedNote>();
        for (int beat = 0; beat < 30; beat++)
        {
            var plan = router.GeneratePlan(beat * 480, 480, null, beat * 480);
            notes.AddRange(plan.Notes);
        }

        Assert.NotEmpty(notes);

        foreach (var note in notes)
        {
            Assert.True(note.DurationTicks > 0, $"Note duration must be positive, got {note.DurationTicks}");
        }
    }

    [Fact]
    public void PlanNotes_HaveValidVelocity()
    {
        var state = CreateState(Scale.Major(60));
        state.Instruments.Add(MakeInstrument("bass1", InstrumentRole.Bass, 2));
        state.Instruments.Add(MakeInstrument("pad1", InstrumentRole.Pad, 3));
        state.Instruments.Add(MakeInstrument("lead1", InstrumentRole.Lead, 4));
        var router = new ChannelRouter(state, new Random(42));

        var notes = new List<PlannedNote>();
        for (int beat = 0; beat < 30; beat++)
        {
            var plan = router.GeneratePlan(beat * 480, 480, null, beat * 480);
            notes.AddRange(plan.Notes);
        }

        Assert.NotEmpty(notes);

        foreach (var note in notes)
        {
            Assert.InRange(note.Velocity, 1, 127);
        }
    }

    [Fact]
    public void LeadNote_OctaveRestoredAfterGeneration()
    {
        var state = CreateState(Scale.PentatonicMinor(60));
        state.Octave = 4; // original octave
        state.Instruments.Add(MakeInstrument("lead1", InstrumentRole.Lead, 4));
        var router = new ChannelRouter(state, new Random(42));

        router.GeneratePlan(0, 480, null, 0);

        // Octave should be restored to original value
        Assert.Equal(4, state.Octave);
    }
}
