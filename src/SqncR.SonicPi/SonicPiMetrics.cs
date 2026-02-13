using System.Diagnostics.Metrics;

namespace SqncR.SonicPi;

/// <summary>
/// Custom metrics for the Sonic Pi integration, exposed via System.Diagnostics.Metrics.
/// Consumed by OpenTelemetry when configured in the composition root.
/// </summary>
public static class SonicPiMetrics
{
    public static readonly Meter Meter = new("SqncR.SonicPi");

    /// <summary>Total number of OSC messages sent to Sonic Pi.</summary>
    public static readonly Counter<long> OscMessagesSent =
        Meter.CreateCounter<long>("sqncr.sonicpi.osc_messages_sent",
            description: "Total OSC messages sent");

    /// <summary>Round-trip timing for OSC sends in microseconds.</summary>
    public static readonly Histogram<double> OscLatency =
        Meter.CreateHistogram<double>("sqncr.sonicpi.osc_latency_us",
            unit: "us",
            description: "OSC send latency in microseconds");

    /// <summary>Time to generate Ruby code in microseconds.</summary>
    public static readonly Histogram<double> CodeGenerationTime =
        Meter.CreateHistogram<double>("sqncr.sonicpi.code_generation_time_us",
            unit: "us",
            description: "Ruby code generation time in microseconds");

    /// <summary>Number of currently active Sonic Pi instruments.</summary>
    public static readonly UpDownCounter<int> ActiveInstruments =
        Meter.CreateUpDownCounter<int>("sqncr.sonicpi.active_instruments",
            description: "Currently active Sonic Pi instruments");
}
