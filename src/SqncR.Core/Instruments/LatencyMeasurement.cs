using System.Diagnostics;

namespace SqncR.Core.Instruments;

/// <summary>
/// A single latency measurement comparing measured vs. estimated latency.
/// </summary>
public class LatencyMeasurement
{
    public LatencyMeasurement(string instrumentId, double measuredLatencyMs, double estimatedLatencyMs)
    {
        InstrumentId = instrumentId;
        MeasuredLatencyMs = measuredLatencyMs;
        EstimatedLatencyMs = estimatedLatencyMs;
    }

    public string InstrumentId { get; }
    public double MeasuredLatencyMs { get; }
    public double EstimatedLatencyMs { get; }
    public double DeltaMs => MeasuredLatencyMs - EstimatedLatencyMs;
    public bool IsWithinTolerance(double toleranceMs = 5.0) => Math.Abs(DeltaMs) <= toleranceMs;
}

/// <summary>
/// Measures send-to-callback latency for instruments and compares against profile estimates.
/// </summary>
public class LatencyProfiler
{
    /// <summary>
    /// Measures send-to-callback latency for an instrument.
    /// </summary>
    public LatencyMeasurement MeasureLatency(
        string instrumentId,
        InstrumentCapabilities capabilities,
        Action sendNote,
        Func<bool> noteReceived)
    {
        var sw = Stopwatch.StartNew();
        sendNote();
        while (!noteReceived())
        {
            // Spin-wait for callback
        }
        sw.Stop();

        return new LatencyMeasurement(instrumentId, sw.Elapsed.TotalMilliseconds, capabilities.EstimatedLatencyMs);
    }

    /// <summary>
    /// Measures average latency over N samples.
    /// </summary>
    public LatencyMeasurement MeasureAverageLatency(
        string instrumentId,
        InstrumentCapabilities capabilities,
        int samples,
        Action sendNote,
        Func<bool> noteReceived)
    {
        double total = 0;
        for (int i = 0; i < samples; i++)
        {
            var measurement = MeasureLatency(instrumentId, capabilities, sendNote, noteReceived);
            total += measurement.MeasuredLatencyMs;
        }

        return new LatencyMeasurement(instrumentId, total / samples, capabilities.EstimatedLatencyMs);
    }

    /// <summary>
    /// Generates a statistical report from a collection of measurements.
    /// </summary>
    public LatencyReport GenerateReport(IReadOnlyList<LatencyMeasurement> measurements)
    {
        if (measurements.Count == 0)
            return new LatencyReport(0, 0, 0, 0, measurements, []);

        double sum = 0;
        double max = double.MinValue;
        double min = double.MaxValue;

        foreach (var m in measurements)
        {
            sum += m.MeasuredLatencyMs;
            if (m.MeasuredLatencyMs > max) max = m.MeasuredLatencyMs;
            if (m.MeasuredLatencyMs < min) min = m.MeasuredLatencyMs;
        }

        double average = sum / measurements.Count;

        double varianceSum = 0;
        foreach (var m in measurements)
        {
            double diff = m.MeasuredLatencyMs - average;
            varianceSum += diff * diff;
        }
        double stdDev = Math.Sqrt(varianceSum / measurements.Count);

        var warnings = new List<string>();
        foreach (var m in measurements)
        {
            if (m.DeltaMs > m.EstimatedLatencyMs * 0.5 + 5.0)
                warnings.Add($"Instrument '{m.InstrumentId}': measured {m.MeasuredLatencyMs:F2}ms >> estimated {m.EstimatedLatencyMs:F2}ms");
        }

        return new LatencyReport(average, max, min, stdDev, measurements, warnings);
    }
}

/// <summary>
/// Statistical report of latency measurements.
/// </summary>
public record LatencyReport(
    double AverageLatencyMs,
    double MaxLatencyMs,
    double MinLatencyMs,
    double StandardDeviationMs,
    IReadOnlyList<LatencyMeasurement> Measurements,
    IReadOnlyList<string> Warnings);
