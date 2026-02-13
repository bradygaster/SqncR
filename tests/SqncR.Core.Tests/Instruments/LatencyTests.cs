using System.Diagnostics;
using System.Diagnostics.Metrics;
using SqncR.Core.Instruments;

namespace SqncR.Core.Tests.Instruments;

public class LatencyTests : IDisposable
{
    private readonly LatencyProfiler _profiler = new();
    private readonly MeterListener _meterListener;
    private readonly List<(string Name, object Value, KeyValuePair<string, object?>[] Tags)> _recorded = new();

    public LatencyTests()
    {
        _meterListener = new MeterListener();
        _meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "SqncR.Instruments")
                listener.EnableMeasurementEvents(instrument);
        };
        _meterListener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
            _recorded.Add((instrument.Name, value, tags.ToArray())));
        _meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            _recorded.Add((instrument.Name, value, tags.ToArray())));
        _meterListener.Start();
    }

    public void Dispose()
    {
        _meterListener.Dispose();
    }

    [Fact]
    public void MeasureSingleLatency_ReturnsReasonableValue()
    {
        var capabilities = new InstrumentCapabilities { EstimatedLatencyMs = 5 };
        bool received = false;

        var measurement = _profiler.MeasureLatency(
            "synth-1",
            capabilities,
            sendNote: () =>
            {
                // Simulate a short processing delay
                Thread.SpinWait(1000);
                received = true;
            },
            noteReceived: () => received);

        Assert.Equal("synth-1", measurement.InstrumentId);
        Assert.True(measurement.MeasuredLatencyMs >= 0, "Measured latency should be non-negative");
        Assert.Equal(5, measurement.EstimatedLatencyMs);
    }

    [Fact]
    public void MeasureAverageLatency_CalculatesCorrectAverage()
    {
        var capabilities = new InstrumentCapabilities { EstimatedLatencyMs = 2 };
        int callCount = 0;

        var measurement = _profiler.MeasureAverageLatency(
            "avg-synth",
            capabilities,
            samples: 10,
            sendNote: () =>
            {
                callCount++;
                Thread.SpinWait(500);
            },
            noteReceived: () => true);

        Assert.Equal(10, callCount);
        Assert.Equal("avg-synth", measurement.InstrumentId);
        Assert.True(measurement.MeasuredLatencyMs >= 0);
        Assert.Equal(2, measurement.EstimatedLatencyMs);
    }

    [Fact]
    public void IsWithinTolerance_Pass_WhenMeasuredNearEstimated()
    {
        var measurement = new LatencyMeasurement("synth-close", 7.0, 5.0);

        Assert.True(measurement.IsWithinTolerance(5.0));
        Assert.Equal(2.0, measurement.DeltaMs);
    }

    [Fact]
    public void IsWithinTolerance_Fail_WhenMeasuredFarFromEstimated()
    {
        var measurement = new LatencyMeasurement("synth-far", 25.0, 5.0);

        Assert.False(measurement.IsWithinTolerance(5.0));
        Assert.Equal(20.0, measurement.DeltaMs);
    }

    [Fact]
    public void GenerateReport_CalculatesStatistics()
    {
        var measurements = new List<LatencyMeasurement>
        {
            new("stat-synth", 10.0, 5.0),
            new("stat-synth", 20.0, 5.0),
            new("stat-synth", 30.0, 5.0),
            new("stat-synth", 40.0, 5.0),
        };

        var report = _profiler.GenerateReport(measurements);

        Assert.Equal(25.0, report.AverageLatencyMs);
        Assert.Equal(40.0, report.MaxLatencyMs);
        Assert.Equal(10.0, report.MinLatencyMs);
        // StdDev of [10, 20, 30, 40] = sqrt(((−15)²+(−5)²+(5)²+(15)²)/4) = sqrt(125) ≈ 11.18
        Assert.True(Math.Abs(report.StandardDeviationMs - 11.180339887) < 0.01);
        Assert.Equal(4, report.Measurements.Count);
    }

    [Fact]
    public void ZeroLatencyProfile_MeasurementWorks()
    {
        var capabilities = new InstrumentCapabilities { EstimatedLatencyMs = 0 };

        var measurement = _profiler.MeasureLatency(
            "soft-synth",
            capabilities,
            sendNote: () => { },
            noteReceived: () => true);

        Assert.Equal("soft-synth", measurement.InstrumentId);
        Assert.True(measurement.MeasuredLatencyMs >= 0);
        Assert.Equal(0, measurement.EstimatedLatencyMs);
        // With 0 estimated, even tiny measured should be valid
        Assert.True(measurement.IsWithinTolerance(5.0));
    }

    [Fact]
    public void ConsistentMeasurements_SmallStandardDeviation()
    {
        // All measurements very close to each other
        var measurements = new List<LatencyMeasurement>
        {
            new("consistent", 5.0, 5.0),
            new("consistent", 5.1, 5.0),
            new("consistent", 4.9, 5.0),
            new("consistent", 5.0, 5.0),
            new("consistent", 5.05, 5.0),
            new("consistent", 4.95, 5.0),
        };

        var report = _profiler.GenerateReport(measurements);

        Assert.True(report.StandardDeviationMs < 0.1, $"StdDev {report.StandardDeviationMs} should be < 0.1 for consistent measurements");
        Assert.Empty(report.Warnings);
    }

    [Fact]
    public void IntegrationWithInstrumentTelemetry_RecordsLatencyValues()
    {
        var telemetry = new InstrumentTelemetry();
        var capabilities = new InstrumentCapabilities { EstimatedLatencyMs = 3 };
        _recorded.Clear();

        // Simulate measuring latency and recording it via telemetry
        var measurement = _profiler.MeasureLatency(
            "telemetry-synth",
            capabilities,
            sendNote: () => Thread.SpinWait(500),
            noteReceived: () => true);

        telemetry.RecordLatency(measurement.InstrumentId, measurement.MeasuredLatencyMs);

        var latencies = _recorded.Where(r => r.Name == "instrument.latency_ms").ToList();
        Assert.Single(latencies);
        Assert.Equal(measurement.MeasuredLatencyMs, (double)latencies[0].Value);
        var tag = latencies[0].Tags.FirstOrDefault(t => t.Key == "instrument.id");
        Assert.Equal("telemetry-synth", tag.Value);
    }

    [Fact]
    public void GenerateReport_WarnsWhenMeasuredFarExceedsEstimated()
    {
        var measurements = new List<LatencyMeasurement>
        {
            new("warn-synth", 50.0, 5.0),  // measured >> estimated
            new("warn-synth", 3.0, 5.0),   // within range
        };

        var report = _profiler.GenerateReport(measurements);

        Assert.NotEmpty(report.Warnings);
        Assert.Contains(report.Warnings, w => w.Contains("warn-synth") && w.Contains(">>"));
    }

    [Fact]
    public void EmptyMeasurements_GeneratesEmptyReport()
    {
        var report = _profiler.GenerateReport(new List<LatencyMeasurement>());

        Assert.Equal(0, report.AverageLatencyMs);
        Assert.Equal(0, report.MaxLatencyMs);
        Assert.Equal(0, report.MinLatencyMs);
        Assert.Equal(0, report.StandardDeviationMs);
        Assert.Empty(report.Measurements);
        Assert.Empty(report.Warnings);
    }
}
