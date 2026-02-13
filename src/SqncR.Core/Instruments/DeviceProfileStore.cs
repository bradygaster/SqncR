using System.Text.Json;

namespace SqncR.Core.Instruments;

/// <summary>
/// File-based persistence for device profiles.
/// Stores JSON files at ~/.sqncr/devices/.
/// Includes built-in profiles that are always available.
/// </summary>
public sealed class DeviceProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _devicesDirectory;

    /// <summary>
    /// Built-in device profiles that are always available.
    /// </summary>
    private static readonly Dictionary<string, DeviceProfile> BuiltInProfiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["moog-sub37"] = new DeviceProfile
        {
            Id = "moog-sub37",
            Name = "Moog Sub 37",
            Description = "Moog Sub 37 paraphonic analog synthesizer",
            Type = InstrumentType.Hardware,
            DefaultRole = InstrumentRole.Bass,
            MidiChannel = 1,
            Capabilities = new InstrumentCapabilities
            {
                MinNote = 24,
                MaxNote = 72,
                MaxPolyphony = 1,
                Timbre = "warm analog bass",
            },
            CcMappings = new Dictionary<string, int>
            {
                ["Filter Cutoff"] = 74,
                ["Filter Resonance"] = 71,
                ["Oscillator Mix"] = 16,
            },
            VelocityCurve = "exponential",
        },
        ["roland-juno"] = new DeviceProfile
        {
            Id = "roland-juno",
            Name = "Roland Juno-106",
            Description = "Roland Juno-106 polyphonic analog synthesizer",
            Type = InstrumentType.Hardware,
            DefaultRole = InstrumentRole.Pad,
            MidiChannel = 2,
            Capabilities = new InstrumentCapabilities
            {
                MinNote = 36,
                MaxNote = 96,
                MaxPolyphony = 6,
                Timbre = "lush analog pad",
            },
            CcMappings = new Dictionary<string, int>
            {
                ["Chorus Rate"] = 93,
                ["VCF Cutoff"] = 74,
            },
            VelocityCurve = "linear",
        },
        ["sonic-pi-default"] = new DeviceProfile
        {
            Id = "sonic-pi-default",
            Name = "Sonic Pi Default",
            Description = "Default Sonic Pi software synthesizer",
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
            VelocityCurve = "linear",
        },
    };

    public DeviceProfileStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".sqncr", "devices"))
    {
    }

    public DeviceProfileStore(string devicesDirectory)
    {
        _devicesDirectory = devicesDirectory;
    }

    public async Task SaveAsync(DeviceProfile profile)
    {
        Directory.CreateDirectory(_devicesDirectory);
        var filePath = GetFilePath(profile.Id);
        var json = JsonSerializer.Serialize(profile, JsonOptions);
        await File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);
    }

    public async Task<DeviceProfile> LoadAsync(string id)
    {
        // Check user profiles first, then built-in
        var filePath = GetFilePath(id);
        if (File.Exists(filePath))
        {
            var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            return JsonSerializer.Deserialize<DeviceProfile>(json, JsonOptions)
                ?? throw new InvalidOperationException($"Failed to deserialize device profile '{id}'.");
        }

        if (BuiltInProfiles.TryGetValue(id, out var builtIn))
        {
            return builtIn;
        }

        throw new FileNotFoundException($"Device profile '{id}' not found.", filePath);
    }

    public Task<IReadOnlyList<DeviceProfileSummary>> ListAsync()
    {
        var summaries = new List<DeviceProfileSummary>();

        // Add built-in profiles
        foreach (var profile in BuiltInProfiles.Values)
        {
            summaries.Add(new DeviceProfileSummary(profile.Id, profile.Name, profile.Type, IsBuiltIn: true));
        }

        // Add user profiles (may shadow built-ins)
        if (Directory.Exists(_devicesDirectory))
        {
            foreach (var file in Directory.GetFiles(_devicesDirectory, "*.json"))
            {
                var id = Path.GetFileNameWithoutExtension(file);
                // Remove built-in if user has overridden it
                summaries.RemoveAll(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
                summaries.Add(new DeviceProfileSummary(id, id, InstrumentType.Hardware, IsBuiltIn: false));
            }
        }

        summaries.Sort((a, b) => string.Compare(a.Id, b.Id, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult<IReadOnlyList<DeviceProfileSummary>>(summaries);
    }

    public Task DeleteAsync(string id)
    {
        var filePath = GetFilePath(id);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    private string GetFilePath(string id) => Path.Combine(_devicesDirectory, $"{id}.json");
}
