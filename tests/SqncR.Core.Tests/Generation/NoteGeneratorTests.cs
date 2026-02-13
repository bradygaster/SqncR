using SqncR.Core.Generation;
using SqncR.Theory;

namespace SqncR.Core.Tests.Generation;

public class NoteGeneratorTests
{
    private static GenerationState CreateState(Scale? scale = null, int octave = 4)
    {
        return new GenerationState
        {
            Scale = scale ?? Scale.Major(60), // C Major
            Octave = octave
        };
    }

    // --- INoteGenerator interface tests ---

    [Fact]
    public void ScaleWalkGenerator_Implements_INoteGenerator()
    {
        INoteGenerator generator = new ScaleWalkGenerator();
        Assert.NotNull(generator);
        Assert.Equal("Scale Walk", generator.Name);
    }

    [Fact]
    public void WeightedNoteGenerator_Implements_INoteGenerator()
    {
        INoteGenerator generator = new WeightedNoteGenerator();
        Assert.NotNull(generator);
        Assert.Equal("Weighted", generator.Name);
    }

    [Fact]
    public void ArpeggioGenerator_Implements_INoteGenerator()
    {
        INoteGenerator generator = new ArpeggioGenerator();
        Assert.NotNull(generator);
        Assert.Contains("Arpeggio", generator.Name);
    }

    // --- ScaleWalkGenerator tests ---

    [Fact]
    public void ScaleWalk_WalksUpThenDown()
    {
        var state = CreateState();
        var generator = new ScaleWalkGenerator();
        var notes = state.Scale.GetNotesInOctave(state.Octave);

        // Walk up
        var emitted = new List<int>();
        for (int i = 0; i < notes.Count; i++)
        {
            var note = generator.NextNote(state);
            Assert.NotNull(note);
            emitted.Add(note!.Value);
        }

        // First pass should ascend
        for (int i = 1; i < notes.Count; i++)
            Assert.True(emitted[i] >= emitted[i - 1], "Expected ascending walk");

        // After reaching top, should descend
        var descNote = generator.NextNote(state);
        Assert.NotNull(descNote);
        Assert.True(descNote!.Value < emitted[^1], "Expected descent after reaching top");
    }

    [Fact]
    public void ScaleWalk_WrapsAtBoundaries()
    {
        var state = CreateState();
        var generator = new ScaleWalkGenerator();
        var notes = state.Scale.GetNotesInOctave(state.Octave);

        // Walk enough to go up and back down and back up
        int totalSteps = notes.Count * 3;
        var emitted = new List<int>();
        for (int i = 0; i < totalSteps; i++)
        {
            var note = generator.NextNote(state);
            Assert.NotNull(note);
            emitted.Add(note!.Value);
        }

        // Should have both ascending and descending motion
        bool hasAscent = false, hasDescent = false;
        for (int i = 1; i < emitted.Count; i++)
        {
            if (emitted[i] > emitted[i - 1]) hasAscent = true;
            if (emitted[i] < emitted[i - 1]) hasDescent = true;
        }

        Assert.True(hasAscent, "Scale walk should have ascending motion");
        Assert.True(hasDescent, "Scale walk should have descending motion");
    }

    // --- WeightedNoteGenerator tests ---

    [Fact]
    public void Weighted_OnlyEmitsNotesInScale()
    {
        var scale = Scale.PentatonicMinor(60);
        var state = CreateState(scale);
        var generator = new WeightedNoteGenerator(restProbability: 0, random: new Random(42));

        for (int i = 0; i < 100; i++)
        {
            var note = generator.NextNote(state);
            if (note != null)
            {
                Assert.True(scale.ContainsNote(note.Value),
                    $"Note {note.Value} is not in {scale.Name}");
            }
        }
    }

    [Fact]
    public void Weighted_NeverEmitsOutsideMidiRange()
    {
        var state = CreateState(octave: 0);
        var generator = new WeightedNoteGenerator(restProbability: 0, random: new Random(42));

        for (int i = 0; i < 100; i++)
        {
            var note = generator.NextNote(state);
            if (note != null)
            {
                Assert.InRange(note.Value, 0, 127);
            }
        }

        // Also test high octave
        state = CreateState(octave: 9);
        for (int i = 0; i < 100; i++)
        {
            var note = generator.NextNote(state);
            if (note != null)
            {
                Assert.InRange(note.Value, 0, 127);
            }
        }
    }

    [Fact]
    public void Weighted_ProducesRestsSometimes()
    {
        var state = CreateState();
        var generator = new WeightedNoteGenerator(restProbability: 0.15, random: new Random(42));

        int restCount = 0;
        int noteCount = 0;
        for (int i = 0; i < 100; i++)
        {
            var note = generator.NextNote(state);
            if (note == null)
                restCount++;
            else
                noteCount++;
        }

        Assert.True(restCount > 0, "Expected at least some rests in 100 iterations");
        Assert.True(noteCount > 0, "Expected at least some notes in 100 iterations");
    }

    [Fact]
    public void Weighted_FavorsRootNote()
    {
        var scale = Scale.Major(60); // C Major, root = C
        var state = CreateState(scale);
        var generator = new WeightedNoteGenerator(restProbability: 0, random: new Random(123));

        var noteCounts = new Dictionary<int, int>();
        int totalNotes = 1000;

        for (int i = 0; i < totalNotes; i++)
        {
            var note = generator.NextNote(state);
            if (note != null)
            {
                if (!noteCounts.ContainsKey(note.Value))
                    noteCounts[note.Value] = 0;
                noteCounts[note.Value]++;
            }
        }

        var scaleNotes = scale.GetNotesInOctave(state.Octave);
        int rootNote = scaleNotes[0];
        int rootCount = noteCounts.GetValueOrDefault(rootNote, 0);

        // Root should appear more often than uniform distribution
        // Uniform would be ~1000/7 ≈ 143 for a 7-note scale
        double uniformExpected = (double)totalNotes / scaleNotes.Count;
        Assert.True(rootCount > uniformExpected,
            $"Root note appeared {rootCount} times, expected more than uniform ({uniformExpected:F0})");
    }

    [Fact]
    public void Weighted_ZeroRestProbability_NeverRests()
    {
        var state = CreateState();
        var generator = new WeightedNoteGenerator(restProbability: 0, random: new Random(42));

        for (int i = 0; i < 100; i++)
        {
            var note = generator.NextNote(state);
            Assert.NotNull(note);
        }
    }

    // --- ArpeggioGenerator tests ---

    [Fact]
    public void Arpeggio_PlaysChordTonesOnly()
    {
        var scale = Scale.Major(60);
        var state = CreateState(scale);
        var generator = new ArpeggioGenerator(ArpeggioPattern.Up);

        var scaleNotes = scale.GetNotesInOctave(state.Octave);
        // Chord tones: root (index 0), 3rd (index 2), 5th (index 4)
        var expectedChordTones = new HashSet<int>
        {
            scaleNotes[0], // root
            scaleNotes[2], // 3rd
            scaleNotes[4]  // 5th
        };

        for (int i = 0; i < 12; i++)
        {
            var note = generator.NextNote(state);
            Assert.NotNull(note);
            Assert.Contains(note!.Value, expectedChordTones);
        }
    }

    [Fact]
    public void Arpeggio_UpPattern_GoesRoot3rd5thRoot()
    {
        var scale = Scale.Major(60);
        var state = CreateState(scale);
        var generator = new ArpeggioGenerator(ArpeggioPattern.Up);

        var scaleNotes = scale.GetNotesInOctave(state.Octave);
        int root = scaleNotes[0];
        int third = scaleNotes[2];
        int fifth = scaleNotes[4];

        // Up pattern: root → 3rd → 5th → root → 3rd → 5th ...
        Assert.Equal(root, generator.NextNote(state));
        Assert.Equal(third, generator.NextNote(state));
        Assert.Equal(fifth, generator.NextNote(state));
        Assert.Equal(root, generator.NextNote(state)); // wraps
    }

    [Fact]
    public void Arpeggio_DownPattern_GoesRoot5th3rdRoot()
    {
        var scale = Scale.Major(60);
        var state = CreateState(scale);
        var generator = new ArpeggioGenerator(ArpeggioPattern.Down);

        var scaleNotes = scale.GetNotesInOctave(state.Octave);
        int root = scaleNotes[0];
        int third = scaleNotes[2];
        int fifth = scaleNotes[4];

        // Down pattern: root → 5th → 3rd → root → 5th → 3rd ...
        Assert.Equal(root, generator.NextNote(state));
        Assert.Equal(fifth, generator.NextNote(state));
        Assert.Equal(third, generator.NextNote(state));
        Assert.Equal(root, generator.NextNote(state)); // wraps
    }

    [Fact]
    public void Arpeggio_RandomPattern_OnlyPlaysChordTones()
    {
        var scale = Scale.Major(60);
        var state = CreateState(scale);
        var generator = new ArpeggioGenerator(ArpeggioPattern.Random, random: new Random(42));

        var scaleNotes = scale.GetNotesInOctave(state.Octave);
        var chordTones = new HashSet<int>
        {
            scaleNotes[0],
            scaleNotes[2],
            scaleNotes[4]
        };

        for (int i = 0; i < 20; i++)
        {
            var note = generator.NextNote(state);
            Assert.NotNull(note);
            Assert.Contains(note!.Value, chordTones);
        }
    }

    // --- GenerationState default generator test ---

    [Fact]
    public void GenerationState_DefaultGenerator_IsWeighted()
    {
        var state = new GenerationState();
        Assert.IsType<WeightedNoteGenerator>(state.NoteGenerator);
    }
}
