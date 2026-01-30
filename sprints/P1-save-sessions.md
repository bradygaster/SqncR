# P1: Save and Load Sessions

**Priority:** 1
**Depends on:** P0 (Play Songs)
**Goal:** Save what's playing, load it later, basic persistence
**Duration:** ~1 week

---

## The Wow Moment

You're playing something cool. You say "save this" and it's persisted. Come back tomorrow and pick up where you left off.

```bash
sqncr save "late-night-jam"
sqncr list-sessions
sqncr load "late-night-jam"
```

---

## What We're Building

1. **Session state** - Track what's playing, on which device, what channel
2. **Save to .sqnc.yaml** - Persist current state to file
3. **Load sessions** - Resume from saved state
4. **Session listing** - See all saved sessions
5. **SQLite storage** - Session metadata and history

## What We're NOT Building Yet

- Full music theory library (still just playing files)
- Generation (we're saving, not creating)
- MCP/API/SDK
- Full observability

---

## Tasks

### Task 1: Add State Project

```bash
cd src
dotnet new classlib -n SqncR.State -f net9.0
cd ..
dotnet sln add src/SqncR.State

cd src/SqncR.State
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design

cd ../SqncR.Core
dotnet add reference ../SqncR.State
```

---

### Task 2: Session Model

**File:** `src/SqncR.State/Models/Session.cs`

```csharp
namespace SqncR.State.Models;

public class Session
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string FilePath { get; set; } = "";  // .sqnc.yaml file
    public string? DeviceName { get; set; }
    public int? DeviceIndex { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastPlayedAt { get; set; }
    public string? Notes { get; set; }
}
```

---

### Task 3: Database Context

**File:** `src/SqncR.State/SqncRDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using SqncR.State.Models;

namespace SqncR.State;

public class SqncRDbContext : DbContext
{
    public DbSet<Session> Sessions => Set<Session>();

    private readonly string _dbPath;

    public SqncRDbContext()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var sqncrDir = Path.Combine(appData, "sqncr");
        Directory.CreateDirectory(sqncrDir);
        _dbPath = Path.Combine(sqncrDir, "sqncr.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={_dbPath}");
}
```

---

### Task 4: Session Service

**File:** `src/SqncR.Core/SessionService.cs`

```csharp
using SqncR.State;
using SqncR.State.Models;

namespace SqncR.Core;

public class SessionService
{
    private readonly SqncRDbContext _db;
    private readonly string _sessionsDir;

    public SessionService()
    {
        _db = new SqncRDbContext();
        _db.Database.EnsureCreated();

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _sessionsDir = Path.Combine(appData, "sqncr", "sessions");
        Directory.CreateDirectory(_sessionsDir);
    }

    public Session Save(string name, Sequence sequence, string? deviceName = null, int? deviceIndex = null)
    {
        var fileName = SanitizeFileName(name) + ".sqnc.yaml";
        var filePath = Path.Combine(_sessionsDir, fileName);

        // Serialize sequence to YAML
        var serializer = new YamlDotNet.Serialization.SerializerBuilder().Build();
        var yaml = serializer.Serialize(sequence);
        File.WriteAllText(filePath, yaml);

        // Save to database
        var session = new Session
        {
            Name = name,
            FilePath = filePath,
            DeviceName = deviceName,
            DeviceIndex = deviceIndex,
            CreatedAt = DateTime.UtcNow
        };

        _db.Sessions.Add(session);
        _db.SaveChanges();

        return session;
    }

    public Session? Load(string name)
    {
        var session = _db.Sessions.FirstOrDefault(s =>
            s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (session != null)
        {
            session.LastPlayedAt = DateTime.UtcNow;
            _db.SaveChanges();
        }

        return session;
    }

    public IReadOnlyList<Session> ListSessions()
    {
        return _db.Sessions.OrderByDescending(s => s.LastPlayedAt ?? s.CreatedAt).ToList();
    }

    public void Delete(string name)
    {
        var session = _db.Sessions.FirstOrDefault(s =>
            s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (session != null)
        {
            if (File.Exists(session.FilePath))
                File.Delete(session.FilePath);

            _db.Sessions.Remove(session);
            _db.SaveChanges();
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c))
            .Replace(' ', '-')
            .ToLowerInvariant();
    }
}
```

---

### Task 5: CLI Save Command

```csharp
var saveCommand = new Command("save", "Save current playback as a session");
var nameArg = new Argument<string>("name", "Name for the session");
saveCommand.AddArgument(nameArg);

saveCommand.SetHandler((name) =>
{
    // For P1, we save the last-played sequence
    // In future, this will save active generation state
    var sessionService = new SessionService();

    if (_lastPlayedSequence == null)
    {
        Console.WriteLine("Nothing to save. Play a sequence first.");
        return;
    }

    var session = sessionService.Save(name, _lastPlayedSequence,
        _lastDeviceName, _lastDeviceIndex);

    Console.WriteLine($"Saved session: {session.Name}");
    Console.WriteLine($"  File: {session.FilePath}");

}, nameArg);
rootCommand.AddCommand(saveCommand);
```

---

### Task 6: CLI Load Command

```csharp
var loadCommand = new Command("load", "Load and play a saved session");
var loadNameArg = new Argument<string>("name", "Session name to load");
loadCommand.AddArgument(loadNameArg);

loadCommand.SetHandler(async (name) =>
{
    var sessionService = new SessionService();
    var session = sessionService.Load(name);

    if (session == null)
    {
        Console.WriteLine($"Session not found: {name}");
        return;
    }

    if (!File.Exists(session.FilePath))
    {
        Console.WriteLine($"Session file missing: {session.FilePath}");
        return;
    }

    Console.WriteLine($"Loading session: {session.Name}");

    using var midi = new MidiService();

    if (session.DeviceIndex.HasValue)
        midi.OpenDevice(session.DeviceIndex.Value);
    else if (!string.IsNullOrEmpty(session.DeviceName))
        midi.OpenDevice(session.DeviceName);
    else
    {
        Console.WriteLine("No device configured. Use --device to specify.");
        return;
    }

    var parser = new SequenceParser();
    var sequence = parser.Parse(session.FilePath);
    var player = new SequencePlayer(midi);

    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

    try
    {
        await player.PlayAsync(sequence, cts.Token);
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Stopped.");
    }

}, loadNameArg);
rootCommand.AddCommand(loadCommand);
```

---

### Task 7: CLI List Sessions Command

```csharp
var sessionsCommand = new Command("sessions", "List saved sessions");

sessionsCommand.SetHandler(() =>
{
    var sessionService = new SessionService();
    var sessions = sessionService.ListSessions();

    if (sessions.Count == 0)
    {
        Console.WriteLine("No saved sessions.");
        return;
    }

    Console.WriteLine("Saved sessions:");
    foreach (var s in sessions)
    {
        var lastPlayed = s.LastPlayedAt?.ToString("g") ?? "never";
        Console.WriteLine($"  {s.Name}");
        Console.WriteLine($"    Created: {s.CreatedAt:g}");
        Console.WriteLine($"    Last played: {lastPlayed}");
        Console.WriteLine($"    Device: {s.DeviceName ?? "not set"}");
    }
});
rootCommand.AddCommand(sessionsCommand);
```

---

### Task 8: CLI Delete Session Command

```csharp
var deleteSessionCommand = new Command("delete-session", "Delete a saved session");
var deleteNameArg = new Argument<string>("name", "Session name to delete");
deleteSessionCommand.AddArgument(deleteNameArg);

deleteSessionCommand.SetHandler((name) =>
{
    var sessionService = new SessionService();
    sessionService.Delete(name);
    Console.WriteLine($"Deleted session: {name}");
}, deleteNameArg);
rootCommand.AddCommand(deleteSessionCommand);
```

---

## Definition of Done

- [ ] `sqncr sessions` lists saved sessions
- [ ] `sqncr save "my-jam"` saves current sequence
- [ ] `sqncr load "my-jam"` loads and plays saved sequence
- [ ] `sqncr delete-session "my-jam"` removes a session
- [ ] Sessions persist across CLI restarts (SQLite)
- [ ] Device preferences saved with session

---

## Demo

```bash
# Play something
sqncr play examples/chill-ambient.sqnc.yaml -d 0

# Save it
sqncr save "late-night-ambient"
# Output: Saved session: late-night-ambient
#         File: ~/.sqncr/sessions/late-night-ambient.sqnc.yaml

# List sessions
sqncr sessions
# Output: Saved sessions:
#           late-night-ambient
#             Created: 1/29/2026 10:30 PM
#             Last played: never
#             Device: Polyend Synth MIDI 1

# Load and play
sqncr load "late-night-ambient"
# Output: Loading session: late-night-ambient
#         Playing: Late Night Ambient
#         ...
```

---

## What's Next

**P2: Music Theory Foundation** - Note, Scale, Chord types, basic generation

---

**Priority:** P1
**Status:** Waiting for P0
**Updated:** January 29, 2026
