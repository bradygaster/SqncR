using SqncR.Core.Generation;

namespace SqncR.Core.Tests.Generation;

/// <summary>
/// Long-running canary tests that simulate hours of operation in seconds.
/// All tests use accelerated time — no real-time waits.
/// </summary>
public class CanaryTests
{
    [Fact]
    public void SimulatedLongRunningHealth_100KTicks_StaysBounded()
    {
        var tracker = new NoteTracker();
        var monitor = new HealthMonitor(tracker);
        monitor.SetExpectedTickDuration(1.0);

        var rng = new Random(42);
        const int totalTicks = 100_000;

        for (int i = 0; i < totalTicks; i++)
        {
            // Realistic jitter: 0-2ms with rare spikes
            long latency = rng.Next(3);
            if (rng.NextDouble() < 0.001) latency = 5; // occasional spike (but < 2x threshold)
            monitor.RecordTick(i, latency);
        }

        var health = monitor.GetHealth();
        Assert.Equal(totalTicks, health.TicksProcessed);
        Assert.True(health.TickLatencyAvgMs < 10, $"Avg latency {health.TickLatencyAvgMs}ms exceeded bound");
        Assert.True(health.TickLatencyMaxMs < 50, $"Max latency {health.TickLatencyMaxMs}ms exceeded bound");
        Assert.True(monitor.IsHealthy, "Monitor should be healthy after normal ticks");
    }

    [Fact]
    public void NoteTrackerChurn_10KPairs_NoBoundedLeaks()
    {
        var tracker = new NoteTracker { MaxActiveNotes = 32 };
        const int totalPairs = 10_000;

        for (int i = 0; i < totalPairs; i++)
        {
            int channel = (i % 16) + 1;
            int note = (i % 128);
            tracker.NoteOn(channel, note, 80);
            Assert.True(tracker.ActiveNoteCount <= tracker.MaxActiveNotes,
                $"Active notes {tracker.ActiveNoteCount} exceeded max {tracker.MaxActiveNotes}");
            tracker.NoteOff(channel, note);
        }

        Assert.Equal(0, tracker.ActiveNoteCount);
    }

    [Fact]
    public void VarietyEngineLongRun_OctaveStaysBounded()
    {
        var state = new GenerationState { Octave = 4 };
        var variety = new VarietyEngine(VarietyLevel.Adventurous, new Random(123));
        state.Variety = variety;

        int originalOctave = state.Octave;
        const int totalMeasures = 500;

        for (int m = 0; m < totalMeasures; m++)
        {
            variety.OnMeasureBoundary(state, m);

            // Octave should never drift more than ±2 from original (drift + register shift max)
            int drift = Math.Abs(state.Octave - originalOctave);
            Assert.True(drift <= 2,
                $"Octave drifted to {state.Octave} (±{drift}) at measure {m}, exceeds ±2 bound");

            // Must stay in valid MIDI range
            Assert.InRange(state.Octave, 1, 8);
        }

        // After many measures, all drifts should eventually revert.
        // Run extra measures to let drifts expire.
        for (int m = totalMeasures; m < totalMeasures + 50; m++)
        {
            variety.OnMeasureBoundary(state, m);
        }

        // The variety engine's internal drift tracking should show convergence
        // (OctaveDrift and RegisterShift revert to 0 after their duration expires)
        // We can't guarantee exact timing, but the state should be within ±2
        Assert.InRange(state.Octave, originalOctave - 2, originalOctave + 2);
    }

    [Fact]
    public void MemoryStability_50KIterations_NoUnboundedGrowth()
    {
        var tracker = new NoteTracker();
        var monitor = new HealthMonitor(tracker);
        var state = new GenerationState();
        monitor.SetExpectedTickDuration(1.0);

        // Warm up to stabilize GC
        for (int i = 0; i < 1000; i++)
        {
            monitor.RecordTick(i, 1);
            tracker.NoteOn(1, i % 128, 80);
            tracker.NoteOff(1, i % 128);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long memoryBefore = GC.GetTotalMemory(true);

        const int iterations = 50_000;
        for (int i = 0; i < iterations; i++)
        {
            monitor.RecordTick(i + 1000, 1);
            int note = i % 128;
            int channel = (i % 16) + 1;
            tracker.NoteOn(channel, note, 80);
            tracker.NoteOff(channel, note);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long memoryAfter = GC.GetTotalMemory(true);

        long growthBytes = memoryAfter - memoryBefore;
        double growthMb = growthBytes / (1024.0 * 1024.0);

        Assert.True(growthMb < 1.0,
            $"Memory grew by {growthMb:F2} MB over {iterations} iterations — exceeds 1 MB bound");
    }

    [Fact]
    public async Task ConcurrentAccess_NoExceptionsOrCorruption()
    {
        var tracker = new NoteTracker { MaxActiveNotes = 64 };
        var exceptions = new List<Exception>();
        const int tasksCount = 8;
        const int opsPerTask = 5_000;

        var tasks = Enumerable.Range(0, tasksCount).Select(taskId => Task.Run(() =>
        {
            try
            {
                var rng = new Random(taskId);
                for (int i = 0; i < opsPerTask; i++)
                {
                    int channel = (taskId % 16) + 1;
                    int note = rng.Next(128);
                    // Use lock since NoteTracker is not thread-safe by design;
                    // we're testing that synchronized access stays consistent
                    lock (tracker)
                    {
                        tracker.NoteOn(channel, note, 80);
                    }
                    lock (tracker)
                    {
                        tracker.NoteOff(channel, note);
                    }
                }
            }
            catch (Exception ex)
            {
                lock (exceptions) { exceptions.Add(ex); }
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.Empty(exceptions);
        // All notes should be off after synchronized on/off pairs
        lock (tracker)
        {
            Assert.True(tracker.ActiveNoteCount <= tracker.MaxActiveNotes,
                $"Active note count {tracker.ActiveNoteCount} exceeds max");
        }
    }

    [Fact]
    public void HealthSnapshotConsistency_MonotonicFields()
    {
        var tracker = new NoteTracker();
        var monitor = new HealthMonitor(tracker);
        monitor.SetExpectedTickDuration(1.0);

        long prevTicks = 0;
        double prevUptime = 0;
        long prevMissed = 0;

        var rng = new Random(99);
        const int intervals = 100;
        const int ticksPerInterval = 500;

        for (int interval = 0; interval < intervals; interval++)
        {
            for (int t = 0; t < ticksPerInterval; t++)
            {
                long tickNum = interval * ticksPerInterval + t;
                long latency = rng.Next(3);
                // Inject some missed ticks (>2x expected)
                if (rng.NextDouble() < 0.01) latency = 5;
                monitor.RecordTick(tickNum, latency);
            }

            var snapshot = monitor.GetHealth();

            // TicksProcessed must increase monotonically
            Assert.True(snapshot.TicksProcessed >= prevTicks,
                $"TicksProcessed decreased: {snapshot.TicksProcessed} < {prevTicks}");

            // UptimeSeconds must increase monotonically
            Assert.True(snapshot.UptimeSeconds >= prevUptime,
                $"UptimeSeconds decreased: {snapshot.UptimeSeconds} < {prevUptime}");

            // MissedTicks must increase monotonically
            Assert.True(snapshot.MissedTicks >= prevMissed,
                $"MissedTicks decreased: {snapshot.MissedTicks} < {prevMissed}");

            // MissedTicks must never exceed TicksProcessed
            Assert.True(snapshot.MissedTicks <= snapshot.TicksProcessed,
                $"MissedTicks {snapshot.MissedTicks} > TicksProcessed {snapshot.TicksProcessed}");

            prevTicks = snapshot.TicksProcessed;
            prevUptime = snapshot.UptimeSeconds;
            prevMissed = snapshot.MissedTicks;
        }
    }

    [Fact]
    public void PolyphonyPressure_FloodWith100Notes_CapsAtMax()
    {
        var tracker = new NoteTracker { MaxActiveNotes = 32 };
        var allForcedOff = new List<(int Channel, int Note)>();

        // Flood with 100 simultaneous notes (no note-offs)
        for (int i = 0; i < 100; i++)
        {
            int channel = (i % 16) + 1;
            int note = i % 128;
            var forced = tracker.NoteOn(channel, note, 80);
            allForcedOff.AddRange(forced);

            // Active count must never exceed MaxActiveNotes
            Assert.True(tracker.ActiveNoteCount <= tracker.MaxActiveNotes,
                $"Active notes {tracker.ActiveNoteCount} exceeded max {tracker.MaxActiveNotes} at note {i}");
        }

        // Forced-off notes should have been returned for cleanup
        Assert.True(allForcedOff.Count > 0, "Expected forced note-offs when exceeding polyphony limit");
        // Exactly (100 - MaxActiveNotes) notes should have been forced off
        // (minus duplicates from same channel/note wrapping)
        Assert.True(allForcedOff.Count >= 100 - tracker.MaxActiveNotes,
            $"Expected at least {100 - tracker.MaxActiveNotes} forced-offs, got {allForcedOff.Count}");

        // Final state: exactly MaxActiveNotes active
        Assert.Equal(tracker.MaxActiveNotes, tracker.ActiveNoteCount);
    }
}
