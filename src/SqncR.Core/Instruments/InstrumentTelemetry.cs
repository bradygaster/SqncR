using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SqncR.Core.Instruments;

/// <summary>
/// Per-instrument telemetry. Emits spans and metrics via System.Diagnostics (no OTel SDK dependency).
/// </summary>
public sealed class InstrumentTelemetry
{
    public static readonly ActivitySource ActivitySource = new("SqncR.Instruments");
    public static readonly Meter Meter = new("SqncR.Instruments");

    internal static readonly Counter<long> NotesSentCounter =
        Meter.CreateCounter<long>("instrument.notes_sent",
            description: "Total notes sent per instrument");

    internal static readonly Histogram<double> LatencyHistogram =
        Meter.CreateHistogram<double>("instrument.latency_ms",
            unit: "ms",
            description: "Per-instrument send latency in milliseconds");

    internal static readonly Counter<long> ErrorsCounter =
        Meter.CreateCounter<long>("instrument.errors",
            description: "Errors per instrument");

    private readonly ConcurrentDictionary<string, double> _polyphonyUtilization = new();

    /// <summary>Observable gauge — reports polyphony utilization per instrument.</summary>
    internal readonly ObservableGauge<double> PolyphonyUtilizationGauge;

    public InstrumentTelemetry()
    {
        PolyphonyUtilizationGauge = Meter.CreateObservableGauge<double>(
            "instrument.polyphony_utilization",
            observeValues: () => _polyphonyUtilization.Select(kvp =>
                new Measurement<double>(kvp.Value,
                    new KeyValuePair<string, object?>("instrument.id", kvp.Key))),
            unit: "%",
            description: "Polyphony utilization percentage per instrument");
    }

    /// <summary>
    /// Creates a span for a note sent to a specific instrument.
    /// </summary>
    public Activity? TraceNoteSent(string instrumentId, int channel, int note, int velocity)
    {
        NotesSentCounter.Add(1, new KeyValuePair<string, object?>("instrument.id", instrumentId));

        var activity = ActivitySource.StartActivity("NoteSent");
        activity?.SetTag("instrument.id", instrumentId);
        activity?.SetTag("midi.channel", channel);
        activity?.SetTag("midi.note", note);
        activity?.SetTag("midi.velocity", velocity);
        return activity;
    }

    /// <summary>
    /// Records polyphony snapshot for an instrument.
    /// </summary>
    public void RecordPolyphony(string instrumentId, int activeVoices, int maxVoices)
    {
        double utilizationPct = maxVoices > 0 ? (double)activeVoices / maxVoices * 100.0 : 0.0;
        _polyphonyUtilization[instrumentId] = utilizationPct;
    }

    /// <summary>
    /// Records send latency for an instrument.
    /// </summary>
    public void RecordLatency(string instrumentId, double latencyMs)
    {
        LatencyHistogram.Record(latencyMs,
            new KeyValuePair<string, object?>("instrument.id", instrumentId));
    }

    /// <summary>
    /// Records an error for an instrument.
    /// </summary>
    public void RecordError(string instrumentId)
    {
        ErrorsCounter.Add(1,
            new KeyValuePair<string, object?>("instrument.id", instrumentId));
    }
}
