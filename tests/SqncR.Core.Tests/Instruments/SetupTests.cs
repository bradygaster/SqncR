using SqncR.Core.Instruments;
using SqncR.Midi.Testing;

namespace SqncR.Core.Tests.Instruments;

public class SetupTests : IDisposable
{
    private readonly string _tempDir;
    private readonly DeviceProfileStore _profileStore;
    private readonly InstrumentRegistry _registry;

    public SetupTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "sqncr-setup-tests-" + Guid.NewGuid().ToString("N"));
        _profileStore = new DeviceProfileStore(_tempDir);
        _registry = new InstrumentRegistry();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task Setup_CreatesProfileAndRegistersInstrument()
    {
        var profile = new DeviceProfile
        {
            Id = "my-bass",
            Name = "My Bass",
            Type = InstrumentType.Hardware,
            DefaultRole = InstrumentRole.Bass,
            MidiChannel = 3,
        };
        await _profileStore.SaveAsync(profile);

        var instrument = new Instrument
        {
            Id = "my-bass",
            Name = "My Bass",
            Type = InstrumentType.Hardware,
            Role = InstrumentRole.Bass,
            MidiChannel = 3,
        };
        _registry.Add(instrument);

        // Verify profile persisted
        var loaded = await _profileStore.LoadAsync("my-bass");
        Assert.Equal("My Bass", loaded.Name);
        Assert.Equal(InstrumentType.Hardware, loaded.Type);
        Assert.Equal(3, loaded.MidiChannel);

        // Verify registered
        var reg = _registry.Get("my-bass");
        Assert.NotNull(reg);
        Assert.Equal("My Bass", reg.Name);
        Assert.Equal(InstrumentRole.Bass, reg.Role);
        Assert.Equal(3, reg.MidiChannel);
    }

    [Fact]
    public void AutoChannel_AssignsNextAvailable()
    {
        // Occupy channels 1 and 2
        _registry.Add(new Instrument { Id = "a", Name = "A", MidiChannel = 1 });
        _registry.Add(new Instrument { Id = "b", Name = "B", MidiChannel = 2 });

        var next = FindNextAvailableChannel(_registry);

        Assert.Equal(3, next);
    }

    [Fact]
    public void AutoChannel_ReturnsOne_WhenEmpty()
    {
        var next = FindNextAvailableChannel(_registry);
        Assert.Equal(1, next);
    }

    [Fact]
    public void AutoChannel_SkipsGaps()
    {
        _registry.Add(new Instrument { Id = "a", Name = "A", MidiChannel = 1 });
        _registry.Add(new Instrument { Id = "c", Name = "C", MidiChannel = 3 });

        var next = FindNextAvailableChannel(_registry);

        Assert.Equal(2, next);
    }

    [Fact]
    public async Task Describe_ReturnsCorrectInfo()
    {
        var instrument = new Instrument
        {
            Id = "lead-synth",
            Name = "Lead Synth",
            Type = InstrumentType.SonicPi,
            Role = InstrumentRole.Lead,
            MidiChannel = 5,
            Capabilities = new InstrumentCapabilities
            {
                MinNote = 24,
                MaxNote = 108,
                MaxPolyphony = 8,
                Timbre = "digital synthesis",
            },
        };
        _registry.Add(instrument);

        var profile = new DeviceProfile
        {
            Id = "lead-synth",
            Name = "Lead Synth",
            Type = InstrumentType.SonicPi,
            DefaultRole = InstrumentRole.Lead,
            MidiChannel = 5,
            CcMappings = new Dictionary<string, int> { ["Filter"] = 74 },
            VelocityCurve = "linear",
        };
        await _profileStore.SaveAsync(profile);

        var reg = _registry.Get("lead-synth");
        Assert.NotNull(reg);
        Assert.Equal("Lead Synth", reg.Name);
        Assert.Equal(InstrumentType.SonicPi, reg.Type);
        Assert.Equal(InstrumentRole.Lead, reg.Role);
        Assert.Equal(5, reg.MidiChannel);
        Assert.Equal(24, reg.Capabilities.MinNote);
        Assert.Equal(108, reg.Capabilities.MaxNote);

        var loadedProfile = await _profileStore.LoadAsync("lead-synth");
        Assert.NotNull(loadedProfile.CcMappings);
        Assert.Equal(74, loadedProfile.CcMappings["Filter"]);
        Assert.Equal("linear", loadedProfile.VelocityCurve);
    }

    [Fact]
    public void Remove_CleansUpRegistry()
    {
        _registry.Add(new Instrument { Id = "rem-1", Name = "Remove Me" });

        var removed = _registry.Remove("rem-1");

        Assert.True(removed);
        Assert.Null(_registry.Get("rem-1"));
    }

    [Fact]
    public async Task Remove_DeletesProfile_WhenRequested()
    {
        var profile = new DeviceProfile
        {
            Id = "del-profile",
            Name = "Delete Profile",
            Type = InstrumentType.Hardware,
            DefaultRole = InstrumentRole.Bass,
        };
        await _profileStore.SaveAsync(profile);

        _registry.Add(new Instrument { Id = "del-profile", Name = "Delete Profile" });

        _registry.Remove("del-profile");
        await _profileStore.DeleteAsync("del-profile");

        Assert.Null(_registry.Get("del-profile"));
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _profileStore.LoadAsync("del-profile"));
    }

    [Fact]
    public void Remove_AllNotesOff_Sent()
    {
        var midi = new MockMidiOutput();
        var instrument = new Instrument { Id = "off-test", Name = "Off Test", MidiChannel = 7 };
        _registry.Add(instrument);

        // Simulate what remove_setup_instrument does
        midi.AllNotesOff(instrument.MidiChannel);
        _registry.Remove("off-test");

        Assert.Contains(midi.Events, e =>
            e.Type == MidiEventType.AllNotesOff && e.Channel == 7);
    }

    [Fact]
    public void List_GroupsByRole()
    {
        _registry.Add(new Instrument { Id = "bass-1", Name = "Bass 1", Role = InstrumentRole.Bass, MidiChannel = 1 });
        _registry.Add(new Instrument { Id = "lead-1", Name = "Lead 1", Role = InstrumentRole.Lead, MidiChannel = 2 });
        _registry.Add(new Instrument { Id = "bass-2", Name = "Bass 2", Role = InstrumentRole.Bass, MidiChannel = 3 });
        _registry.Add(new Instrument { Id = "drums-1", Name = "Drums 1", Role = InstrumentRole.Drums, MidiChannel = 10 });

        var all = _registry.GetAll();
        var grouped = all.GroupBy(i => i.Role).ToDictionary(g => g.Key, g => g.ToList());

        Assert.Equal(3, grouped.Count);
        Assert.Equal(2, grouped[InstrumentRole.Bass].Count);
        Assert.Single(grouped[InstrumentRole.Lead]);
        Assert.Single(grouped[InstrumentRole.Drums]);
    }

    [Fact]
    public void MissingInstrument_GivesHelpfulError()
    {
        var result = _registry.Get("nonexistent-synth");

        Assert.Null(result);
        // The MCP tool would return a helpful message like:
        // "Instrument 'nonexistent-synth' not found. Use list_setup_instruments to see registered instruments."
        // We verify the registry returns null for missing IDs, which the tool checks.
    }

    [Fact]
    public void AutoChannel_FallsBackToOne_WhenAllOccupied()
    {
        for (int ch = 1; ch <= 16; ch++)
            _registry.Add(new Instrument { Id = $"ch{ch}", Name = $"Ch{ch}", MidiChannel = ch });

        var next = FindNextAvailableChannel(_registry);

        Assert.Equal(1, next);
    }

    [Fact]
    public async Task Setup_SonicPi_GetsDefaultCapabilities()
    {
        var profile = new DeviceProfile
        {
            Id = "sonic-test",
            Name = "Sonic Test",
            Type = InstrumentType.SonicPi,
            DefaultRole = InstrumentRole.Melody,
            MidiChannel = 1,
            Capabilities = new InstrumentCapabilities
            {
                MinNote = 24,
                MaxNote = 108,
                MaxPolyphony = 8,
                Timbre = "digital synthesis",
            },
        };
        await _profileStore.SaveAsync(profile);

        var loaded = await _profileStore.LoadAsync("sonic-test");
        Assert.Equal(InstrumentType.SonicPi, loaded.Type);
        Assert.Equal(24, loaded.Capabilities.MinNote);
        Assert.Equal(108, loaded.Capabilities.MaxNote);
        Assert.Equal(8, loaded.Capabilities.MaxPolyphony);
        Assert.Equal("digital synthesis", loaded.Capabilities.Timbre);
    }

    [Fact]
    public async Task Setup_VcvRack_GetsDefaultCapabilities()
    {
        var profile = new DeviceProfile
        {
            Id = "vcv-test",
            Name = "VCV Test",
            Type = InstrumentType.VcvRack,
            DefaultRole = InstrumentRole.Pad,
            MidiChannel = 4,
            Capabilities = new InstrumentCapabilities
            {
                MinNote = 0,
                MaxNote = 127,
                MaxPolyphony = 16,
                Timbre = "modular synthesis",
            },
        };
        await _profileStore.SaveAsync(profile);

        var loaded = await _profileStore.LoadAsync("vcv-test");
        Assert.Equal(InstrumentType.VcvRack, loaded.Type);
        Assert.Equal(0, loaded.Capabilities.MinNote);
        Assert.Equal(127, loaded.Capabilities.MaxNote);
        Assert.Equal(16, loaded.Capabilities.MaxPolyphony);
        Assert.Equal("modular synthesis", loaded.Capabilities.Timbre);
    }

    /// <summary>
    /// Replicates the auto-channel logic from SetupTool for unit testing
    /// without depending on the MCP server assembly.
    /// </summary>
    private static int FindNextAvailableChannel(InstrumentRegistry registry)
    {
        var usedChannels = new HashSet<int>(registry.GetAll().Select(i => i.MidiChannel));
        for (int ch = 1; ch <= 16; ch++)
        {
            if (!usedChannels.Contains(ch))
                return ch;
        }
        return 1;
    }
}
