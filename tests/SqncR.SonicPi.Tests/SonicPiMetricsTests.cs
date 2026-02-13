using System.Diagnostics.Metrics;
using SqncR.SonicPi;

namespace SqncR.SonicPi.Tests;

public class SonicPiMetricsTests : IDisposable
{
    private readonly MeterListener _listener;
    private readonly List<(string Name, object Value)> _recorded = new();

    public SonicPiMetricsTests()
    {
        _listener = new MeterListener();
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "SqncR.SonicPi")
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
        Assert.Equal("SqncR.SonicPi", SonicPiMetrics.Meter.Name);
    }

    [Fact]
    public void OscMessagesSent_HasCorrectInstrumentName()
    {
        Assert.Equal("sqncr.sonicpi.osc_messages_sent", SonicPiMetrics.OscMessagesSent.Name);
    }

    [Fact]
    public void OscMessagesSent_Counter_Increments()
    {
        _recorded.Clear();
        SonicPiMetrics.OscMessagesSent.Add(1);
        _listener.RecordObservableInstruments();

        var messages = _recorded.Where(r => r.Name == "sqncr.sonicpi.osc_messages_sent").ToList();
        Assert.Single(messages);
        Assert.Equal(1L, messages[0].Value);
    }

    [Fact]
    public void OscLatency_HasMicrosecondUnit()
    {
        Assert.Equal("us", SonicPiMetrics.OscLatency.Unit);
    }

    [Fact]
    public void OscLatency_Histogram_RecordsValues()
    {
        _recorded.Clear();
        SonicPiMetrics.OscLatency.Record(123.4);
        _listener.RecordObservableInstruments();

        var latencies = _recorded.Where(r => r.Name == "sqncr.sonicpi.osc_latency_us").ToList();
        Assert.NotEmpty(latencies);
        Assert.Contains(latencies, r => (double)r.Value == 123.4);
    }

    [Fact]
    public void CodeGenerationTime_HasMicrosecondUnit()
    {
        Assert.Equal("us", SonicPiMetrics.CodeGenerationTime.Unit);
    }

    [Fact]
    public void ActiveInstruments_HasCorrectInstrumentName()
    {
        Assert.Equal("sqncr.sonicpi.active_instruments", SonicPiMetrics.ActiveInstruments.Name);
    }

    [Fact]
    public void ActiveInstruments_UpDownCounter_TracksChanges()
    {
        _recorded.Clear();
        SonicPiMetrics.ActiveInstruments.Add(1);
        SonicPiMetrics.ActiveInstruments.Add(-1);
        _listener.RecordObservableInstruments();

        var instruments = _recorded.Where(r => r.Name == "sqncr.sonicpi.active_instruments").ToList();
        Assert.Equal(2, instruments.Count);
        Assert.Equal(1, instruments[0].Value);
        Assert.Equal(-1, instruments[1].Value);
    }
}
