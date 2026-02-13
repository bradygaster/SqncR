using System.Diagnostics.Metrics;
using SqncR.Core.Generation;

namespace SqncR.Core.Tests.Generation;

public class GenerationMetricsTests : IDisposable
{
    private readonly MeterListener _listener;
    private readonly List<(string Name, object Value)> _recorded = new();

    public GenerationMetricsTests()
    {
        _listener = new MeterListener();
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "SqncR.Generation")
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
        Assert.Equal("SqncR.Generation", GenerationMetrics.Meter.Name);
    }

    [Fact]
    public void NotesEmitted_HasCorrectInstrumentName()
    {
        Assert.Equal("sqncr.generation.notes_per_second", GenerationMetrics.NotesEmitted.Name);
    }

    [Fact]
    public void NotesEmitted_Counter_Increments()
    {
        _recorded.Clear();
        GenerationMetrics.NotesEmitted.Add(1);
        _listener.RecordObservableInstruments();

        var notes = _recorded.Where(r => r.Name == "sqncr.generation.notes_per_second").ToList();
        Assert.Single(notes);
        Assert.Equal(1L, notes[0].Value);
    }

    [Fact]
    public void TickLatency_Histogram_RecordsValues()
    {
        _recorded.Clear();
        GenerationMetrics.TickLatency.Record(42.5);
        _listener.RecordObservableInstruments();

        var latencies = _recorded.Where(r => r.Name == "sqncr.generation.tick_latency_us").ToList();
        Assert.NotEmpty(latencies);
        Assert.Contains(latencies, r => (double)r.Value == 42.5);
    }

    [Fact]
    public void PatternDensity_Histogram_RecordsValues()
    {
        _recorded.Clear();
        GenerationMetrics.PatternDensity.Record(16);
        _listener.RecordObservableInstruments();

        var density = _recorded.Where(r => r.Name == "sqncr.generation.pattern_density").ToList();
        Assert.Single(density);
        Assert.Equal(16, density[0].Value);
    }

    [Fact]
    public void ActiveVoices_HasCorrectInstrumentName()
    {
        Assert.Equal("sqncr.generation.active_voices", GenerationMetrics.ActiveVoices.Name);
    }

    [Fact]
    public void ActiveVoices_UpDownCounter_TracksChanges()
    {
        _recorded.Clear();
        GenerationMetrics.ActiveVoices.Add(1);
        GenerationMetrics.ActiveVoices.Add(-1);
        _listener.RecordObservableInstruments();

        var voices = _recorded.Where(r => r.Name == "sqncr.generation.active_voices").ToList();
        Assert.Equal(2, voices.Count);
        Assert.Equal(1, voices[0].Value);
        Assert.Equal(-1, voices[1].Value);
    }

    [Fact]
    public void TickLatency_HasMicrosecondUnit()
    {
        Assert.Equal("us", GenerationMetrics.TickLatency.Unit);
    }
}
