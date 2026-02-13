using System.Diagnostics;
using System.Diagnostics.Metrics;
using SqncR.Core.Instruments;

namespace SqncR.Core.Tests.Instruments;

public class InstrumentTelemetryTests : IDisposable
{
    private readonly ActivityListener _activityListener;
    private readonly MeterListener _meterListener;
    private readonly List<Activity> _activities = new();
    private readonly List<(string Name, object Value, KeyValuePair<string, object?>[] Tags)> _recorded = new();

    public InstrumentTelemetryTests()
    {
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "SqncR.Instruments",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => _activities.Add(activity),
        };
        ActivitySource.AddActivityListener(_activityListener);

        _meterListener = new MeterListener();
        _meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "SqncR.Instruments")
                listener.EnableMeasurementEvents(instrument);
        };
        _meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            _recorded.Add((instrument.Name, value, tags.ToArray())));
        _meterListener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
            _recorded.Add((instrument.Name, value, tags.ToArray())));
        _meterListener.Start();
    }

    public void Dispose()
    {
        _activityListener.Dispose();
        _meterListener.Dispose();
    }

    [Fact]
    public void TraceNoteSent_CreatesSpanWithCorrectTags()
    {
        var telemetry = new InstrumentTelemetry();
        _activities.Clear();

        using var activity = telemetry.TraceNoteSent("synth-1", 3, 60, 100);

        Assert.NotNull(activity);
        Assert.Equal("NoteSent", activity!.OperationName);
        Assert.Equal("synth-1", activity.GetTagItem("instrument.id"));
        Assert.Equal(3, activity.GetTagItem("midi.channel"));
        Assert.Equal(60, activity.GetTagItem("midi.note"));
        Assert.Equal(100, activity.GetTagItem("midi.velocity"));
    }

    [Fact]
    public void TraceNoteSent_IncrementsNotesSentCounter()
    {
        var telemetry = new InstrumentTelemetry();
        _recorded.Clear();

        using var a1 = telemetry.TraceNoteSent("synth-1", 1, 60, 80);
        using var a2 = telemetry.TraceNoteSent("synth-1", 1, 64, 80);
        using var a3 = telemetry.TraceNoteSent("synth-2", 2, 48, 70);

        var notesSent = _recorded.Where(r => r.Name == "instrument.notes_sent").ToList();
        Assert.Equal(3, notesSent.Count);
    }

    [Fact]
    public void NotesSentCounter_TaggedByInstrumentId()
    {
        var telemetry = new InstrumentTelemetry();
        _recorded.Clear();

        using var a1 = telemetry.TraceNoteSent("bass-1", 3, 36, 90);

        var notesSent = _recorded.Where(r => r.Name == "instrument.notes_sent").ToList();
        Assert.Single(notesSent);
        var tag = notesSent[0].Tags.FirstOrDefault(t => t.Key == "instrument.id");
        Assert.Equal("bass-1", tag.Value);
    }

    [Fact]
    public void RecordPolyphony_TracksUtilization()
    {
        var telemetry = new InstrumentTelemetry();

        telemetry.RecordPolyphony("synth-1", 4, 8);

        _recorded.Clear();
        _meterListener.RecordObservableInstruments();

        var gauges = _recorded.Where(r => r.Name == "instrument.polyphony_utilization").ToList();
        Assert.NotEmpty(gauges);
        Assert.Contains(gauges, g => Math.Abs((double)g.Value - 50.0) < 0.01);
    }

    [Fact]
    public void RecordPolyphony_MultipleInstrumentsTrackedIndependently()
    {
        var telemetry = new InstrumentTelemetry();

        telemetry.RecordPolyphony("multi-synth-a", 2, 8);   // 25%
        telemetry.RecordPolyphony("multi-synth-b", 6, 8);   // 75%

        _recorded.Clear();
        _meterListener.RecordObservableInstruments();

        var gauges = _recorded.Where(r => r.Name == "instrument.polyphony_utilization").ToList();

        var synthA = gauges.First(g => g.Tags.Any(t => t.Key == "instrument.id" && (string?)t.Value == "multi-synth-a"));
        var synthB = gauges.First(g => g.Tags.Any(t => t.Key == "instrument.id" && (string?)t.Value == "multi-synth-b"));

        Assert.True(Math.Abs((double)synthA.Value - 25.0) < 0.01);
        Assert.True(Math.Abs((double)synthB.Value - 75.0) < 0.01);
    }

    [Fact]
    public void RecordLatency_RecordsHistogramValue()
    {
        var telemetry = new InstrumentTelemetry();
        _recorded.Clear();

        telemetry.RecordLatency("synth-1", 2.5);

        var latencies = _recorded.Where(r => r.Name == "instrument.latency_ms").ToList();
        Assert.Single(latencies);
        Assert.Equal(2.5, (double)latencies[0].Value);
    }

    [Fact]
    public void RecordError_IncrementsErrorCounter()
    {
        var telemetry = new InstrumentTelemetry();
        _recorded.Clear();

        telemetry.RecordError("synth-1");
        telemetry.RecordError("synth-1");

        var errors = _recorded.Where(r => r.Name == "instrument.errors").ToList();
        Assert.Equal(2, errors.Count);
    }

    [Fact]
    public void Meter_HasCorrectName()
    {
        Assert.Equal("SqncR.Instruments", InstrumentTelemetry.Meter.Name);
    }

    [Fact]
    public void ActivitySource_HasCorrectName()
    {
        Assert.Equal("SqncR.Instruments", InstrumentTelemetry.ActivitySource.Name);
    }

    // PerInstrumentNoteTracker tests

    [Fact]
    public void PerInstrumentNoteTracker_NoteOn_TracksPerInstrument()
    {
        var tracker = new PerInstrumentNoteTracker();

        tracker.NoteOn("synth-1", 1, 60);
        tracker.NoteOn("synth-1", 1, 64);
        tracker.NoteOn("synth-2", 2, 48);

        Assert.Equal(2, tracker.GetActiveCount("synth-1"));
        Assert.Equal(1, tracker.GetActiveCount("synth-2"));
    }

    [Fact]
    public void PerInstrumentNoteTracker_NoteOff_RemovesNote()
    {
        var tracker = new PerInstrumentNoteTracker();

        tracker.NoteOn("synth-1", 1, 60);
        tracker.NoteOn("synth-1", 1, 64);
        tracker.NoteOff("synth-1", 1, 60);

        Assert.Equal(1, tracker.GetActiveCount("synth-1"));
    }

    [Fact]
    public void PerInstrumentNoteTracker_AllNotesOff_ClearsOnlySpecifiedInstrument()
    {
        var tracker = new PerInstrumentNoteTracker();

        tracker.NoteOn("synth-1", 1, 60);
        tracker.NoteOn("synth-1", 1, 64);
        tracker.NoteOn("synth-2", 2, 48);

        var cleared = tracker.AllNotesOff("synth-1");

        Assert.Equal(2, cleared.Count);
        Assert.Equal(0, tracker.GetActiveCount("synth-1"));
        Assert.Equal(1, tracker.GetActiveCount("synth-2"));
    }

    [Fact]
    public void PerInstrumentNoteTracker_GetActiveCount_ReturnsZeroForUnknownInstrument()
    {
        var tracker = new PerInstrumentNoteTracker();
        Assert.Equal(0, tracker.GetActiveCount("unknown"));
    }

    [Fact]
    public void PerInstrumentNoteTracker_AllNotesOff_ReturnsEmptyForUnknownInstrument()
    {
        var tracker = new PerInstrumentNoteTracker();
        var cleared = tracker.AllNotesOff("unknown");
        Assert.Empty(cleared);
    }
}
