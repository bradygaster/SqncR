using SqncR.Core.Generation;
using SqncR.Core.Rhythm;
using SqncR.Midi.Testing;
using Microsoft.Extensions.Logging.Abstractions;

namespace SqncR.Core.Tests.Generation;

public class StabilityTests : IDisposable
{
    private readonly MockMidiOutput _midi = new();
    private readonly GenerationState _state = new();
    private readonly GenerationEngine _engine;

    public StabilityTests()
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

    // --- NoteTracker tests ---

    [Fact]
    public void NoteTracker_TracksActiveNotes()
    {
        var tracker = new NoteTracker();
        tracker.NoteOn(1, 60, 80);
        tracker.NoteOn(1, 64, 80);

        var active = tracker.GetActiveNotes();
        Assert.Equal(2, active.Count);
        Assert.Contains((1, 60), active);
        Assert.Contains((1, 64), active);
    }

    [Fact]
    public void NoteTracker_NoteOff_RemovesNote()
    {
        var tracker = new NoteTracker();
        tracker.NoteOn(1, 60, 80);
        tracker.NoteOn(1, 64, 80);
        tracker.NoteOff(1, 60);

        var active = tracker.GetActiveNotes();
        Assert.Single(active);
        Assert.Contains((1, 64), active);
    }

    [Fact]
    public void NoteTracker_EnforcesMaxActiveNotes()
    {
        var tracker = new NoteTracker { MaxActiveNotes = 3 };
        tracker.NoteOn(1, 60, 80);
        tracker.NoteOn(1, 61, 80);
        tracker.NoteOn(1, 62, 80);

        // Adding a 4th note should force the oldest off
        var forced = tracker.NoteOn(1, 63, 80);
        Assert.Single(forced);
        Assert.Equal((1, 60), forced[0]);

        Assert.Equal(3, tracker.ActiveNoteCount);
        Assert.DoesNotContain((1, 60), tracker.GetActiveNotes());
        Assert.Contains((1, 63), tracker.GetActiveNotes());
    }

    [Fact]
    public void NoteTracker_AllNotesOff_ReturnsAllActive()
    {
        var tracker = new NoteTracker();
        tracker.NoteOn(1, 60, 80);
        tracker.NoteOn(2, 64, 80);
        tracker.NoteOn(10, 36, 100);

        var allOff = tracker.AllNotesOff();
        Assert.Equal(3, allOff.Count);
        Assert.Equal(0, tracker.ActiveNoteCount);
    }

    [Fact]
    public void NoteTracker_DuplicateNoteOn_RetriggersWithoutLeak()
    {
        var tracker = new NoteTracker();
        tracker.NoteOn(1, 60, 80);
        tracker.NoteOn(1, 60, 100); // re-trigger same note

        Assert.Equal(1, tracker.ActiveNoteCount);
    }

    // --- HealthMonitor tests ---

    [Fact]
    public void HealthMonitor_TracksTickLatency()
    {
        var tracker = new NoteTracker();
        var monitor = new HealthMonitor(tracker);

        monitor.RecordTick(0, 1);
        monitor.RecordTick(1, 2);
        monitor.RecordTick(2, 3);

        var health = monitor.GetHealth();
        Assert.Equal(2.0, health.TickLatencyAvgMs);
        Assert.Equal(3.0, health.TickLatencyMaxMs);
        Assert.Equal(3, health.TicksProcessed);
    }

    [Fact]
    public void HealthMonitor_DetectsMissedTicks()
    {
        var tracker = new NoteTracker();
        var monitor = new HealthMonitor(tracker);
        monitor.SetExpectedTickDuration(1.0); // 1ms per tick expected

        monitor.RecordTick(0, 0); // on time
        monitor.RecordTick(1, 1); // slightly late
        monitor.RecordTick(2, 3); // >2x late → missed

        var health = monitor.GetHealth();
        Assert.Equal(1, health.MissedTicks);
    }

    [Fact]
    public void HealthMonitor_ReportsMemoryUsage()
    {
        var tracker = new NoteTracker();
        var monitor = new HealthMonitor(tracker);

        var health = monitor.GetHealth();
        Assert.True(health.MemoryUsageMb > 0);
    }

    [Fact]
    public void HealthMonitor_ReportsActiveNoteCount()
    {
        var tracker = new NoteTracker();
        var monitor = new HealthMonitor(tracker);

        tracker.NoteOn(1, 60, 80);
        tracker.NoteOn(1, 64, 80);

        var health = monitor.GetHealth();
        Assert.Equal(2, health.ActiveNoteCount);
    }

    [Fact]
    public void HealthMonitor_IsHealthy_TrueWhenNormal()
    {
        var tracker = new NoteTracker();
        var monitor = new HealthMonitor(tracker);
        monitor.SetExpectedTickDuration(1.0);

        for (int i = 0; i < 100; i++)
            monitor.RecordTick(i, 0);

        Assert.True(monitor.IsHealthy);
    }

    [Fact]
    public void HealthMonitor_GetHealth_ReturnsValidSnapshot()
    {
        var tracker = new NoteTracker();
        var monitor = new HealthMonitor(tracker);
        monitor.SetExpectedTickDuration(1.0);

        for (int i = 0; i < 10; i++)
            monitor.RecordTick(i, i);

        var health = monitor.GetHealth();
        Assert.Equal(10, health.TicksProcessed);
        Assert.True(health.UptimeSeconds >= 0);
        Assert.True(health.MemoryUsageMb > 0);
        Assert.True(health.TickLatencyAvgMs >= 0);
        Assert.True(health.TickLatencyMaxMs >= health.TickLatencyAvgMs);
    }

    // --- Engine integration tests ---

    [Fact]
    public async Task Engine_MidiSendFailure_DoesNotCrash()
    {
        var failingMidi = new FailingMidiOutput();
        var engine = new GenerationEngine(
            _state,
            failingMidi,
            NullLogger<GenerationEngine>.Instance);

        _state.IsPlaying = true;
        _state.DrumPattern = PatternLibrary.Get("rock");

        using var cts = new CancellationTokenSource();
        await engine.StartAsync(cts.Token);

        // Let it run long enough for events to be emitted — engine should not crash
        await Task.Delay(600);

        await engine.StopAsync(CancellationToken.None);
        engine.Dispose();
        // If we reach here, the engine survived MIDI failures
    }

    [Fact]
    public async Task Engine_AllNotesOffCommand_ClearsTrackedNotes()
    {
        _state.IsPlaying = true;
        _state.DrumPattern = PatternLibrary.Get("rock");

        using var cts = new CancellationTokenSource();
        await _engine.StartAsync(cts.Token);

        // Let it play to accumulate some notes
        await Task.Delay(600);

        // Send all notes off
        await _engine.Commands.WriteAsync(new GenerationCommand.AllNotesOff());
        await Task.Delay(100);

        // Verify AllNotesOff was sent
        var allNotesOffEvents = _midi.Events
            .Where(e => e.Type == MidiEventType.AllNotesOff)
            .ToList();
        Assert.NotEmpty(allNotesOffEvents);

        await _engine.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Engine_OnShutdown_TrackedNotesAreCleared()
    {
        _state.IsPlaying = true;
        _state.DrumPattern = PatternLibrary.Get("rock");

        using var cts = new CancellationTokenSource();
        await _engine.StartAsync(cts.Token);

        await Task.Delay(600);
        await _engine.StopAsync(CancellationToken.None);

        // NoteTracker should be empty after shutdown
        Assert.Equal(0, _engine.NoteTracker.ActiveNoteCount);
    }

    /// <summary>Mock that throws on every MIDI send to test error recovery.</summary>
    private class FailingMidiOutput : IMidiOutput
    {
        public string? CurrentDeviceName => "FailingDevice";
        public void SendNoteOn(int channel, int note, int velocity)
            => throw new InvalidOperationException("Simulated MIDI failure");
        public void SendNoteOff(int channel, int note)
            => throw new InvalidOperationException("Simulated MIDI failure");
        public void AllNotesOff(int channel) { }
        public void Dispose() { }
    }
}
