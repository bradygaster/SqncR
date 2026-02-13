using System.Diagnostics;

namespace SqncR.Core.Generation;

/// <summary>
/// Monitors runtime health of the generation engine for long-running stability.
/// Tracks tick latency, missed ticks, memory usage, and uptime.
/// </summary>
public sealed class HealthMonitor
{
    private const int RollingWindowSize = 1000;

    private readonly double[] _latencies = new double[RollingWindowSize];
    private int _latencyIndex;
    private int _latencyCount;
    private long _ticksProcessed;
    private long _missedTicks;
    private readonly Stopwatch _uptimeStopwatch = Stopwatch.StartNew();
    private readonly NoteTracker _noteTracker;
    private double _expectedTickDurationMs;

    public HealthMonitor(NoteTracker noteTracker)
    {
        _noteTracker = noteTracker;
    }

    /// <summary>Sets the expected tick duration for missed-tick detection.</summary>
    public void SetExpectedTickDuration(double milliseconds)
    {
        _expectedTickDurationMs = milliseconds;
    }

    /// <summary>Records a tick with its elapsed time in milliseconds.</summary>
    public void RecordTick(long tickNumber, long elapsedMs)
    {
        _latencies[_latencyIndex] = elapsedMs;
        _latencyIndex = (_latencyIndex + 1) % RollingWindowSize;
        if (_latencyCount < RollingWindowSize) _latencyCount++;

        _ticksProcessed++;

        if (_expectedTickDurationMs > 0 && elapsedMs > _expectedTickDurationMs * 2)
        {
            _missedTicks++;
        }
    }

    /// <summary>Returns a snapshot of the current health state.</summary>
    public HealthSnapshot GetHealth()
    {
        var (avg, max) = ComputeLatencyStats();

        return new HealthSnapshot
        {
            TickLatencyAvgMs = avg,
            TickLatencyMaxMs = max,
            ActiveNoteCount = _noteTracker.ActiveNoteCount,
            MemoryUsageMb = GC.GetTotalMemory(false) / (1024.0 * 1024.0),
            UptimeSeconds = _uptimeStopwatch.Elapsed.TotalSeconds,
            TicksProcessed = _ticksProcessed,
            MissedTicks = _missedTicks,
        };
    }

    /// <summary>True if no critical issues detected.</summary>
    public bool IsHealthy
    {
        get
        {
            var snapshot = GetHealth();
            // Unhealthy if >5% ticks missed or latency avg >50ms
            if (snapshot.TicksProcessed > 0 &&
                (double)snapshot.MissedTicks / snapshot.TicksProcessed > 0.05)
                return false;
            if (snapshot.TickLatencyAvgMs > 50)
                return false;
            return true;
        }
    }

    private (double Avg, double Max) ComputeLatencyStats()
    {
        if (_latencyCount == 0)
            return (0, 0);

        double sum = 0;
        double max = 0;
        for (int i = 0; i < _latencyCount; i++)
        {
            sum += _latencies[i];
            if (_latencies[i] > max) max = _latencies[i];
        }

        return (sum / _latencyCount, max);
    }
}

/// <summary>Point-in-time health snapshot of the generation engine.</summary>
public record HealthSnapshot
{
    public double TickLatencyAvgMs { get; init; }
    public double TickLatencyMaxMs { get; init; }
    public int ActiveNoteCount { get; init; }
    public double MemoryUsageMb { get; init; }
    public double UptimeSeconds { get; init; }
    public long TicksProcessed { get; init; }
    public long MissedTicks { get; init; }
}
