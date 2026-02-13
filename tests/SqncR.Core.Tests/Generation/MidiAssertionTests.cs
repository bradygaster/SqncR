using SqncR.Core.Generation;
using SqncR.Core.Rhythm;
using SqncR.Midi.Testing;
using SqncR.Theory;
using Microsoft.Extensions.Logging.Abstractions;

namespace SqncR.Core.Tests.Generation;

public class MidiAssertionTests
{
    /// <summary>
    /// Runs the engine for the specified duration and returns all captured MIDI events.
    /// </summary>
    private static async Task<IReadOnlyList<CapturedMidiEvent>> RunEngineFor(
        TimeSpan duration,
        Action<GenerationState>? configureState = null,
        Action<GenerationEngine>? configureEngine = null)
    {
        var mock = new MockMidiOutput();
        var state = new GenerationState();
        configureState?.Invoke(state);

        var logger = NullLogger<GenerationEngine>.Instance;
        var engine = new GenerationEngine(state, mock, logger);
        configureEngine?.Invoke(engine);

        using var cts = new CancellationTokenSource(duration);
        await engine.StartAsync(cts.Token);
        try { await Task.Delay(duration + TimeSpan.FromMilliseconds(100), cts.Token); }
        catch (OperationCanceledException) { }
        await engine.StopAsync(CancellationToken.None);

        engine.Dispose();
        mock.Dispose();

        return mock.Events.ToList();
    }

    // ── Test 1: C Major notes only ──

    [Fact]
    public async Task MelodyNotes_InCMajor_AreAllInScale()
    {
        var scale = Scale.Major(60); // C Major

        var events = await RunEngineFor(
            TimeSpan.FromSeconds(1.5),
            state =>
            {
                state.Scale = scale;
                state.IsPlaying = true;
                state.NoteGenerator = new ScaleWalkGenerator();
                state.DrumPattern = PatternLibrary.Get("rock");
            });

        var melodyNoteOns = events
            .Where(e => e.Type == MidiEventType.NoteOn && e.Channel == 1)
            .ToList();

        Assert.NotEmpty(melodyNoteOns);

        foreach (var evt in melodyNoteOns)
        {
            int pitchClass = evt.Note % 12;
            Assert.True(scale.ContainsNote(evt.Note),
                $"Note {evt.Note} (pitch class {pitchClass}) is not in C Major");
        }
    }

    // ── Test 2: A Minor Pentatonic notes only ──

    [Fact]
    public async Task MelodyNotes_InAMinorPentatonic_AreAllInScale()
    {
        var scale = Scale.PentatonicMinor(69); // A minor pentatonic (A=69)

        var events = await RunEngineFor(
            TimeSpan.FromSeconds(1.5),
            state =>
            {
                state.Scale = scale;
                state.IsPlaying = true;
                state.NoteGenerator = new ScaleWalkGenerator();
                state.DrumPattern = PatternLibrary.Get("rock");
            });

        var melodyNoteOns = events
            .Where(e => e.Type == MidiEventType.NoteOn && e.Channel == 1)
            .ToList();

        Assert.NotEmpty(melodyNoteOns);

        foreach (var evt in melodyNoteOns)
        {
            Assert.True(scale.ContainsNote(evt.Note),
                $"Note {evt.Note} is not in A minor pentatonic");
        }
    }

    // ── Test 3: Tempo tick consistency ──

    [Fact]
    public async Task MelodyNotes_At120BPM_ArriveEvery500ms()
    {
        var events = await RunEngineFor(
            TimeSpan.FromSeconds(2),
            state =>
            {
                state.Tempo = 120;
                state.IsPlaying = true;
                state.NoteGenerator = new ScaleWalkGenerator();
                state.DrumPattern = PatternLibrary.Get("rock");
            });

        var melodyNoteOns = events
            .Where(e => e.Type == MidiEventType.NoteOn && e.Channel == 1)
            .OrderBy(e => e.Timestamp)
            .ToList();

        // Need at least 2 notes to check intervals
        Assert.True(melodyNoteOns.Count >= 2, $"Expected ≥2 melody notes, got {melodyNoteOns.Count}");

        for (int i = 1; i < melodyNoteOns.Count; i++)
        {
            var delta = melodyNoteOns[i].Timestamp - melodyNoteOns[i - 1].Timestamp;
            // At 120 BPM, quarter note = 500ms. Allow ±50ms tolerance.
            Assert.True(Math.Abs(delta.TotalMilliseconds - 500) < 50,
                $"Note interval {i}: expected ~500ms, got {delta.TotalMilliseconds:F1}ms");
        }
    }

    // ── Test 4: Modify tempo mid-play ──

    [Fact]
    public async Task TempoChange_MidPlay_NotesComeFaster()
    {
        var mock = new MockMidiOutput();
        var state = new GenerationState
        {
            Tempo = 120,
            IsPlaying = true,
            NoteGenerator = new ScaleWalkGenerator(),
            DrumPattern = PatternLibrary.Get("rock")
        };

        var logger = NullLogger<GenerationEngine>.Instance;
        var engine = new GenerationEngine(state, mock, logger);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await engine.StartAsync(cts.Token);

        // Let it run at 120 BPM for 1.2 seconds
        await Task.Delay(1200);

        // Change tempo to 200 BPM
        await engine.Commands.WriteAsync(new GenerationCommand.SetTempo(200));

        // Let it run at 200 BPM for 1.2 seconds
        await Task.Delay(1200);

        await engine.StopAsync(CancellationToken.None);
        engine.Dispose();
        mock.Dispose();

        var melodyNoteOns = mock.Events
            .Where(e => e.Type == MidiEventType.NoteOn && e.Channel == 1)
            .OrderBy(e => e.Timestamp)
            .ToList();

        Assert.True(melodyNoteOns.Count >= 4, $"Expected ≥4 melody notes, got {melodyNoteOns.Count}");

        // Split into before and after tempo change (~1.2s mark)
        var tempoChangeTime = TimeSpan.FromMilliseconds(1200);
        var beforeChange = melodyNoteOns.Where(e => e.Timestamp < tempoChangeTime).ToList();
        var afterChange = melodyNoteOns.Where(e => e.Timestamp >= tempoChangeTime).ToList();

        if (beforeChange.Count >= 2 && afterChange.Count >= 2)
        {
            var avgIntervalBefore = Enumerable.Range(1, beforeChange.Count - 1)
                .Average(i => (beforeChange[i].Timestamp - beforeChange[i - 1].Timestamp).TotalMilliseconds);

            var avgIntervalAfter = Enumerable.Range(1, afterChange.Count - 1)
                .Average(i => (afterChange[i].Timestamp - afterChange[i - 1].Timestamp).TotalMilliseconds);

            // After tempo increase (200 BPM → 300ms per beat), interval should be shorter
            Assert.True(avgIntervalAfter < avgIntervalBefore,
                $"Expected faster notes after tempo change: before={avgIntervalBefore:F0}ms, after={avgIntervalAfter:F0}ms");
        }
    }

    // ── Test 5: Stop sends AllNotesOff ──

    [Fact]
    public async Task StopPlayback_SendsAllNotesOff()
    {
        var events = await RunEngineFor(
            TimeSpan.FromMilliseconds(800),
            state =>
            {
                state.IsPlaying = true;
                state.DrumPattern = PatternLibrary.Get("rock");
            });

        var allNotesOff = events.Where(e => e.Type == MidiEventType.AllNotesOff).ToList();
        Assert.NotEmpty(allNotesOff);
    }

    // ── Test 6: Note-off matches note-on ──

    [Fact]
    public async Task NoteOff_MatchesNoteOn_BeforeNextNoteOn()
    {
        var events = await RunEngineFor(
            TimeSpan.FromSeconds(1.5),
            state =>
            {
                state.IsPlaying = true;
                state.NoteGenerator = new ScaleWalkGenerator();
                state.DrumPattern = PatternLibrary.Get("rock");
            });

        // Check melody channel (1) — each NoteOn should have a NoteOff before next NoteOn
        var melodyEvents = events
            .Where(e => e.Channel == 1 &&
                       (e.Type == MidiEventType.NoteOn || e.Type == MidiEventType.NoteOff))
            .OrderBy(e => e.Timestamp)
            .ToList();

        int? activeNote = null;
        foreach (var evt in melodyEvents)
        {
            if (evt.Type == MidiEventType.NoteOn)
            {
                Assert.True(activeNote == null,
                    $"NoteOn({evt.Note}) at {evt.Timestamp.TotalMilliseconds:F0}ms without NoteOff for previous note {activeNote}");
                activeNote = evt.Note;
            }
            else if (evt.Type == MidiEventType.NoteOff)
            {
                Assert.True(activeNote != null,
                    $"NoteOff({evt.Note}) at {evt.Timestamp.TotalMilliseconds:F0}ms without matching NoteOn");
                Assert.Equal(activeNote.Value, evt.Note);
                activeNote = null;
            }
        }
    }

    // ── Test 7: Drum pattern fires ──

    [Fact]
    public async Task DrumPattern_Rock_FiresGMDrumNotes()
    {
        var events = await RunEngineFor(
            TimeSpan.FromSeconds(1.5),
            state =>
            {
                state.IsPlaying = true;
                state.DrumPattern = PatternLibrary.Get("rock");
            });

        var drumNoteOns = events
            .Where(e => e.Type == MidiEventType.NoteOn && e.Channel == 10)
            .ToList();

        Assert.NotEmpty(drumNoteOns);

        // Rock pattern uses Kick (36), Snare (38), ClosedHiHat (42)
        var gmDrumNotes = new HashSet<int> { 36, 38, 42 };
        var foundDrumNotes = drumNoteOns.Select(e => e.Note).Distinct().ToHashSet();

        Assert.True(foundDrumNotes.IsSubsetOf(gmDrumNotes),
            $"Unexpected drum notes: {string.Join(", ", foundDrumNotes.Except(gmDrumNotes))}. Expected only 36, 38, 42.");

        // Verify we get at least kick and hat (most frequent in rock)
        Assert.Contains(36, foundDrumNotes); // kick
        Assert.Contains(42, foundDrumNotes); // hat
    }

    // ── Test 8: Scale change mid-play ──

    [Fact]
    public async Task ScaleChange_MidPlay_NotesFollowNewScale()
    {
        var cMajor = Scale.Major(60);
        var dMinor = Scale.Minor(62); // D natural minor

        var mock = new MockMidiOutput();
        var state = new GenerationState
        {
            Scale = cMajor,
            IsPlaying = true,
            NoteGenerator = new ScaleWalkGenerator(),
            DrumPattern = PatternLibrary.Get("rock")
        };

        var logger = NullLogger<GenerationEngine>.Instance;
        var engine = new GenerationEngine(state, mock, logger);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await engine.StartAsync(cts.Token);

        // Run with C Major for 800ms
        await Task.Delay(800);

        // Switch to D Minor
        await engine.Commands.WriteAsync(new GenerationCommand.SetScale(dMinor));

        // Allow time for command processing and a couple notes
        await Task.Delay(50);
        mock.Reset();

        // Now collect notes in D Minor
        await Task.Delay(1200);

        await engine.StopAsync(CancellationToken.None);
        engine.Dispose();

        var melodyAfterChange = mock.Events
            .Where(e => e.Type == MidiEventType.NoteOn && e.Channel == 1)
            .ToList();

        Assert.NotEmpty(melodyAfterChange);

        foreach (var evt in melodyAfterChange)
        {
            Assert.True(dMinor.ContainsNote(evt.Note),
                $"Note {evt.Note} after scale change is not in D Minor");
        }

        mock.Dispose();
    }

    // ── Test 9: Generator swap mid-play ──

    [Fact]
    public async Task GeneratorSwap_MidPlay_ChangesNoteCharacter()
    {
        var mock = new MockMidiOutput();
        var scale = Scale.Major(60);
        var state = new GenerationState
        {
            Scale = scale,
            IsPlaying = true,
            NoteGenerator = new ScaleWalkGenerator(),
            DrumPattern = PatternLibrary.Get("rock"),
            Octave = 4
        };

        var logger = NullLogger<GenerationEngine>.Instance;
        var engine = new GenerationEngine(state, mock, logger);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await engine.StartAsync(cts.Token);

        // Run with ScaleWalkGenerator for 1s
        await Task.Delay(1000);
        var eventsBeforeSwap = mock.Events
            .Where(e => e.Type == MidiEventType.NoteOn && e.Channel == 1)
            .Select(e => e.Note)
            .ToList();

        // Swap to ArpeggioGenerator
        await engine.Commands.WriteAsync(
            new GenerationCommand.SetGenerator(new ArpeggioGenerator()));

        await Task.Delay(50);
        mock.Reset();

        // Run with ArpeggioGenerator for 1s
        await Task.Delay(1000);

        await engine.StopAsync(CancellationToken.None);
        engine.Dispose();

        var eventsAfterSwap = mock.Events
            .Where(e => e.Type == MidiEventType.NoteOn && e.Channel == 1)
            .Select(e => e.Note)
            .ToList();

        Assert.NotEmpty(eventsBeforeSwap);
        Assert.NotEmpty(eventsAfterSwap);

        // ArpeggioGenerator only plays chord tones (root, 3rd, 5th)
        var chordTones = new HashSet<int>();
        var scaleNotes = scale.GetNotesInOctave(4);
        if (scaleNotes.Count > 0) chordTones.Add(scaleNotes[0]); // root
        if (scaleNotes.Count > 2) chordTones.Add(scaleNotes[2]); // 3rd
        if (scaleNotes.Count > 4) chordTones.Add(scaleNotes[4]); // 5th

        foreach (var note in eventsAfterSwap)
        {
            Assert.True(chordTones.Contains(note),
                $"Arpeggio note {note} is not a chord tone. Expected: {string.Join(", ", chordTones)}");
        }

        mock.Dispose();
    }

    // ── Test 10: Octave change ──

    [Fact]
    public async Task OctaveChange_NotesInCorrectRange()
    {
        var events = await RunEngineFor(
            TimeSpan.FromSeconds(1.5),
            state =>
            {
                state.Octave = 3;
                state.IsPlaying = true;
                state.NoteGenerator = new ScaleWalkGenerator();
                state.Scale = Scale.Major(60);
                state.DrumPattern = PatternLibrary.Get("rock");
            });

        var melodyNoteOns = events
            .Where(e => e.Type == MidiEventType.NoteOn && e.Channel == 1)
            .ToList();

        Assert.NotEmpty(melodyNoteOns);

        // Octave 3 with C Major: GetNotesInOctave(3) = (3+1)*12 + 0 + intervals
        // = 48 + {0,2,4,5,7,9,11} = {48,50,52,53,55,57,59}
        foreach (var evt in melodyNoteOns)
        {
            Assert.True(evt.Note >= 48 && evt.Note <= 59,
                $"Note {evt.Note} is outside octave 3 range (48-59)");
        }
    }

    // ── Test 11: Clean shutdown no hanging notes ──

    [Fact]
    public async Task CleanShutdown_NoHangingNotes()
    {
        var events = await RunEngineFor(
            TimeSpan.FromSeconds(1),
            state =>
            {
                state.IsPlaying = true;
                state.NoteGenerator = new ScaleWalkGenerator();
                state.DrumPattern = PatternLibrary.Get("rock");
            });

        // Track active notes per channel
        var activeNotes = new Dictionary<(int Channel, int Note), int>();

        foreach (var evt in events.OrderBy(e => e.Timestamp))
        {
            var key = (evt.Channel, evt.Note);
            if (evt.Type == MidiEventType.NoteOn)
            {
                activeNotes[key] = activeNotes.GetValueOrDefault(key) + 1;
            }
            else if (evt.Type == MidiEventType.NoteOff)
            {
                if (activeNotes.ContainsKey(key))
                {
                    activeNotes[key]--;
                    if (activeNotes[key] <= 0)
                        activeNotes.Remove(key);
                }
            }
            else if (evt.Type == MidiEventType.AllNotesOff)
            {
                // Clear all notes on that channel
                var channelKeys = activeNotes.Keys
                    .Where(k => k.Channel == evt.Channel).ToList();
                foreach (var k in channelKeys)
                    activeNotes.Remove(k);
            }
        }

        Assert.Empty(activeNotes);
    }

    // ── Test 12: Concurrent command safety ──

    [Fact]
    public async Task ConcurrentCommands_NoExceptions()
    {
        var mock = new MockMidiOutput();
        var state = new GenerationState
        {
            IsPlaying = true,
            DrumPattern = PatternLibrary.Get("rock")
        };

        var logger = NullLogger<GenerationEngine>.Instance;
        var engine = new GenerationEngine(state, mock, logger);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await engine.StartAsync(cts.Token);

        // Fire 100 SetTempo commands from a background thread
        var commandTask = Task.Run(async () =>
        {
            for (int i = 0; i < 100; i++)
            {
                await engine.Commands.WriteAsync(new GenerationCommand.SetTempo(60 + i));
            }
        });

        await commandTask; // Should complete without exceptions
        await Task.Delay(200);
        await engine.StopAsync(CancellationToken.None);
        engine.Dispose();
        mock.Dispose();

        // If we got here, no exceptions were thrown
        Assert.True(true);
    }
}
