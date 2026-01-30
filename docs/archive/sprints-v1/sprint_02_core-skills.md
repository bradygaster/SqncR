# Sprint 02: Core Skills & Service Facade

**Duration:** 2 weeks  
**Goal:** Implement MVP skills, create service facade, enable first generation workflows

---

## Sprint Objectives

- ✅ Implement 6 core skills (list-devices, send-midi, device-selector, vibe-to-music, chord-progression, scale-selector)
- ✅ Create SqncRService facade
- ✅ Skill registry with dependency injection
- ✅ First end-to-end workflow: "generate ambient drone"
- ✅ All skills observable in Aspire Dashboard

---

## User Stories

### US-02-01: Skill framework
**As a developer, I need a skill system that's composable and observable**

**Acceptance Criteria:**
- [ ] ISkill interface defined
- [ ] SkillBase with OpenTelemetry built-in
- [ ] SkillRegistry for discovery
- [ ] Skills registered in DI
- [ ] Skills callable from service facade

### US-02-02: Basic generation workflow
**As a user, I want to generate a simple ambient drone**

**Acceptance Criteria:**
- [ ] User provides: description, key, tempo
- [ ] System selects: scale, device, channel
- [ ] Generates: sustained notes
- [ ] Observable in dashboard
- [ ] Music plays on hardware

### US-02-03: Device suggestion
**As a user, I want smart device suggestions for musical roles**

**Acceptance Criteria:**
- [ ] User says "I want bass"
- [ ] System suggests best device with reasoning
- [ ] Multiple devices ranked by suitability
- [ ] Device characteristics matched to requirement

---

## Tasks

### Task 1: Create Skill Framework

**File:** `src/SqncR.Core/Skills/ISkill.cs`

```csharp
namespace SqncR.Core.Skills;

public interface ISkill
{
    string Name { get; }
    string Description { get; }
    Task<SkillResult> ExecuteAsync(SkillInput input, CancellationToken cancellationToken);
}

public record SkillInput(Dictionary<string, object> Parameters);

public record SkillResult(bool Success, object? Data, string? Error);
```

**File:** `src/SqncR.Core/Skills/SkillBase.cs`

```csharp
public abstract class SkillBase : ISkill
{
    protected readonly ActivitySource ActivitySource;
    protected readonly ILogger Logger;
    
    public abstract string Name { get; }
    public abstract string Description { get; }
    
    public async Task<SkillResult> ExecuteAsync(SkillInput input, CancellationToken ct)
    {
        using var activity = ActivitySource.StartActivity($"Skill.{Name}");
        activity?.SetTag("skill.name", Name);
        activity?.SetTag("skill.input", JsonSerializer.Serialize(input.Parameters));
        
        try
        {
            var result = await ExecuteInternalAsync(input, ct);
            activity?.SetTag("skill.success", result.Success);
            return result;
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            Logger.LogError(ex, "Skill {SkillName} failed", Name);
            return new SkillResult(false, null, ex.Message);
        }
    }
    
    protected abstract Task<SkillResult> ExecuteInternalAsync(SkillInput input, CancellationToken ct);
}
```

**Checklist:**
- [ ] Create ISkill, SkillInput, SkillResult
- [ ] Create SkillBase with telemetry
- [ ] Add error handling
- [ ] Add structured logging
- [ ] Unit tests for SkillBase

**Estimated Time:** 3 hours

---

### Task 2: Implement skill-list-devices

**File:** `src/SqncR.Core/Skills/Device/ListDevicesSkill.cs`

```csharp
public class ListDevicesSkill : SkillBase
{
    private readonly IMidiService _midiService;
    
    public override string Name => "list-devices";
    public override string Description => "Lists all connected MIDI devices";
    
    protected override async Task<SkillResult> ExecuteInternalAsync(SkillInput input, CancellationToken ct)
    {
        var devices = await _midiService.ListDevicesAsync();
        return new SkillResult(true, devices, null);
    }
}
```

**Checklist:**
- [ ] Implement skill
- [ ] Inject IMidiService
- [ ] Return device list
- [ ] Unit test with mocked IMidiService
- [ ] Integration test with real devices

**Estimated Time:** 2 hours

---

### Task 3: Implement skill-vibe-to-music

**File:** `src/SqncR.Core/Skills/Musical/VibeToMusicSkill.cs`

```csharp
public class VibeToMusicSkill : SkillBase
{
    public override string Name => "vibe-to-music";
    
    protected override async Task<SkillResult> ExecuteInternalAsync(SkillInput input, CancellationToken ct)
    {
        var concept = input.Parameters["concept"].ToString();
        
        var parameters = concept.ToLower() switch
        {
            "darker" => new {
                mode = "phrygian",
                brightness = -0.3,
                velocity_offset = -15,
                voicing = "lower"
            },
            "brighter" => new {
                mode = "lydian",
                brightness = 0.5,
                velocity_offset = 15,
                voicing = "higher"
            },
            // ... more mappings
            _ => new { mode = "minor", brightness = 0.0, velocity_offset = 0, voicing = "mid" }
        };
        
        return new SkillResult(true, parameters, null);
    }
}
```

**Checklist:**
- [ ] Implement concept → parameters mapping
- [ ] Handle: darker, brighter, Rothko, film noir, etc.
- [ ] Return structured musical parameters
- [ ] Unit tests for all mappings
- [ ] Document reasoning for each mapping

**Estimated Time:** 4 hours

---

### Task 4: Implement skill-chord-progression

**File:** `src/SqncR.Core/Skills/Musical/ChordProgressionSkill.cs`

```csharp
public class ChordProgressionSkill : SkillBase
{
    private readonly IMusicTheoryService _theoryService;
    
    protected override async Task<SkillResult> ExecuteInternalAsync(SkillInput input, CancellationToken ct)
    {
        var key = input.Parameters["key"].ToString();
        var mode = input.Parameters["mode"].ToString();
        var bars = (int)input.Parameters["bars"];
        
        // Generate progression based on mode and vibe
        var progression = mode.ToLower() switch
        {
            "minor" => new[] { "i", "iv", "VI", "V" },
            "dorian" => new[] { "i", "ii", "IV", "v" },
            // ... more progressions
        };
        
        // Convert roman numerals to actual chords
        var chords = await _theoryService.BuildProgressionAsync(key, progression);
        
        return new SkillResult(true, chords, null);
    }
}
```

**Checklist:**
- [ ] Implement progression generation
- [ ] Support multiple modes
- [ ] Tension curve calculation
- [ ] Voice leading hints
- [ ] Unit tests for progressions
- [ ] Music theory correctness verified

**Estimated Time:** 6 hours

---

### Task 5: Implement skill-device-selector

**File:** `src/SqncR.Core/Skills/Device/DeviceSelectorSkill.cs`

```csharp
public class DeviceSelectorSkill : SkillBase
{
    protected override async Task<SkillResult> ExecuteInternalAsync(SkillInput input, CancellationToken ct)
    {
        var role = input.Parameters["role"].ToString();
        var characteristics = (string[])input.Parameters.GetValueOrDefault("characteristics", Array.Empty<string>());
        var availableDevices = (List<MidiDevice>)input.Parameters["available_devices"];
        
        // Score devices based on role and characteristics
        var scored = availableDevices.Select(device => new
        {
            Device = device,
            Score = CalculateScore(device, role, characteristics)
        }).OrderByDescending(x => x.Score);
        
        var best = scored.First();
        
        return new SkillResult(true, new
        {
            selected_device = best.Device,
            channel = best.Device.Channels.FirstOrDefault(),
            reasoning = $"{best.Device.Manufacturer} {best.Device.DeviceName} scored {best.Score:F2}"
        }, null);
    }
    
    private double CalculateScore(MidiDevice device, string role, string[] characteristics)
    {
        // Match device characteristics to requirements
        // Polyend + versatile = high score for many roles
        // Moog + analog + warm = high score for bass
        // MESS + glitchy = high score for fx
    }
}
```

**Checklist:**
- [ ] Implement device scoring algorithm
- [ ] Match characteristics (warm, analog, glitchy, etc.)
- [ ] Match roles (bass, pads, lead, fx)
- [ ] Return reasoning for selection
- [ ] Unit tests for scoring logic

**Estimated Time:** 4 hours

---

### Task 6: Create SqncRService Facade

**File:** `src/SqncR.Core/SqncRService.cs`

```csharp
namespace SqncR.Core;

public class SqncRService
{
    private readonly ISkillRegistry _skillRegistry;
    private readonly ISessionManagerAgent _sessionManager;
    private readonly ILogger<SqncRService> _logger;
    
    public async Task<IReadOnlyList<MidiDevice>> ListDevicesAsync()
    {
        var skill = _skillRegistry.GetSkill("list-devices");
        var result = await skill.ExecuteAsync(new SkillInput(new()), CancellationToken.None);
        return (IReadOnlyList<MidiDevice>)result.Data!;
    }
    
    public async Task<GenerationResult> GenerateAsync(GenerationRequest request)
    {
        // Orchestrate multiple skills:
        // 1. Parse request
        // 2. Select devices
        // 3. Generate musical content
        // 4. Start playback
    }
    
    public async Task ModifyAsync(string instruction)
    {
        // Use vibe-to-music skill
        // Update active generation
    }
}
```

**Checklist:**
- [ ] Create SqncRService class
- [ ] Implement ListDevicesAsync
- [ ] Implement GenerateAsync (basic)
- [ ] Implement ModifyAsync (basic)
- [ ] Wire up skill registry
- [ ] Add to DI container
- [ ] Integration test

**Estimated Time:** 4 hours

---

### Task 7: Implement Skill Registry

**File:** `src/SqncR.Core/Skills/SkillRegistry.cs`

```csharp
public class SkillRegistry : ISkillRegistry
{
    private readonly Dictionary<string, ISkill> _skills;
    
    public SkillRegistry(IEnumerable<ISkill> skills)
    {
        _skills = skills.ToDictionary(s => s.Name, s => s);
    }
    
    public ISkill GetSkill(string name)
    {
        if (!_skills.TryGetValue(name, out var skill))
        {
            throw new SkillNotFoundException(name);
        }
        return skill;
    }
    
    public IEnumerable<ISkill> GetAllSkills() => _skills.Values;
}
```

**Checklist:**
- [ ] Implement SkillRegistry
- [ ] Skill discovery from DI
- [ ] Error handling for missing skills
- [ ] Unit tests

**Estimated Time:** 2 hours

---

### Task 8: Configure Dependency Injection

**File:** `src/SqncR.Core/ServiceCollectionExtensions.cs`

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqncRCore(this IServiceCollection services)
    {
        // Register skills
        services.AddSingleton<ISkill, ListDevicesSkill>();
        services.AddSingleton<ISkill, SendMidiSkill>();
        services.AddSingleton<ISkill, DeviceSelectorSkill>();
        services.AddSingleton<ISkill, VibeToMusicSkill>();
        services.AddSingleton<ISkill, ChordProgressionSkill>();
        services.AddSingleton<ISkill, ScaleSelectorSkill>();
        
        // Register registry
        services.AddSingleton<ISkillRegistry, SkillRegistry>();
        
        // Register service facade
        services.AddSingleton<SqncRService>();
        
        // Register agents (basic for now)
        services.AddSingleton<ISessionManagerAgent, SessionManagerAgent>();
        
        return services;
    }
}
```

**Checklist:**
- [ ] Create extension method
- [ ] Register all skills
- [ ] Register skill registry
- [ ] Register SqncRService
- [ ] Test DI resolution

**Estimated Time:** 2 hours

---

### Task 9: First End-to-End Test

**Test:** Generate simple ambient drone

```csharp
[Fact]
public async Task GenerateAmbientDrone_ShouldPlayOnPolyendSynth()
{
    // Arrange
    var service = CreateSqncRService();  // with all dependencies
    
    var request = new GenerationRequest
    {
        Description = "ambient drone in A minor, 60 BPM",
        Devices = new[] { "Polyend Synth" },
        Channels = new[] { 1 }
    };
    
    // Act
    var result = await service.GenerateAsync(request);
    
    // Assert
    result.Success.Should().BeTrue();
    result.ActiveDevices.Should().Contain(d => d.Name.Contains("Polyend"));
    
    // Verify trace in telemetry
    // Verify MIDI messages sent
    
    // Listen for sound on hardware!
}
```

**Checklist:**
- [ ] Create integration test
- [ ] Wire up all dependencies
- [ ] Test with real hardware
- [ ] Verify in Aspire Dashboard
- [ ] Document test requirements

**Estimated Time:** 3 hours

---

## Definition of Done

- ✅ 6 core skills implemented and tested
- ✅ SkillRegistry working with DI
- ✅ SqncRService facade operational
- ✅ "Generate ambient drone" works end-to-end
- ✅ All skills traced in Aspire Dashboard
- ✅ Unit tests pass (>85% coverage)
- ✅ Integration test with hardware successful
- ✅ Documentation updated (SKILLS.md)

---

## Deliverables

1. **Skill Framework** - ISkill, SkillBase, SkillRegistry
2. **6 Core Skills** - Implemented and tested
3. **SqncRService** - Facade for all operations
4. **DI Configuration** - All wired up correctly
5. **E2E Test** - Ambient drone generation works
6. **Observability** - All skills visible in dashboard

---

### Task 10: Implement Sequence Format Parser

**File:** `src/SqncR.Core/Formats/SequenceFormat.cs`

```csharp
namespace SqncR.Core.Formats;

/// <summary>
/// Parser and serializer for .sqnc.yaml sequence format.
/// See examples/README.md for format specification.
/// </summary>
public class SequenceFormat
{
    public static Sequence Parse(string yaml);
    public static Sequence Load(string filePath);
    public static string Serialize(Sequence sequence);
    public static void Save(Sequence sequence, string filePath);
}

public record Sequence
{
    public SequenceMeta Meta { get; init; }
    public List<string> Intent { get; init; }
    public Dictionary<string, DeviceMapping> Devices { get; init; }
    public Dictionary<string, Pattern> Patterns { get; init; }
    public Dictionary<string, Automation> Automation { get; init; }
    public Dictionary<string, Groove> Grooves { get; init; }
    public Dictionary<string, Section> Sections { get; init; }
    public List<ArrangeItem> Arrange { get; init; }
}

public record SequenceMeta
{
    public string Title { get; init; }
    public string? Artist { get; init; }
    public int Tempo { get; init; }
    public string Key { get; init; }
    public TimeSignature Time { get; init; }
    public string? Swing { get; init; }
    public int Tpq { get; init; } = 480;
    public DateTime Created { get; init; }
}

public record TimeSignature(int Beats, int Division);
```

**Checklist:**
- [ ] Define Sequence model classes
- [ ] Implement YAML parser using YamlDotNet
- [ ] Implement serializer
- [ ] Handle randomization types (range, choice, prob)
- [ ] Validate format on load
- [ ] Unit tests for parsing example files
- [ ] Unit tests for round-trip (parse → serialize → parse)

**Estimated Time:** 6 hours

---

### Task 11: Implement Sequence Playback Engine

**File:** `src/SqncR.Core/Playback/SequencePlayer.cs`

```csharp
public class SequencePlayer
{
    private readonly IMidiService _midiService;
    
    public async Task PlayAsync(Sequence sequence, CancellationToken ct)
    {
        // Resolve patterns to events
        // Apply randomization (range, choice, prob)
        // Apply grooves
        // Interpolate automation curves
        // Schedule MIDI events
    }
    
    public void Stop();
    public void Pause();
    public void Resume();
    public TimeSpan Position { get; }
}
```

**Checklist:**
- [ ] Implement pattern resolution
- [ ] Implement randomization resolution
- [ ] Implement groove application
- [ ] Implement automation curve interpolation
- [ ] Implement scheduling with high-resolution timer
- [ ] Unit tests for each resolution step
- [ ] Integration test with real MIDI output

**Estimated Time:** 8 hours

---

## Demo Script

1. Run Aspire: `dotnet run` in AppHost
2. Open Dashboard
3. Call SqncRService.GenerateAsync("ambient drone in Am")
4. Show traces in dashboard:
   - skill-list-devices called
   - skill-device-selector chose Polyend
   - skill-chord-progression generated Am progression
   - MIDI notes sent to Polyend
5. **Hear ambient drone from Polyend Synth**
6. Save session: `sqncr session save "my-drone"`
7. Show saved file: `~/.sqncr/sessions/my-drone.sqnc.yaml`
8. Run tests: `dotnet test` - all green

---

**Sprint Status:** 🔲 Not Started  
**Updated:** January 29, 2026
