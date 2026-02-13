using System.Diagnostics.Metrics;

namespace SqncR.VcvRack;

/// <summary>
/// Custom metrics for the VCV Rack integration, exposed via System.Diagnostics.Metrics.
/// Consumed by OpenTelemetry when configured in the composition root.
/// </summary>
public static class VcvRackMetrics
{
    public static readonly Meter Meter = new("SqncR.VcvRack");

    /// <summary>Total number of patches generated.</summary>
    public static readonly Counter<long> PatchesGenerated =
        Meter.CreateCounter<long>("sqncr.vcvrack.patches_generated",
            description: "Total patches generated");

    /// <summary>Time to generate and serialize a patch in milliseconds.</summary>
    public static readonly Histogram<double> PatchGenerationTime =
        Meter.CreateHistogram<double>("sqncr.vcvrack.patch_generation_time_ms",
            unit: "ms",
            description: "Patch generation time in milliseconds");

    /// <summary>Time to launch VCV Rack in milliseconds.</summary>
    public static readonly Histogram<double> LaunchTime =
        Meter.CreateHistogram<double>("sqncr.vcvrack.launch_time_ms",
            unit: "ms",
            description: "VCV Rack launch time in milliseconds");

    /// <summary>1 if VCV Rack is currently running, 0 if not.</summary>
    public static ObservableGauge<int>? IsRunning { get; private set; }

    /// <summary>
    /// Registers the IsRunning observable gauge with a callback.
    /// Called once during composition root setup.
    /// </summary>
    public static void RegisterIsRunningGauge(Func<int> observeValue)
    {
        IsRunning = Meter.CreateObservableGauge("sqncr.vcvrack.is_running",
            observeValue,
            description: "1 if VCV Rack is running, 0 if not");
    }
}
