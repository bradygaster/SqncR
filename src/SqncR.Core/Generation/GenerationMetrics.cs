using System.Diagnostics.Metrics;

namespace SqncR.Core.Generation;

/// <summary>
/// Custom metrics for the generation engine, exposed via System.Diagnostics.Metrics.
/// Consumed by OpenTelemetry when configured in the composition root.
/// </summary>
public static class GenerationMetrics
{
    public static readonly Meter Meter = new("SqncR.Generation");

    /// <summary>Total number of note events emitted (drum + melody).</summary>
    public static readonly Counter<long> NotesEmitted =
        Meter.CreateCounter<long>("sqncr.generation.notes_per_second",
            description: "Total note events emitted");

    /// <summary>Number of currently active voices (incremented on NoteOn, decremented on NoteOff).</summary>
    public static readonly UpDownCounter<int> ActiveVoices =
        Meter.CreateUpDownCounter<int>("sqncr.generation.active_voices",
            description: "Number of currently sounding voices");

    /// <summary>Number of events per completed measure.</summary>
    public static readonly Histogram<int> PatternDensity =
        Meter.CreateHistogram<int>("sqncr.generation.pattern_density",
            description: "Events emitted per measure");

    /// <summary>Microseconds deviation between target tick time and actual tick time.</summary>
    public static readonly Histogram<double> TickLatency =
        Meter.CreateHistogram<double>("sqncr.generation.tick_latency_us",
            unit: "us",
            description: "Tick timing deviation in microseconds");
}
