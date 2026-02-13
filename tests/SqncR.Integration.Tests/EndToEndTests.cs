using Microsoft.Extensions.Logging.Abstractions;
using SqncR.Core.Generation;
using SqncR.Core.Rhythm;
using SqncR.Midi.Testing;
using SqncR.Testing.Assertions;
using SqncR.Testing.Audio;
using SqncR.Theory;

namespace SqncR.Integration.Tests;

/// <summary>
/// End-to-end tests validating the full signal chain:
/// GenerationEngine → MockMidiOutput → frequency validation via MidiFrequency.
/// </summary>
public class EndToEndTests
{
    [Fact]
    public async Task MelodyNotes_AreInScale()
    {
        var scale = Scale.PentatonicMinor(60); // C4 pentatonic minor
        var state = new GenerationState
        {
            Scale = scale,
            Tempo = 240, // Fast tempo for quicker test
            IsPlaying = false,
            NoteGenerator = new ScaleWalkGenerator()
        };

        var mockOutput = new MockMidiOutput();
        var engine = new GenerationEngine(state, mockOutput, NullLogger<GenerationEngine>.Instance);

        using var cts = new CancellationTokenSource();
        var engineTask = engine.StartAsync(cts.Token);

        // Start playback
        await engine.Commands.WriteAsync(new GenerationCommand.Start());
        await Task.Delay(600); // Let it generate some notes

        // Stop and collect
        await engine.Commands.WriteAsync(new GenerationCommand.Stop());
        cts.Cancel();
        try { await engineTask; } catch (OperationCanceledException) { }

        var noteOnEvents = mockOutput.Events
            .Where(e => e.Type == MidiEventType.NoteOn)
            .ToList();

        Assert.NotEmpty(noteOnEvents);

        // Every melody note should be in the scale
        foreach (var evt in noteOnEvents)
        {
            Assert.True(scale.ContainsNote(evt.Note),
                $"MIDI note {evt.Note} ({MidiFrequency.MidiToNoteName(evt.Note)}) is not in {scale.Name} scale");
        }
    }

    [Fact]
    public async Task MelodyNotes_MapToCorrectFrequencies()
    {
        var scale = Scale.Major(60); // C Major
        var state = new GenerationState
        {
            Scale = scale,
            Tempo = 240,
            IsPlaying = false,
            NoteGenerator = new ScaleWalkGenerator()
        };

        var mockOutput = new MockMidiOutput();
        var engine = new GenerationEngine(state, mockOutput, NullLogger<GenerationEngine>.Instance);

        using var cts = new CancellationTokenSource();
        var engineTask = engine.StartAsync(cts.Token);

        await engine.Commands.WriteAsync(new GenerationCommand.Start());
        await Task.Delay(600);

        await engine.Commands.WriteAsync(new GenerationCommand.Stop());
        cts.Cancel();
        try { await engineTask; } catch (OperationCanceledException) { }

        var noteOnEvents = mockOutput.Events
            .Where(e => e.Type == MidiEventType.NoteOn)
            .ToList();

        Assert.NotEmpty(noteOnEvents);

        // Verify MIDI → frequency conversion is musically correct
        foreach (var evt in noteOnEvents)
        {
            var freq = MidiFrequency.MidiToFrequency(evt.Note);
            var roundTrip = MidiFrequency.FrequencyToMidi(freq);
            Assert.Equal(evt.Note, roundTrip);

            // Frequency should be in human hearing range for normal MIDI notes
            Assert.InRange(freq, 20.0, 20000.0);
        }
    }

    [Fact]
    public async Task DrumPattern_GeneratesCorrectGmMappings()
    {
        var rockPattern = PatternLibrary.Get("rock");
        var state = new GenerationState
        {
            DrumPattern = rockPattern,
            Tempo = 240,
            IsPlaying = false,
            DrumChannel = 10,
            NoteGenerator = new ScaleWalkGenerator()
        };

        var mockOutput = new MockMidiOutput();
        var engine = new GenerationEngine(state, mockOutput, NullLogger<GenerationEngine>.Instance);

        using var cts = new CancellationTokenSource();
        var engineTask = engine.StartAsync(cts.Token);

        await engine.Commands.WriteAsync(new GenerationCommand.Start());
        await Task.Delay(800);

        await engine.Commands.WriteAsync(new GenerationCommand.Stop());
        cts.Cancel();
        try { await engineTask; } catch (OperationCanceledException) { }

        var drumEvents = mockOutput.Events
            .Where(e => e.Type == MidiEventType.NoteOn && e.Channel == 10)
            .ToList();

        Assert.NotEmpty(drumEvents);

        // All drum notes should be valid GM percussion notes
        var gmDrumMap = DrumMap.GeneralMidi;
        var validDrumNotes = gmDrumMap.Voices.Select(v => gmDrumMap.GetMidiNote(v)).ToHashSet();

        foreach (var evt in drumEvents)
        {
            Assert.Contains(evt.Note, validDrumNotes);
        }
    }

    [Fact]
    public async Task MultiChannel_NoCrossChannelContamination()
    {
        var scale = Scale.PentatonicMinor(60);
        var rockPattern = PatternLibrary.Get("rock");

        var state = new GenerationState
        {
            Scale = scale,
            DrumPattern = rockPattern,
            Tempo = 240,
            MelodicChannel = 1,
            DrumChannel = 10,
            IsPlaying = false,
            NoteGenerator = new ScaleWalkGenerator()
        };

        var mockOutput = new MockMidiOutput();
        var engine = new GenerationEngine(state, mockOutput, NullLogger<GenerationEngine>.Instance);

        using var cts = new CancellationTokenSource();
        var engineTask = engine.StartAsync(cts.Token);

        await engine.Commands.WriteAsync(new GenerationCommand.Start());
        await Task.Delay(800);

        await engine.Commands.WriteAsync(new GenerationCommand.Stop());
        cts.Cancel();
        try { await engineTask; } catch (OperationCanceledException) { }

        var noteOnEvents = mockOutput.Events
            .Where(e => e.Type == MidiEventType.NoteOn)
            .ToList();

        var ch1Events = noteOnEvents.Where(e => e.Channel == 1).ToList();
        var ch10Events = noteOnEvents.Where(e => e.Channel == 10).ToList();

        // Both channels should have events
        Assert.NotEmpty(ch1Events);
        Assert.NotEmpty(ch10Events);

        // Melody channel should NOT have GM drum notes (36=kick, 38=snare, 42=hat)
        var drumNoteNumbers = new[] { 36, 38, 42, 46 };
        foreach (var evt in ch1Events)
        {
            // Melody notes from PentatonicMinor(60) in octave 4 won't overlap with drum range
            Assert.DoesNotContain(evt.Note, drumNoteNumbers);
        }

        // Drum channel should only have valid drum notes
        var gmDrumMap = DrumMap.GeneralMidi;
        var validDrumNotes = gmDrumMap.Voices.Select(v => gmDrumMap.GetMidiNote(v)).ToHashSet();
        foreach (var evt in ch10Events)
        {
            Assert.Contains(evt.Note, validDrumNotes);
        }
    }

    [Fact]
    public void MidiToFrequency_MusicallyCorrectForScaleNotes()
    {
        // C Major scale starting at C4 (MIDI 60)
        var scale = Scale.Major(60);
        var notes = scale.GetNotesInOctave(4);

        // Expected frequencies for C Major (C4 through B4)
        var expectedFreqs = new Dictionary<int, double>
        {
            [60] = 261.63, // C4
            [62] = 293.66, // D4
            [64] = 329.63, // E4
            [65] = 349.23, // F4
            [67] = 392.00, // G4
            [69] = 440.00, // A4
            [71] = 493.88, // B4
        };

        foreach (var midiNote in notes)
        {
            var freq = MidiFrequency.MidiToFrequency(midiNote);
            Assert.True(expectedFreqs.ContainsKey(midiNote),
                $"Unexpected note {midiNote} in C Major scale");

            var expected = expectedFreqs[midiNote];
            var tolerance = expected * 0.01; // 1% tolerance
            Assert.InRange(freq, expected - tolerance, expected + tolerance);
        }
    }

    [Fact]
    public void FrequencyValidation_WithSpectralAnalysisHelpers()
    {
        // Generate a test tone at A4 (440 Hz) — the standard tuning reference
        var sampleRate = 44100;
        var samples = ToneGenerator.GenerateSineWave(440.0, sampleRate, 1.0);

        // Verify spectral analysis correctly identifies the frequency
        AudioAssertions.AssertFrequencyPresent(samples, sampleRate, 440.0);
        AudioAssertions.AssertDominantFrequency(samples, sampleRate, 440.0);

        // A tone at 440 Hz should NOT be detected as 880 Hz (octave above)
        AudioAssertions.AssertFrequencyAbsent(samples, sampleRate, 880.0);
    }

    [Fact]
    public void MidiNotesToFrequencies_FullChainSimulation()
    {
        // Simulate: scale notes → MIDI numbers → frequencies → spectral verification
        var scale = Scale.PentatonicMinor(60);
        var notes = scale.GetNotesInOctave(4);
        var sampleRate = 44100;

        foreach (var midiNote in notes)
        {
            var freq = MidiFrequency.MidiToFrequency(midiNote);

            // Generate a tone at this frequency
            var samples = ToneGenerator.GenerateSineWave(freq, sampleRate, 1.0);

            // Verify spectral analysis detects it
            var result = SpectralAnalyzer.Analyze(samples, sampleRate);
            Assert.NotNull(result.DominantFrequency);

            // Detected frequency should match within 5%
            var detected = result.DominantFrequency.Frequency;
            var lowerBound = freq * 0.95;
            var upperBound = freq * 1.05;
            Assert.InRange(detected, lowerBound, upperBound);
        }
    }
}
