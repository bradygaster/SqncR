using System.Text.Json;

namespace SqncR.Core.Persistence;

/// <summary>
/// File-based persistence for named scene presets.
/// Stores JSON files at ~/.sqncr/scenes/.
/// Includes built-in presets that are always available.
/// </summary>
public sealed class SceneStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _scenesDirectory;

    /// <summary>
    /// Built-in presets that are always available.
    /// </summary>
    private static readonly Dictionary<string, Scene> BuiltInScenes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ambient-pad"] = new Scene
        {
            Name = "ambient-pad",
            Description = "Slow ambient pad with pentatonic minor scale",
            Tempo = 65,
            ScaleName = "Pentatonic Minor",
            RootNote = "C4",
            Octave = 4,
            GeneratorName = "Weighted",
            VarietyLevel = "Conservative",
            MelodicChannel = 1,
            DrumChannel = 10,
        },
        ["driving-techno"] = new Scene
        {
            Name = "driving-techno",
            Description = "Fast driving techno with arpeggio pattern",
            Tempo = 128,
            ScaleName = "Natural Minor",
            RootNote = "A3",
            Octave = 3,
            GeneratorName = "Arpeggio (Up)",
            VarietyLevel = "Adventurous",
            MelodicChannel = 1,
            DrumChannel = 10,
        },
        ["chill-lofi"] = new Scene
        {
            Name = "chill-lofi",
            Description = "Relaxed lo-fi with dorian mode and scale walk",
            Tempo = 85,
            ScaleName = "Dorian",
            RootNote = "D4",
            Octave = 4,
            GeneratorName = "Scale Walk",
            VarietyLevel = "Moderate",
            MelodicChannel = 1,
            DrumChannel = 10,
        },
    };

    public SceneStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".sqncr", "scenes"))
    {
    }

    public SceneStore(string scenesDirectory)
    {
        _scenesDirectory = scenesDirectory;
    }

    public async Task SaveAsync(Scene scene)
    {
        Directory.CreateDirectory(_scenesDirectory);
        var filePath = GetFilePath(scene.Name);
        var json = JsonSerializer.Serialize(scene, JsonOptions);
        await File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);
    }

    public async Task<Scene> LoadAsync(string name)
    {
        // Check user scenes first, then built-in
        var filePath = GetFilePath(name);
        if (File.Exists(filePath))
        {
            var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            return JsonSerializer.Deserialize<Scene>(json, JsonOptions)
                ?? throw new InvalidOperationException($"Failed to deserialize scene '{name}'.");
        }

        if (BuiltInScenes.TryGetValue(name, out var builtIn))
        {
            return builtIn;
        }

        throw new FileNotFoundException($"Scene '{name}' not found.", filePath);
    }

    public Task<IReadOnlyList<SceneSummary>> ListAsync()
    {
        var summaries = new List<SceneSummary>();

        // Add built-in scenes
        foreach (var scene in BuiltInScenes.Values)
        {
            summaries.Add(new SceneSummary(scene.Name, scene.Description, IsBuiltIn: true));
        }

        // Add user scenes (may shadow built-ins)
        if (Directory.Exists(_scenesDirectory))
        {
            foreach (var file in Directory.GetFiles(_scenesDirectory, "*.json"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                // Remove built-in if user has overridden it
                summaries.RemoveAll(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                summaries.Add(new SceneSummary(name, null, IsBuiltIn: false));
            }
        }

        summaries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult<IReadOnlyList<SceneSummary>>(summaries);
    }

    public Task DeleteAsync(string name)
    {
        var filePath = GetFilePath(name);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    private string GetFilePath(string name) => Path.Combine(_scenesDirectory, $"{name}.json");
}
