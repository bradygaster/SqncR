using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SqncR.Core.Generation;

/// <summary>
/// Session-level telemetry for the generation engine.
/// Emits spans and metrics via System.Diagnostics (no OTel SDK dependency).
/// </summary>
public sealed class SessionTelemetry : IDisposable
{
    public static readonly ActivitySource ActivitySource = new("SqncR.Session");
    public static readonly Meter Meter = new("SqncR.Session");

    private Activity? _sessionActivity;
    private readonly Stopwatch _sessionStopwatch = new();
    private long _totalNotes;
    private long _varietyChanges;
    private long _healthSnapshots;

    /// <summary>Total notes emitted during the session.</summary>
    public long TotalNotes => Interlocked.Read(ref _totalNotes);

    /// <summary>Total variety changes during the session.</summary>
    public long VarietyChanges => Interlocked.Read(ref _varietyChanges);

    /// <summary>Total health snapshots recorded during the session.</summary>
    public long HealthSnapshotCount => Interlocked.Read(ref _healthSnapshots);

    // Instruments
    internal static readonly Counter<long> TotalNotesCounter =
        Meter.CreateCounter<long>("session.total_notes",
            description: "Total note events emitted in the session");

    internal static readonly Counter<long> VarietyChangesCounter =
        Meter.CreateCounter<long>("session.variety_changes",
            description: "Total variety engine decisions in the session");

    internal static readonly Counter<long> HealthSnapshotsCounter =
        Meter.CreateCounter<long>("session.health_snapshots",
            description: "Total health snapshots recorded");

    /// <summary>Observable gauge — reports current session duration in seconds.</summary>
    internal readonly ObservableGauge<double> SessionDurationGauge;

    public SessionTelemetry()
    {
        SessionDurationGauge = Meter.CreateObservableGauge<double>(
            "session.duration_seconds",
            () => _sessionStopwatch.Elapsed.TotalSeconds,
            unit: "s",
            description: "Current session duration in seconds");
    }

    /// <summary>
    /// Starts a session-level root span. Call once when generation starts.
    /// </summary>
    public Activity? StartSession(string sessionId, double tempo, string scaleName)
    {
        _sessionStopwatch.Restart();
        _sessionActivity = ActivitySource.StartActivity("Session", ActivityKind.Internal);
        _sessionActivity?.SetTag("session.id", sessionId);
        _sessionActivity?.SetTag("session.start_time", DateTimeOffset.UtcNow.ToString("o"));
        _sessionActivity?.SetTag("initial.tempo", tempo);
        _sessionActivity?.SetTag("initial.scale", scaleName);
        return _sessionActivity;
    }

    /// <summary>
    /// Ends the session span. Call once when generation stops.
    /// </summary>
    public void EndSession()
    {
        _sessionStopwatch.Stop();
        _sessionActivity?.Dispose();
        _sessionActivity = null;
    }

    /// <summary>
    /// Creates a child span under the session for a variety engine decision.
    /// </summary>
    public Activity? TraceVarietyDecision(string behavior, string detail, int measureNumber)
    {
        Interlocked.Increment(ref _varietyChanges);
        VarietyChangesCounter.Add(1);

        var activity = ActivitySource.StartActivity("VarietyDecision",
            ActivityKind.Internal, _sessionActivity?.Context ?? default);
        activity?.SetTag("variety.behavior", behavior);
        activity?.SetTag("variety.detail", detail);
        activity?.SetTag("variety.measure_number", measureNumber);
        return activity;
    }

    /// <summary>
    /// Creates a child span under the session recording a health snapshot.
    /// </summary>
    public Activity? RecordHealthSnapshot(HealthSnapshot snapshot)
    {
        Interlocked.Increment(ref _healthSnapshots);
        HealthSnapshotsCounter.Add(1);

        var activity = ActivitySource.StartActivity("HealthSnapshot",
            ActivityKind.Internal, _sessionActivity?.Context ?? default);
        activity?.SetTag("health.tick_latency_avg_ms", snapshot.TickLatencyAvgMs);
        activity?.SetTag("health.missed_ticks", snapshot.MissedTicks);
        activity?.SetTag("health.memory_mb", snapshot.MemoryUsageMb);
        activity?.SetTag("health.active_notes", snapshot.ActiveNoteCount);
        activity?.SetTag("health.uptime_seconds", snapshot.UptimeSeconds);
        return activity;
    }

    /// <summary>Increments the session note counter.</summary>
    public void RecordNote()
    {
        Interlocked.Increment(ref _totalNotes);
        TotalNotesCounter.Add(1);
    }

    /// <summary>Whether a session span is currently active.</summary>
    public bool IsSessionActive => _sessionActivity != null;

    public void Dispose()
    {
        EndSession();
    }
}
