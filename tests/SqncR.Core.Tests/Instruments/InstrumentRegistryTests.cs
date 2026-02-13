using SqncR.Core.Instruments;

namespace SqncR.Core.Tests.Instruments;

public class InstrumentRegistryTests
{
    private readonly InstrumentRegistry _registry = new();

    private static Instrument CreateInstrument(
        string id = "synth-1",
        string name = "Juno-106",
        InstrumentType type = InstrumentType.Hardware,
        InstrumentRole role = InstrumentRole.Lead,
        int midiChannel = 1) =>
        new()
        {
            Id = id,
            Name = name,
            Type = type,
            Role = role,
            MidiChannel = midiChannel
        };

    [Fact]
    public void Add_And_Get_Returns_Instrument()
    {
        var inst = CreateInstrument();
        _registry.Add(inst);

        var result = _registry.Get("synth-1");

        Assert.NotNull(result);
        Assert.Equal("Juno-106", result.Name);
        Assert.Equal(InstrumentType.Hardware, result.Type);
    }

    [Fact]
    public void Get_Returns_Null_For_Missing_Id()
    {
        var result = _registry.Get("does-not-exist");
        Assert.Null(result);
    }

    [Fact]
    public void Remove_Removes_Instrument()
    {
        _registry.Add(CreateInstrument());

        bool removed = _registry.Remove("synth-1");

        Assert.True(removed);
        Assert.Null(_registry.Get("synth-1"));
    }

    [Fact]
    public void Remove_Returns_False_For_Missing_Id()
    {
        bool removed = _registry.Remove("nope");
        Assert.False(removed);
    }

    [Fact]
    public void GetByRole_Returns_Matching_Instruments()
    {
        _registry.Add(CreateInstrument("bass-1", "Bass Station", role: InstrumentRole.Bass));
        _registry.Add(CreateInstrument("lead-1", "Prophet-5", role: InstrumentRole.Lead));
        _registry.Add(CreateInstrument("bass-2", "Moog Sub 37", role: InstrumentRole.Bass));

        var bassInstruments = _registry.GetByRole(InstrumentRole.Bass);

        Assert.Equal(2, bassInstruments.Count);
        Assert.All(bassInstruments, i => Assert.Equal(InstrumentRole.Bass, i.Role));
    }

    [Fact]
    public void GetByRole_Returns_Empty_When_No_Match()
    {
        _registry.Add(CreateInstrument(role: InstrumentRole.Lead));

        var drums = _registry.GetByRole(InstrumentRole.Drums);

        Assert.Empty(drums);
    }

    [Fact]
    public void GetAll_Returns_All_Registered_Instruments()
    {
        _registry.Add(CreateInstrument("a", "A"));
        _registry.Add(CreateInstrument("b", "B"));
        _registry.Add(CreateInstrument("c", "C"));

        var all = _registry.GetAll();

        Assert.Equal(3, all.Count);
    }

    [Fact]
    public void Duplicate_Id_Throws()
    {
        _registry.Add(CreateInstrument("dup", "First"));

        Assert.Throws<ArgumentException>(() =>
            _registry.Add(CreateInstrument("dup", "Second")));
    }

    [Fact]
    public void Add_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _registry.Add(null!));
    }

    [Fact]
    public async Task Concurrent_AddRemove_Is_ThreadSafe()
    {
        const int count = 100;
        var tasks = new List<Task>();

        // Add instruments concurrently
        for (int i = 0; i < count; i++)
        {
            int idx = i;
            tasks.Add(Task.Run(() =>
                _registry.Add(CreateInstrument($"inst-{idx}", $"Instrument {idx}"))));
        }

        await Task.WhenAll(tasks);

        Assert.Equal(count, _registry.GetAll().Count);

        // Remove half concurrently
        tasks.Clear();
        for (int i = 0; i < count / 2; i++)
        {
            int idx = i;
            tasks.Add(Task.Run(() => _registry.Remove($"inst-{idx}")));
        }

        await Task.WhenAll(tasks);

        Assert.Equal(count / 2, _registry.GetAll().Count);
    }

    [Fact]
    public void Capabilities_Defaults_Are_Correct()
    {
        var inst = CreateInstrument();
        _registry.Add(inst);

        var result = _registry.Get("synth-1")!;

        Assert.Equal(0, result.Capabilities.MinNote);
        Assert.Equal(127, result.Capabilities.MaxNote);
        Assert.Equal(8, result.Capabilities.MaxPolyphony);
        Assert.Equal(0, result.Capabilities.EstimatedLatencyMs);
        Assert.Null(result.Capabilities.Timbre);
    }

    [Fact]
    public void Custom_Capabilities_Are_Preserved()
    {
        var inst = new Instrument
        {
            Id = "bass-hw",
            Name = "Moog Sub 37",
            Type = InstrumentType.Hardware,
            Role = InstrumentRole.Bass,
            MidiChannel = 3,
            Capabilities = new InstrumentCapabilities
            {
                MinNote = 24,
                MaxNote = 72,
                MaxPolyphony = 1,
                EstimatedLatencyMs = 2,
                Timbre = "warm analog bass"
            }
        };

        _registry.Add(inst);
        var result = _registry.Get("bass-hw")!;

        Assert.Equal(24, result.Capabilities.MinNote);
        Assert.Equal(72, result.Capabilities.MaxNote);
        Assert.Equal(1, result.Capabilities.MaxPolyphony);
        Assert.Equal(2, result.Capabilities.EstimatedLatencyMs);
        Assert.Equal("warm analog bass", result.Capabilities.Timbre);
    }

    [Fact]
    public void InstrumentType_Enum_Has_All_Expected_Values()
    {
        Assert.Equal(3, Enum.GetValues<InstrumentType>().Length);
        Assert.Contains(InstrumentType.Hardware, Enum.GetValues<InstrumentType>());
        Assert.Contains(InstrumentType.SonicPi, Enum.GetValues<InstrumentType>());
        Assert.Contains(InstrumentType.VcvRack, Enum.GetValues<InstrumentType>());
    }

    [Fact]
    public void InstrumentRole_Enum_Has_All_Expected_Values()
    {
        Assert.Equal(5, Enum.GetValues<InstrumentRole>().Length);
        Assert.Contains(InstrumentRole.Bass, Enum.GetValues<InstrumentRole>());
        Assert.Contains(InstrumentRole.Pad, Enum.GetValues<InstrumentRole>());
        Assert.Contains(InstrumentRole.Lead, Enum.GetValues<InstrumentRole>());
        Assert.Contains(InstrumentRole.Drums, Enum.GetValues<InstrumentRole>());
        Assert.Contains(InstrumentRole.Melody, Enum.GetValues<InstrumentRole>());
    }
}
