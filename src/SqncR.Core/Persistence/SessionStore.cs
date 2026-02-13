using System.Text.Json;

namespace SqncR.Core.Persistence;

/// <summary>
/// File-based persistence for generation sessions.
/// Stores JSON files at ~/.sqncr/sessions/.
/// </summary>
public sealed class SessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _sessionsDirectory;

    public SessionStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".sqncr", "sessions"))
    {
    }

    public SessionStore(string sessionsDirectory)
    {
        _sessionsDirectory = sessionsDirectory;
    }

    public async Task SaveAsync(SessionState session)
    {
        Directory.CreateDirectory(_sessionsDirectory);
        var filePath = GetFilePath(session.Name);
        var json = JsonSerializer.Serialize(session, JsonOptions);
        await File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);
    }

    public async Task<SessionState> LoadAsync(string name)
    {
        var filePath = GetFilePath(name);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Session '{name}' not found.", filePath);
        }

        var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        return JsonSerializer.Deserialize<SessionState>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize session '{name}'.");
    }

    public Task<IReadOnlyList<string>> ListAsync()
    {
        if (!Directory.Exists(_sessionsDirectory))
        {
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        var names = Directory.GetFiles(_sessionsDirectory, "*.json")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .OrderBy(n => n)
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(names);
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

    private string GetFilePath(string name) => Path.Combine(_sessionsDirectory, $"{name}.json");
}
