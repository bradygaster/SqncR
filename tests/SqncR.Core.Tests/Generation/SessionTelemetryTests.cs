using System.Diagnostics;
using System.Diagnostics.Metrics;
using SqncR.Core.Generation;

namespace SqncR.Core.Tests.Generation;

public class SessionTelemetryTests : IDisposable
{
    private readonly ActivityListener _activityListener;
    private readonly MeterListener _meterListener;
    private readonly List<Activity> _activities = new();
    private readonly List<(string Name, object Value)> _recorded = new();

    public SessionTelemetryTests()
    {
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "SqncR.Session",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => _activities.Add(activity),
        };
        ActivitySource.AddActivityListener(_activityListener);

        _meterListener = new MeterListener();
        _meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "SqncR.Session")
                listener.EnableMeasurementEvents(instrument);
        };
        _meterListener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
            _recorded.Add((instrument.Name, value)));
        _meterListener.SetMeasurementEventCallback<double>((instrument, value, _, _) =>
            _recorded.Add((instrument.Name, value)));
        _meterListener.Start();
    }

    public void Dispose()
    {
        _activityListener.Dispose();
        _meterListener.Dispose();
    }

    [Fact]
    public void StartSession_CreatesActivity()
    {
        using var telemetry = new SessionTelemetry();
        _activities.Clear();

        var activity = telemetry.StartSession("test-session", 120.0, "C minor pentatonic");

        Assert.NotNull(activity);
        Assert.Equal("Session", activity!.OperationName);
        Assert.True(telemetry.IsSessionActive);

        telemetry.EndSession();
    }

    [Fact]
    public void StartSession_SetsCorrectTags()
    {
        using var telemetry = new SessionTelemetry();

        var activity = telemetry.StartSession("abc123", 140.0, "D major");

        Assert.Equal("abc123", activity?.GetTagItem("session.id"));
        Assert.Equal(140.0, activity?.GetTagItem("initial.tempo"));
        Assert.Equal("D major", activity?.GetTagItem("initial.scale"));
        Assert.NotNull(activity?.GetTagItem("session.start_time"));

        telemetry.EndSession();
    }

    [Fact]
    public void EndSession_StopsActivity()
    {
        using var telemetry = new SessionTelemetry();

        telemetry.StartSession("test", 120.0, "C major");
        Assert.True(telemetry.IsSessionActive);

        telemetry.EndSession();
        Assert.False(telemetry.IsSessionActive);
    }

    [Fact]
    public void TraceVarietyDecision_CreatesChildSpan()
    {
        using var telemetry = new SessionTelemetry();
        telemetry.StartSession("test", 120.0, "C major");
        _activities.Clear();

        using var activity = telemetry.TraceVarietyDecision("octave_drift", "octave changed to 5", 8);

        Assert.NotNull(activity);
        Assert.Equal("VarietyDecision", activity!.OperationName);
        Assert.Equal("octave_drift", activity.GetTagItem("variety.behavior"));
        Assert.Equal("octave changed to 5", activity.GetTagItem("variety.detail"));
        Assert.Equal(8, activity.GetTagItem("variety.measure_number"));

        telemetry.EndSession();
    }

    [Fact]
    public void RecordHealthSnapshot_CreatesChildSpanWithCorrectTags()
    {
        using var telemetry = new SessionTelemetry();
        telemetry.StartSession("test", 120.0, "C major");
        _activities.Clear();

        var snapshot = new HealthSnapshot
        {
            TickLatencyAvgMs = 1.5,
            MissedTicks = 3,
            MemoryUsageMb = 128.5,
            ActiveNoteCount = 4,
            UptimeSeconds = 300.0,
            TicksProcessed = 50000,
            TickLatencyMaxMs = 5.0,
        };

        using var activity = telemetry.RecordHealthSnapshot(snapshot);

        Assert.NotNull(activity);
        Assert.Equal("HealthSnapshot", activity!.OperationName);
        Assert.Equal(1.5, activity.GetTagItem("health.tick_latency_avg_ms"));
        Assert.Equal(3L, activity.GetTagItem("health.missed_ticks"));
        Assert.Equal(128.5, activity.GetTagItem("health.memory_mb"));
        Assert.Equal(4, activity.GetTagItem("health.active_notes"));
        Assert.Equal(300.0, activity.GetTagItem("health.uptime_seconds"));

        telemetry.EndSession();
    }

    [Fact]
    public void RecordNote_IncrementsCounter()
    {
        using var telemetry = new SessionTelemetry();
        _recorded.Clear();

        telemetry.RecordNote();
        telemetry.RecordNote();
        telemetry.RecordNote();
        _meterListener.RecordObservableInstruments();

        var notes = _recorded.Where(r => r.Name == "session.total_notes").ToList();
        Assert.Equal(3, notes.Count);
        Assert.Equal(3, telemetry.TotalNotes);
    }

    [Fact]
    public void TraceVarietyDecision_IncrementsVarietyCounter()
    {
        using var telemetry = new SessionTelemetry();
        telemetry.StartSession("test", 120.0, "C major");
        _recorded.Clear();

        using var a1 = telemetry.TraceVarietyDecision("octave_drift", "up", 1);
        using var a2 = telemetry.TraceVarietyDecision("velocity_drift", "drift=10", 2);
        _meterListener.RecordObservableInstruments();

        var changes = _recorded.Where(r => r.Name == "session.variety_changes").ToList();
        Assert.Equal(2, changes.Count);
        Assert.Equal(2, telemetry.VarietyChanges);

        telemetry.EndSession();
    }

    [Fact]
    public void RecordHealthSnapshot_IncrementsSnapshotCounter()
    {
        using var telemetry = new SessionTelemetry();
        telemetry.StartSession("test", 120.0, "C major");
        _recorded.Clear();

        var snapshot = new HealthSnapshot
        {
            TickLatencyAvgMs = 0,
            MissedTicks = 0,
            MemoryUsageMb = 50,
            ActiveNoteCount = 0,
            UptimeSeconds = 60,
            TicksProcessed = 1000,
            TickLatencyMaxMs = 0,
        };

        using var a1 = telemetry.RecordHealthSnapshot(snapshot);
        _meterListener.RecordObservableInstruments();

        var snapshots = _recorded.Where(r => r.Name == "session.health_snapshots").ToList();
        Assert.Single(snapshots);
        Assert.Equal(1, telemetry.HealthSnapshotCount);

        telemetry.EndSession();
    }

    [Fact]
    public void Meter_HasCorrectName()
    {
        Assert.Equal("SqncR.Session", SessionTelemetry.Meter.Name);
    }

    [Fact]
    public void ActivitySource_HasCorrectName()
    {
        Assert.Equal("SqncR.Session", SessionTelemetry.ActivitySource.Name);
    }

    [Fact]
    public void SessionDurationGauge_ReportsTime()
    {
        using var telemetry = new SessionTelemetry();
        telemetry.StartSession("test", 120.0, "C major");

        // Allow a brief moment so duration is >0
        Thread.Sleep(50);

        _recorded.Clear();
        _meterListener.RecordObservableInstruments();

        var durations = _recorded.Where(r => r.Name == "session.duration_seconds").ToList();
        Assert.NotEmpty(durations);
        Assert.True(durations.Any(d => (double)d.Value > 0),
            "At least one duration measurement should be > 0");

        telemetry.EndSession();
    }
}
