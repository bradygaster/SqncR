using SqncR.Core.Instruments;

namespace SqncR.Core.Tests.Instruments;

public class DeviceProfileTests : IDisposable
{
    private readonly string _tempDir;
    private readonly DeviceProfileStore _store;

    public DeviceProfileTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "sqncr-device-tests-" + Guid.NewGuid().ToString("N"));
        _store = new DeviceProfileStore(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrips_Correctly()
    {
        var profile = new DeviceProfile
        {
            Id = "test-synth",
            Name = "Test Synth",
            Description = "A test synthesizer",
            Type = InstrumentType.Hardware,
            DefaultRole = InstrumentRole.Lead,
            MidiChannel = 3,
            Capabilities = new InstrumentCapabilities
            {
                MinNote = 36,
                MaxNote = 84,
                MaxPolyphony = 4,
                EstimatedLatencyMs = 5,
                Timbre = "bright lead",
            },
            VelocityCurve = "exponential",
        };

        await _store.SaveAsync(profile);
        var loaded = await _store.LoadAsync("test-synth");

        Assert.Equal("test-synth", loaded.Id);
        Assert.Equal("Test Synth", loaded.Name);
        Assert.Equal("A test synthesizer", loaded.Description);
        Assert.Equal(InstrumentType.Hardware, loaded.Type);
        Assert.Equal(InstrumentRole.Lead, loaded.DefaultRole);
        Assert.Equal(3, loaded.MidiChannel);
        Assert.Equal(36, loaded.Capabilities.MinNote);
        Assert.Equal(84, loaded.Capabilities.MaxNote);
        Assert.Equal(4, loaded.Capabilities.MaxPolyphony);
        Assert.Equal(5, loaded.Capabilities.EstimatedLatencyMs);
        Assert.Equal("bright lead", loaded.Capabilities.Timbre);
        Assert.Equal("exponential", loaded.VelocityCurve);
    }

    [Fact]
    public async Task ListAsync_IncludesSavedAndBuiltInProfiles()
    {
        var userProfile = new DeviceProfile
        {
            Id = "my-synth",
            Name = "My Synth",
            Type = InstrumentType.Hardware,
            DefaultRole = InstrumentRole.Lead,
        };
        await _store.SaveAsync(userProfile);

        var list = await _store.ListAsync();

        Assert.True(list.Count >= 4); // 3 built-in + 1 user
        Assert.Contains(list, s => s.Id == "moog-sub37" && s.IsBuiltIn);
        Assert.Contains(list, s => s.Id == "roland-juno" && s.IsBuiltIn);
        Assert.Contains(list, s => s.Id == "sonic-pi-default" && s.IsBuiltIn);
        Assert.Contains(list, s => s.Id == "my-synth" && !s.IsBuiltIn);
    }

    [Fact]
    public async Task DeleteAsync_RemovesUserProfile()
    {
        var profile = new DeviceProfile
        {
            Id = "delete-me",
            Name = "Delete Me",
            Type = InstrumentType.Hardware,
            DefaultRole = InstrumentRole.Bass,
        };
        await _store.SaveAsync(profile);

        var listBefore = await _store.ListAsync();
        Assert.Contains(listBefore, s => s.Id == "delete-me");

        await _store.DeleteAsync("delete-me");

        var listAfter = await _store.ListAsync();
        Assert.DoesNotContain(listAfter, s => s.Id == "delete-me" && !s.IsBuiltIn);
    }

    [Fact]
    public async Task LoadAsync_MissingProfile_ThrowsFileNotFoundException()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _store.LoadAsync("nonexistent-profile"));
    }

    [Fact]
    public async Task CcMappings_PersistCorrectly()
    {
        var profile = new DeviceProfile
        {
            Id = "cc-test",
            Name = "CC Test Synth",
            Type = InstrumentType.Hardware,
            DefaultRole = InstrumentRole.Lead,
            CcMappings = new Dictionary<string, int>
            {
                ["Filter Cutoff"] = 74,
                ["Resonance"] = 71,
                ["Attack"] = 73,
            },
        };

        await _store.SaveAsync(profile);
        var loaded = await _store.LoadAsync("cc-test");

        Assert.NotNull(loaded.CcMappings);
        Assert.Equal(3, loaded.CcMappings.Count);
        Assert.Equal(74, loaded.CcMappings["Filter Cutoff"]);
        Assert.Equal(71, loaded.CcMappings["Resonance"]);
        Assert.Equal(73, loaded.CcMappings["Attack"]);
    }

    [Fact]
    public async Task BuiltInProfiles_HaveValidValues()
    {
        var moog = await _store.LoadAsync("moog-sub37");
        Assert.Equal("Moog Sub 37", moog.Name);
        Assert.Equal(InstrumentType.Hardware, moog.Type);
        Assert.Equal(InstrumentRole.Bass, moog.DefaultRole);
        Assert.Equal(1, moog.MidiChannel);
        Assert.Equal(1, moog.Capabilities.MaxPolyphony);
        Assert.Equal(24, moog.Capabilities.MinNote);
        Assert.Equal(72, moog.Capabilities.MaxNote);

        var juno = await _store.LoadAsync("roland-juno");
        Assert.Equal("Roland Juno-106", juno.Name);
        Assert.Equal(InstrumentType.Hardware, juno.Type);
        Assert.Equal(InstrumentRole.Pad, juno.DefaultRole);
        Assert.Equal(2, juno.MidiChannel);
        Assert.Equal(6, juno.Capabilities.MaxPolyphony);
        Assert.Equal(36, juno.Capabilities.MinNote);
        Assert.Equal(96, juno.Capabilities.MaxNote);

        var sonicPi = await _store.LoadAsync("sonic-pi-default");
        Assert.Equal("Sonic Pi Default", sonicPi.Name);
        Assert.Equal(InstrumentType.SonicPi, sonicPi.Type);
        Assert.Equal(InstrumentRole.Melody, sonicPi.DefaultRole);
        Assert.Equal(1, sonicPi.MidiChannel);
        Assert.Equal(8, sonicPi.Capabilities.MaxPolyphony);
        Assert.Equal(24, sonicPi.Capabilities.MinNote);
        Assert.Equal(108, sonicPi.Capabilities.MaxNote);
    }

    [Fact]
    public async Task DeleteAsync_MissingProfile_DoesNotThrow()
    {
        await _store.DeleteAsync("nonexistent"); // should not throw
    }

    [Fact]
    public async Task SaveAsync_Overwrites_ExistingProfile()
    {
        var profile1 = new DeviceProfile
        {
            Id = "overwrite-me",
            Name = "Version 1",
            Type = InstrumentType.Hardware,
            DefaultRole = InstrumentRole.Bass,
            MidiChannel = 1,
        };
        var profile2 = new DeviceProfile
        {
            Id = "overwrite-me",
            Name = "Version 2",
            Type = InstrumentType.Hardware,
            DefaultRole = InstrumentRole.Lead,
            MidiChannel = 5,
        };

        await _store.SaveAsync(profile1);
        await _store.SaveAsync(profile2);

        var loaded = await _store.LoadAsync("overwrite-me");
        Assert.Equal("Version 2", loaded.Name);
        Assert.Equal(InstrumentRole.Lead, loaded.DefaultRole);
        Assert.Equal(5, loaded.MidiChannel);
    }

    [Fact]
    public async Task UserProfile_ShadowsBuiltIn_InList()
    {
        var userProfile = new DeviceProfile
        {
            Id = "moog-sub37",
            Name = "My Custom Moog",
            Type = InstrumentType.Hardware,
            DefaultRole = InstrumentRole.Lead,
        };
        await _store.SaveAsync(userProfile);

        var list = await _store.ListAsync();

        var moogEntries = list.Where(s => s.Id == "moog-sub37").ToList();
        Assert.Single(moogEntries);
        Assert.False(moogEntries[0].IsBuiltIn);
    }

    [Fact]
    public async Task ListAsync_EmptyDirectory_ReturnsOnlyBuiltIns()
    {
        var list = await _store.ListAsync();

        Assert.Equal(3, list.Count);
        Assert.All(list, s => Assert.True(s.IsBuiltIn));
    }
}
