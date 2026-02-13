using SqncR.Core.Generation;
using SqncR.Core.Rhythm;
using SqncR.Midi.Testing;
using SqncR.Theory;
using Microsoft.Extensions.Logging.Abstractions;

namespace SqncR.Core.Tests.Generation;

public class GenerationEngineTests : IDisposable
{
    private readonly MockMidiOutput _midi = new();
    private readonly GenerationState _state = new();
    private readonly GenerationEngine _engine;

    public GenerationEngineTests()
    {
        _engine = new GenerationEngine(
            _state,
            _midi,
            NullLogger<GenerationEngine>.Instance);
    }

    public void Dispose()
    {
        _engine.Dispose();
        _midi.Dispose();
    }

    [Fact]
    public async Task Engine_StartsAndStops_Cleanly()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var task = _engine.StartAsync(cts.Token);
        await Task.Delay(50);
        await _engine.StopAsync(CancellationToken.None);

        // Should complete without throwing
        Assert.True(task.IsCompletedSuccessfully || task.IsCompleted);
    }

    [Fact]
    public async Task Engine_SendsAllNotesOff_OnShutdown()
    {
        _state.IsPlaying = true;
        _state.DrumPattern = PatternLibrary.Get("rock");

        using var cts = new CancellationTokenSource();
        await _engine.StartAsync(cts.Token);

        // Let it run briefly to emit some events
        await Task.Delay(200);

        await _engine.StopAsync(CancellationToken.None);

        var allNotesOffEvents = _midi.Events
            .Where(e => e.Type == MidiEventType.AllNotesOff)
            .ToList();

        Assert.NotEmpty(allNotesOffEvents);
        // Should have AllNotesOff for both melodic and drum channels
        Assert.Contains(allNotesOffEvents, e => e.Channel == _state.MelodicChannel);
        Assert.Contains(allNotesOffEvents, e => e.Channel == _state.DrumChannel);
    }

    [Fact]
    public async Task Engine_ProcessesSetTempo_Command()
    {
        using var cts = new CancellationTokenSource();
        await _engine.StartAsync(cts.Token);

        await _engine.Commands.WriteAsync(new GenerationCommand.SetTempo(140));
        await Task.Delay(50);

        Assert.Equal(140, _state.Tempo);

        await _engine.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Engine_ProcessesSetScale_Command()
    {
        using var cts = new CancellationTokenSource();
        await _engine.StartAsync(cts.Token);

        var newScale = Scale.Major(60);
        await _engine.Commands.WriteAsync(new GenerationCommand.SetScale(newScale));
        await Task.Delay(50);

        Assert.Equal(newScale, _state.Scale);

        await _engine.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Engine_ProcessesSetPattern_Command()
    {
        using var cts = new CancellationTokenSource();
        await _engine.StartAsync(cts.Token);

        var pattern = PatternLibrary.Get("house");
        await _engine.Commands.WriteAsync(new GenerationCommand.SetPattern(pattern));
        await Task.Delay(50);

        Assert.NotNull(_state.DrumPattern);
        Assert.Equal("house", _state.DrumPattern.Name);

        await _engine.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Engine_SendsNoteEvents_ThroughMidiOutput()
    {
        _state.IsPlaying = true;
        _state.DrumPattern = PatternLibrary.Get("rock");

        using var cts = new CancellationTokenSource();
        await _engine.StartAsync(cts.Token);

        // Wait enough time for events to be emitted (at 120 BPM, one beat = 500ms)
        await Task.Delay(600);

        await _engine.StopAsync(CancellationToken.None);

        var noteOnEvents = _midi.Events.Where(e => e.Type == MidiEventType.NoteOn).ToList();
        Assert.NotEmpty(noteOnEvents);
    }

    [Fact]
    public async Task Engine_EmitsDrumEvents_OnCorrectChannel()
    {
        _state.IsPlaying = true;
        _state.DrumChannel = 10;
        _state.DrumPattern = PatternLibrary.Get("rock");

        using var cts = new CancellationTokenSource();
        await _engine.StartAsync(cts.Token);

        await Task.Delay(600);
        await _engine.StopAsync(CancellationToken.None);

        var drumEvents = _midi.Events
            .Where(e => e.Type == MidiEventType.NoteOn && e.Channel == 10)
            .ToList();
        Assert.NotEmpty(drumEvents);
    }

    [Fact]
    public async Task Engine_EmitsMelodyEvents_OnCorrectChannel()
    {
        _state.IsPlaying = true;
        _state.MelodicChannel = 1;
        _state.DrumPattern = PatternLibrary.Get("rock");

        using var cts = new CancellationTokenSource();
        await _engine.StartAsync(cts.Token);

        await Task.Delay(1500);
        await _engine.StopAsync(CancellationToken.None);

        var melodyNoteOn = _midi.Events
            .Where(e => e.Type == MidiEventType.NoteOn && e.Channel == 1)
            .ToList();
        Assert.NotEmpty(melodyNoteOn);
    }

    [Fact]
    public async Task Engine_CommandQueue_IsThreadSafe()
    {
        using var cts = new CancellationTokenSource();
        await _engine.StartAsync(cts.Token);

        // Enqueue commands concurrently from multiple threads
        var tasks = Enumerable.Range(0, 20).Select(i =>
            Task.Run(async () =>
            {
                await _engine.Commands.WriteAsync(new GenerationCommand.SetTempo(100 + i));
            }));

        await Task.WhenAll(tasks);
        await Task.Delay(100);

        // All commands processed without errors; tempo should be one of the set values
        Assert.InRange(_state.Tempo, 100, 119);

        await _engine.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Engine_StartCommand_BeginsPlayback()
    {
        // Engine starts with IsPlaying = false by default
        Assert.False(_state.IsPlaying);

        using var cts = new CancellationTokenSource();
        await _engine.StartAsync(cts.Token);

        await _engine.Commands.WriteAsync(new GenerationCommand.Start());
        await Task.Delay(50);

        Assert.True(_state.IsPlaying);

        await _engine.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Engine_StopCommand_HaltsPlayback_AndSendsAllNotesOff()
    {
        _state.IsPlaying = true;
        _state.DrumPattern = PatternLibrary.Get("rock");

        using var cts = new CancellationTokenSource();
        await _engine.StartAsync(cts.Token);

        await Task.Delay(200);
        _midi.Reset();

        await _engine.Commands.WriteAsync(new GenerationCommand.Stop());
        await Task.Delay(100);

        Assert.False(_state.IsPlaying);

        var allNotesOffEvents = _midi.Events
            .Where(e => e.Type == MidiEventType.AllNotesOff)
            .ToList();
        Assert.NotEmpty(allNotesOffEvents);

        await _engine.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Engine_ProcessesSetOctave_Command()
    {
        using var cts = new CancellationTokenSource();
        await _engine.StartAsync(cts.Token);

        await _engine.Commands.WriteAsync(new GenerationCommand.SetOctave(5));
        await Task.Delay(50);

        Assert.Equal(5, _state.Octave);

        await _engine.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Engine_Ppq_Is480()
    {
        Assert.Equal(480, GenerationEngine.Ppq);
    }

    [Fact]
    public async Task Engine_MelodyNotes_AreInScale()
    {
        var scale = Scale.PentatonicMinor(60); // C pentatonic minor
        _state.Scale = scale;
        _state.IsPlaying = true;
        _state.Octave = 4;
        _state.DrumPattern = PatternLibrary.Get("rock");

        using var cts = new CancellationTokenSource();
        await _engine.StartAsync(cts.Token);

        await Task.Delay(1200);
        await _engine.StopAsync(CancellationToken.None);

        var melodyNoteOns = _midi.Events
            .Where(e => e.Type == MidiEventType.NoteOn && e.Channel == _state.MelodicChannel)
            .ToList();

        Assert.NotEmpty(melodyNoteOns);

        foreach (var noteEvent in melodyNoteOns)
        {
            Assert.True(scale.ContainsNote(noteEvent.Note),
                $"Melody note {noteEvent.Note} is not in scale {scale.Name}");
        }
    }
}
