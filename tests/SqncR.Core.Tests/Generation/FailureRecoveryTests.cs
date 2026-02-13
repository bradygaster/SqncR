using SqncR.Core.Generation;
using SqncR.Core.Rhythm;
using SqncR.Midi.Testing;
using Microsoft.Extensions.Logging.Abstractions;

namespace SqncR.Core.Tests.Generation;

public class FailureRecoveryTests : IDisposable
{
    private readonly GenerationState _state = new();
    private readonly MockMidiOutput _midi = new();
    private readonly GenerationEngine _engine;

    public FailureRecoveryTests()
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

    // --- Test 1: MIDI Send Failure — Log and Continue ---

    [Fact]
    public async Task MidiSendFailure_EngineLogsContinues()
    {
        var failingMidi = new EveryNthFailingMidiOutput(failEveryN: 3);
        using var engine = new GenerationEngine(
            _state,
            failingMidi,
            NullLogger<GenerationEngine>.Instance);

        _state.IsPlaying = true;
        _state.DrumPattern = PatternLibrary.Get("rock");

        using var cts = new CancellationTokenSource();
        await engine.StartAsync(cts.Token);
        await Task.Delay(800);
        await engine.StopAsync(CancellationToken.None);

        // Engine survived and some notes got through
        Assert.True(failingMidi.SuccessCount > 0, "Some MIDI sends should have succeeded");
        Assert.True(failingMidi.FailureCount > 0, "Some MIDI sends should have failed");
    }

    // --- Test 2: Intermittent MIDI Failure — Recovery ---

    [Fact]
    public async Task IntermittentMidiFailure_EngineRecoversAfterBurst()
    {
        var burstFailMidi = new BurstFailingMidiOutput(failCount: 5);
        var state = new GenerationState
        {
            IsPlaying = true,
            DrumPattern = PatternLibrary.Get("rock"),
            Tempo = 240, // faster tempo = more events in less time
        };
        using var engine = new GenerationEngine(
            state,
            burstFailMidi,
            NullLogger<GenerationEngine>.Instance);

        using var cts = new CancellationTokenSource();
        await engine.StartAsync(cts.Token);
        await Task.Delay(1500);
        await engine.StopAsync(CancellationToken.None);

        // Engine survived the burst and resumed normal operation
        Assert.True(burstFailMidi.SuccessCount > 0, "Notes should succeed after failure burst");
    }

    // --- Test 3: NoteTracker Under Polyphony Pressure ---

    [Fact]
    public void NoteTracker_PolyphonyPressure_ForcesOldestOff()
    {
        var tracker = new NoteTracker { MaxActiveNotes = 32 };
        var allForced = new List<(int Channel, int Note)>();

        for (int i = 0; i < 100; i++)
        {
            var forced = tracker.NoteOn(1, i, 80);
            allForced.AddRange(forced);
        }

        // Oldest notes were force-off'd
        Assert.True(allForced.Count >= 68, "At least 68 notes should have been forced off");
        // Active count never exceeds max
        Assert.True(tracker.ActiveNoteCount <= 32, "Active notes should never exceed max");
        Assert.Equal(32, tracker.ActiveNoteCount);
    }

    // --- Test 4: HealthMonitor Detects Degradation ---

    [Fact]
    public void HealthMonitor_IncreasingLatency_DetectsDegradation()
    {
        var tracker = new NoteTracker();
        var monitor = new HealthMonitor(tracker);
        monitor.SetExpectedTickDuration(1.0); // 1ms per tick

        // Feed ticks with increasing latency simulating overload
        for (int i = 0; i < 200; i++)
        {
            // Latency grows: starts at 0, ramps up way past threshold
            long latency = i / 2; // 0, 0, 1, 1, 2, 2, ... up to 99ms
            monitor.RecordTick(i, latency);
        }

        var health = monitor.GetHealth();
        Assert.True(health.MissedTicks > 0, "Should detect missed ticks from high latency");
        Assert.False(monitor.IsHealthy, "Should report unhealthy under sustained high latency");
    }

    // --- Test 5: AllNotesOff Panic Button ---

    [Fact]
    public void AllNotesOff_PanicButton_ClearsAllNotes()
    {
        var tracker = new NoteTracker();

        // Add 20 active notes across multiple channels
        for (int i = 0; i < 20; i++)
        {
            tracker.NoteOn(channel: (i % 3) + 1, note: 40 + i, velocity: 80);
        }

        Assert.Equal(20, tracker.ActiveNoteCount);

        var noteOffs = tracker.AllNotesOff();

        Assert.Equal(20, noteOffs.Count);
        Assert.Equal(0, tracker.ActiveNoteCount);
    }

    // --- Test 6: Engine Handles Null/Missing Components ---

    [Fact]
    public async Task Engine_NullVarietyEngine_RunsWithoutThrow()
    {
        // Explicitly ensure Variety is null
        _state.Variety = null;
        _state.IsPlaying = true;
        _state.DrumPattern = PatternLibrary.Get("rock");

        using var cts = new CancellationTokenSource();
        await _engine.StartAsync(cts.Token);

        // Let it run past at least one measure boundary
        await Task.Delay(600);

        await _engine.StopAsync(CancellationToken.None);
        // If we reach here, the engine handled null Variety without throwing
    }

    // --- Test 7: Multiple Rapid Start/Stop Cycles ---

    [Fact]
    public async Task Engine_RapidStartStopCycles_NoExceptionsOrLeaks()
    {
        _state.DrumPattern = PatternLibrary.Get("rock");

        for (int i = 0; i < 10; i++)
        {
            var midi = new MockMidiOutput();
            using var engine = new GenerationEngine(
                new GenerationState
                {
                    IsPlaying = true,
                    DrumPattern = PatternLibrary.Get("rock"),
                },
                midi,
                NullLogger<GenerationEngine>.Instance);

            using var cts = new CancellationTokenSource();
            await engine.StartAsync(cts.Token);
            await Task.Delay(50);
            await engine.StopAsync(CancellationToken.None);

            // No stuck notes — NoteTracker should be cleared on stop
            Assert.Equal(0, engine.NoteTracker.ActiveNoteCount);

            midi.Dispose();
        }
    }

    // --- Mock MIDI outputs ---

    /// <summary>Throws on every Nth MIDI send call (NoteOn or NoteOff).</summary>
    private sealed class EveryNthFailingMidiOutput : IMidiOutput
    {
        private readonly int _failEveryN;
        private int _callCount;

        public int SuccessCount { get; private set; }
        public int FailureCount { get; private set; }
        public string? CurrentDeviceName => "EveryNthFailingDevice";

        public EveryNthFailingMidiOutput(int failEveryN) => _failEveryN = failEveryN;

        public void SendNoteOn(int channel, int note, int velocity) => TrySend();
        public void SendNoteOff(int channel, int note) => TrySend();
        public void AllNotesOff(int channel) { }
        public void Dispose() { }

        private void TrySend()
        {
            _callCount++;
            if (_callCount % _failEveryN == 0)
            {
                FailureCount++;
                throw new InvalidOperationException("Simulated MIDI failure");
            }
            SuccessCount++;
        }
    }

    /// <summary>Fails SendNoteOn for the first N calls, then succeeds forever after.</summary>
    private sealed class BurstFailingMidiOutput : IMidiOutput
    {
        private readonly int _failCount;
        private int _noteOnCallCount;

        public int SuccessCount { get; private set; }
        public string? CurrentDeviceName => "BurstFailingDevice";

        public BurstFailingMidiOutput(int failCount) => _failCount = failCount;

        public void SendNoteOn(int channel, int note, int velocity)
        {
            _noteOnCallCount++;
            if (_noteOnCallCount <= _failCount)
                throw new InvalidOperationException("Simulated burst MIDI failure");
            SuccessCount++;
        }

        public void SendNoteOff(int channel, int note) { }
        public void AllNotesOff(int channel) { }
        public void Dispose() { }
    }
}
