using System.Diagnostics.Metrics;
using SqncR.VcvRack;

namespace SqncR.VcvRack.Tests;

public class VcvRackMetricsTests : IDisposable
{
    private readonly MeterListener _listener;
    private readonly List<(string Name, object Value)> _recorded = new();

    public VcvRackMetricsTests()
    {
        _listener = new MeterListener();
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "SqncR.VcvRack")
                listener.EnableMeasurementEvents(instrument);
        };

        _listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
            _recorded.Add((instrument.Name, value)));

        _listener.SetMeasurementEventCallback<int>((instrument, value, _, _) =>
            _recorded.Add((instrument.Name, value)));

        _listener.SetMeasurementEventCallback<double>((instrument, value, _, _) =>
            _recorded.Add((instrument.Name, value)));

        _listener.Start();
    }

    public void Dispose() => _listener.Dispose();

    [Fact]
    public void Meter_HasCorrectName()
    {
        Assert.Equal("SqncR.VcvRack", VcvRackMetrics.Meter.Name);
    }

    [Fact]
    public void PatchesGenerated_HasCorrectInstrumentName()
    {
        Assert.Equal("sqncr.vcvrack.patches_generated", VcvRackMetrics.PatchesGenerated.Name);
    }

    [Fact]
    public void PatchesGenerated_Counter_Increments()
    {
        _recorded.Clear();
        VcvRackMetrics.PatchesGenerated.Add(1);
        _listener.RecordObservableInstruments();

        var patches = _recorded.Where(r => r.Name == "sqncr.vcvrack.patches_generated").ToList();
        Assert.Single(patches);
        Assert.Equal(1L, patches[0].Value);
    }

    [Fact]
    public void PatchGenerationTime_HasMillisecondUnit()
    {
        Assert.Equal("ms", VcvRackMetrics.PatchGenerationTime.Unit);
    }

    [Fact]
    public void PatchGenerationTime_Histogram_RecordsValues()
    {
        _recorded.Clear();
        VcvRackMetrics.PatchGenerationTime.Record(55.3);
        _listener.RecordObservableInstruments();

        var times = _recorded.Where(r => r.Name == "sqncr.vcvrack.patch_generation_time_ms").ToList();
        Assert.NotEmpty(times);
        Assert.Contains(times, r => (double)r.Value == 55.3);
    }

    [Fact]
    public void LaunchTime_HasMillisecondUnit()
    {
        Assert.Equal("ms", VcvRackMetrics.LaunchTime.Unit);
    }

    [Fact]
    public void LaunchTime_Histogram_RecordsValues()
    {
        _recorded.Clear();
        VcvRackMetrics.LaunchTime.Record(1500.0);
        _listener.RecordObservableInstruments();

        var times = _recorded.Where(r => r.Name == "sqncr.vcvrack.launch_time_ms").ToList();
        Assert.NotEmpty(times);
        Assert.Contains(times, r => (double)r.Value == 1500.0);
    }

    [Fact]
    public void RegisterIsRunningGauge_CreatesGauge()
    {
        VcvRackMetrics.RegisterIsRunningGauge(() => 1);
        Assert.NotNull(VcvRackMetrics.IsRunning);
    }
}
