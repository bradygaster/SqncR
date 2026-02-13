using SqncR.Core.Generation;
using SqncR.Core.Instruments;
using SqncR.Core.Rhythm;
using SqncR.Theory;

namespace SqncR.Core.Tests.Generation;

public class RoleNoteSelectorTests
{
    private const int TicksPerBeat = 480;

    private static Scale CMajor => Scale.Major(60);
    private static Scale CMinor => Scale.Minor(60);

    // --- Bass tests ---

    [Fact]
    public void Bass_GeneratesLowNotes_BelowMidi60()
    {
        var selector = new RoleNoteSelector(random: new Random(42));
        var allNotes = new List<int>();

        for (int tick = 0; tick < TicksPerBeat * 16; tick += TicksPerBeat)
        {
            var output = selector.SelectNotes(InstrumentRole.Bass, CMajor, 60, tick, TicksPerBeat);
            allNotes.AddRange(output.Notes.Select(n => n.MidiNote));
        }

        Assert.NotEmpty(allNotes);
        Assert.All(allNotes, note => Assert.True(note < 60, $"Bass note {note} should be below MIDI 60"));
    }

    [Fact]
    public void Bass_PlaysRootNotesPreferentially()
    {
        var selector = new RoleNoteSelector(random: new Random(123));
        int rootCount = 0;
        int totalNotes = 0;
        int rootPitchClass = 60 % 12; // C = 0

        for (int tick = 0; tick < TicksPerBeat * 200; tick += TicksPerBeat)
        {
            var output = selector.SelectNotes(InstrumentRole.Bass, CMajor, 60, tick, TicksPerBeat);
            foreach (var note in output.Notes)
            {
                totalNotes++;
                if (note.MidiNote % 12 == rootPitchClass)
                    rootCount++;
            }
        }

        Assert.True(totalNotes > 0, "Expected bass to produce notes");
        double rootPct = (double)rootCount / totalNotes;
        // Root should appear at least 30% of the time (weighted heavily)
        Assert.True(rootPct > 0.3, $"Root appeared {rootPct:P0}, expected > 30%");
    }

    [Fact]
    public void Bass_VelocityInRange_80To100()
    {
        var selector = new RoleNoteSelector(random: new Random(42));

        for (int tick = 0; tick < TicksPerBeat * 32; tick += TicksPerBeat)
        {
            var output = selector.SelectNotes(InstrumentRole.Bass, CMajor, 60, tick, TicksPerBeat);
            foreach (var note in output.Notes)
            {
                Assert.InRange(note.Velocity, 80, 100);
            }
        }
    }

    [Fact]
    public void Bass_NotesAreWithinScale()
    {
        var scale = Scale.PentatonicMinor(60);
        var selector = new RoleNoteSelector(random: new Random(42));

        for (int tick = 0; tick < TicksPerBeat * 32; tick += TicksPerBeat)
        {
            var output = selector.SelectNotes(InstrumentRole.Bass, scale, 60, tick, TicksPerBeat);
            foreach (var note in output.Notes)
            {
                Assert.True(scale.ContainsNote(note.MidiNote),
                    $"Bass note {note.MidiNote} is not in {scale.Name}");
            }
        }
    }

    // --- Pad tests ---

    [Fact]
    public void Pad_GeneratesChords_ThreeNotes()
    {
        var selector = new RoleNoteSelector(random: new Random(42));

        // On first beat, pad should produce a chord
        var output = selector.SelectNotes(InstrumentRole.Pad, CMajor, 60, 0, TicksPerBeat);
        Assert.Equal(3, output.Notes.Count);
    }

    [Fact]
    public void Pad_NotesAreInMidRange()
    {
        var selector = new RoleNoteSelector(random: new Random(42));

        var output = selector.SelectNotes(InstrumentRole.Pad, CMajor, 60, 0, TicksPerBeat);
        Assert.All(output.Notes, note =>
        {
            Assert.InRange(note.MidiNote, 48, 83);
        });
    }

    [Fact]
    public void Pad_VelocityInRange_50To70()
    {
        var selector = new RoleNoteSelector(random: new Random(42));

        var output = selector.SelectNotes(InstrumentRole.Pad, CMajor, 60, 0, TicksPerBeat);
        Assert.All(output.Notes, note =>
        {
            Assert.InRange(note.Velocity, 50, 70);
        });
    }

    [Fact]
    public void Pad_DurationIsWholeNote()
    {
        var selector = new RoleNoteSelector(random: new Random(42));

        var output = selector.SelectNotes(InstrumentRole.Pad, CMajor, 60, 0, TicksPerBeat);
        int wholeNoteDuration = TicksPerBeat * 4;

        Assert.All(output.Notes, note =>
        {
            Assert.Equal(wholeNoteDuration, note.DurationTicks);
        });
    }

    // --- Lead/Melody tests ---

    [Fact]
    public void Lead_UsesPluggedInGenerator()
    {
        var generator = new ScaleWalkGenerator();
        var selector = new RoleNoteSelector(leadGenerator: generator, random: new Random(42));
        var allNotes = new List<int>();

        for (int tick = 0; tick < TicksPerBeat * 8; tick += TicksPerBeat / 2)
        {
            var output = selector.SelectNotes(InstrumentRole.Lead, CMajor, 60, tick, TicksPerBeat);
            allNotes.AddRange(output.Notes.Select(n => n.MidiNote));
        }

        Assert.NotEmpty(allNotes);
    }

    [Fact]
    public void Lead_NotesAreInHighRange()
    {
        var generator = new WeightedNoteGenerator(restProbability: 0, random: new Random(42));
        var selector = new RoleNoteSelector(leadGenerator: generator, random: new Random(42));
        var allNotes = new List<int>();

        for (int tick = 0; tick < TicksPerBeat * 16; tick += TicksPerBeat / 2)
        {
            var output = selector.SelectNotes(InstrumentRole.Lead, CMajor, 60, tick, TicksPerBeat);
            allNotes.AddRange(output.Notes.Select(n => n.MidiNote));
        }

        Assert.NotEmpty(allNotes);
        Assert.All(allNotes, note => Assert.InRange(note, 60, 95));
    }

    [Fact]
    public void Lead_VelocityInRange_70To90()
    {
        var generator = new WeightedNoteGenerator(restProbability: 0, random: new Random(42));
        var selector = new RoleNoteSelector(leadGenerator: generator, random: new Random(42));

        for (int tick = 0; tick < TicksPerBeat * 16; tick += TicksPerBeat / 2)
        {
            var output = selector.SelectNotes(InstrumentRole.Lead, CMajor, 60, tick, TicksPerBeat);
            foreach (var note in output.Notes)
            {
                Assert.InRange(note.Velocity, 70, 90);
            }
        }
    }

    [Fact]
    public void Lead_NotesAreWithinScale()
    {
        var scale = Scale.PentatonicMajor(60);
        var generator = new WeightedNoteGenerator(restProbability: 0, random: new Random(42));
        var selector = new RoleNoteSelector(leadGenerator: generator, random: new Random(42));

        for (int tick = 0; tick < TicksPerBeat * 16; tick += TicksPerBeat / 2)
        {
            var output = selector.SelectNotes(InstrumentRole.Lead, scale, 60, tick, TicksPerBeat);
            foreach (var note in output.Notes)
            {
                Assert.True(scale.ContainsNote(note.MidiNote),
                    $"Lead note {note.MidiNote} is not in {scale.Name}");
            }
        }
    }

    [Fact]
    public void Melody_BehavesLikeLead()
    {
        var generator = new ScaleWalkGenerator();
        var selector = new RoleNoteSelector(leadGenerator: generator, random: new Random(42));

        var leadOutput = selector.SelectNotes(InstrumentRole.Lead, CMajor, 60, 0, TicksPerBeat);

        // Melody uses same logic as Lead
        var selector2 = new RoleNoteSelector(leadGenerator: new ScaleWalkGenerator(), random: new Random(42));
        var melodyOutput = selector2.SelectNotes(InstrumentRole.Melody, CMajor, 60, 0, TicksPerBeat);

        // Both should produce notes in the same range
        Assert.NotEmpty(leadOutput.Notes);
        Assert.NotEmpty(melodyOutput.Notes);
        Assert.All(melodyOutput.Notes, n => Assert.InRange(n.MidiNote, 60, 95));
    }

    // --- Drums tests ---

    [Fact]
    public void Drums_ReturnsDrumMapNotes()
    {
        var pattern = PatternLibrary.Get("rock");
        var sequencer = pattern.ToSequencer(TicksPerBeat);
        var drumMap = DrumMap.GeneralMidi;
        var selector = new RoleNoteSelector(drumSequencer: sequencer, drumMap: drumMap, random: new Random(42));

        var allNotes = new List<int>();
        int ticksPerMeasure = sequencer.TicksPerMeasure;

        // Check every tick position in one measure
        for (int tick = 0; tick < ticksPerMeasure; tick += TicksPerBeat / 4)
        {
            var output = selector.SelectNotes(InstrumentRole.Drums, CMajor, 60, tick, TicksPerBeat);
            allNotes.AddRange(output.Notes.Select(n => n.MidiNote));
        }

        Assert.NotEmpty(allNotes);
        // All drum notes should be in GM percussion range (35-59)
        Assert.All(allNotes, note => Assert.InRange(note, 35, 59));
    }

    [Fact]
    public void Drums_WithNoSequencer_ReturnsEmpty()
    {
        var selector = new RoleNoteSelector(random: new Random(42));
        var output = selector.SelectNotes(InstrumentRole.Drums, CMajor, 60, 0, TicksPerBeat);
        Assert.Empty(output.Notes);
    }

    [Fact]
    public void Drums_VelocityFromPattern()
    {
        var pattern = PatternLibrary.Get("rock");
        var sequencer = pattern.ToSequencer(TicksPerBeat);
        var drumMap = DrumMap.GeneralMidi;
        var selector = new RoleNoteSelector(drumSequencer: sequencer, drumMap: drumMap, random: new Random(42));

        // Tick 0 should have hits (beat 1 in rock: kick + hat)
        var output = selector.SelectNotes(InstrumentRole.Drums, CMajor, 60, 0, TicksPerBeat);

        Assert.NotEmpty(output.Notes);
        Assert.All(output.Notes, note =>
        {
            Assert.InRange(note.Velocity, 1, 127);
        });
    }

    // --- GenerationState integration ---

    [Fact]
    public void GenerationState_HasNoteSelector()
    {
        var state = new GenerationState();
        Assert.NotNull(state.NoteSelector);
        Assert.IsType<RoleNoteSelector>(state.NoteSelector);
    }

    // --- Cross-role tests ---

    [Fact]
    public void AllRoles_ProduceNotesAtBeatBoundary()
    {
        var pattern = PatternLibrary.Get("rock");
        var sequencer = pattern.ToSequencer(TicksPerBeat);
        var generator = new WeightedNoteGenerator(restProbability: 0, random: new Random(42));
        var selector = new RoleNoteSelector(
            leadGenerator: generator,
            drumSequencer: sequencer,
            drumMap: DrumMap.GeneralMidi,
            random: new Random(42));

        // At tick 0 (beat 1), all roles should be able to produce output
        var bassOutput = selector.SelectNotes(InstrumentRole.Bass, CMajor, 60, 0, TicksPerBeat);
        var padOutput = selector.SelectNotes(InstrumentRole.Pad, CMajor, 60, 0, TicksPerBeat);
        var leadOutput = selector.SelectNotes(InstrumentRole.Lead, CMajor, 60, 0, TicksPerBeat);
        var drumOutput = selector.SelectNotes(InstrumentRole.Drums, CMajor, 60, 0, TicksPerBeat);

        // Each role should produce at least some notes at beat boundary
        Assert.NotEmpty(bassOutput.Notes);
        Assert.NotEmpty(padOutput.Notes);
        Assert.NotEmpty(leadOutput.Notes);
        Assert.NotEmpty(drumOutput.Notes);
    }

    [Fact]
    public void Bass_InMidiRange_36To59()
    {
        var selector = new RoleNoteSelector(random: new Random(99));

        for (int tick = 0; tick < TicksPerBeat * 64; tick += TicksPerBeat)
        {
            var output = selector.SelectNotes(InstrumentRole.Bass, CMajor, 60, tick, TicksPerBeat);
            foreach (var note in output.Notes)
            {
                Assert.InRange(note.MidiNote, 36, 59);
            }
        }
    }
}
