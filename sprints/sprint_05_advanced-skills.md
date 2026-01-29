# Sprint 05: Advanced Skills & Agents

**Duration:** 3 weeks  
**Goal:** Implement advanced skills, build autonomous agents, enable complex workflows

---

## Sprint Objectives

- ✅ Implement 15+ advanced skills (analysis, generation, transformation)
- ✅ Build all 4 agents (SessionManager, Composition, Listener, DeviceOrchestrator)
- ✅ Enable complex multi-device workflows
- ✅ Real-time MIDI input analysis
- ✅ Autonomous composition with agent coordination

---

## User Stories

### US-05-01: Polyrhythm generation
**As a user, I want to create polyrhythmic patterns**

**Acceptance Criteria:**
- [ ] User says "add polyrhythms, 3 against 4"
- [ ] System generates mathematically correct polyrhythms
- [ ] Multiple instruments play in different time divisions
- [ ] Sync points calculated correctly

### US-05-02: Song analysis
**As a user, I want to analyze songs for musical data**

**Acceptance Criteria:**
- [ ] User says "analyze that Cream song from The Breakfast Club"
- [ ] System searches for song
- [ ] Extracts key, tempo, chord progression
- [ ] Returns musical characteristics

### US-05-03: Autonomous composition
**As a user, I want AI to compose evolving music autonomously**

**Acceptance Criteria:**
- [ ] User says "build something over 3 minutes"
- [ ] CompositionAgent creates structural arc
- [ ] DeviceOrchestratorAgent coordinates devices
- [ ] Music evolves without user intervention
- [ ] Agent decisions traced in dashboard

### US-05-04: Live jamming
**As a user, I want AI to listen and respond to my playing**

**Acceptance Criteria:**
- [ ] User says "listen and complement what I play"
- [ ] ListenerAgent monitors MIDI input
- [ ] Detects key, chords, tempo
- [ ] CompositionAgent generates complementary parts
- [ ] Real-time response < 100ms

---

## Tasks

### Task 1: Implement Analysis Skills

**Skills to implement:**
- [ ] skill-analyze-song (with web search)
- [ ] skill-detect-key
- [ ] skill-detect-tempo
- [ ] skill-analyze-harmony

**File:** `src/SqncR.Core/Skills/Analysis/AnalyzeSongSkill.cs`

```csharp
public class AnalyzeSongSkill : SkillBase
{
    private readonly IWebSearchService _webSearch;
    
    protected override async Task<SkillResult> ExecuteInternalAsync(SkillInput input, CancellationToken ct)
    {
        var description = input.Parameters["song_description"].ToString();
        
        // Search for song
        var searchResults = await _webSearch.SearchAsync($"{description} key tempo chords");
        
        // Parse musical data
        var analysis = new SongAnalysis
        {
            Title = ExtractTitle(searchResults),
            Artist = ExtractArtist(searchResults),
            Key = ExtractKey(searchResults),
            Tempo = ExtractTempo(searchResults),
            // ... etc
        };
        
        return new SkillResult(true, analysis, null);
    }
}
```

**Estimated Time:** 8 hours (all 4 skills)

---

### Task 2: Implement Generation Skills

**Skills to implement:**
- [ ] skill-polyrhythm-generator
- [ ] skill-arpeggio-generator
- [ ] skill-bass-line-generator
- [ ] skill-melody-generator
- [ ] skill-rhythm-generator

**File:** `src/SqncR.Core/Skills/Generation/PolyrhythmGeneratorSkill.cs`

```csharp
public class PolyrhythmGeneratorSkill : SkillBase
{
    protected override async Task<SkillResult> ExecuteInternalAsync(SkillInput input, CancellationToken ct)
    {
        var baseTempo = (int)input.Parameters["base_tempo"];
        var complexity = (int)input.Parameters["complexity"];  // e.g., 3 for "3 against 4"
        
        // Calculate polyrhythm timing
        var quarterNoteDuration = 60000.0 / baseTempo;  // ms
        var pattern1 = Enumerable.Range(0, 4).Select(i => i * quarterNoteDuration).ToArray();
        var pattern2 = Enumerable.Range(0, complexity).Select(i => i * (quarterNoteDuration * 4 / complexity)).ToArray();
        
        return new SkillResult(true, new { pattern1, pattern2 }, null);
    }
}
```

**Estimated Time:** 10 hours (all 5 skills)

---

### Task 3: Implement Transformation Skills

**Skills to implement:**
- [ ] skill-transpose
- [ ] skill-quantize
- [ ] skill-humanize
- [ ] skill-modal-interchange

**Estimated Time:** 6 hours

---

### Task 4: Build Agent Framework

**File:** `src/SqncR.Core/Agents/IAgent.cs`

```csharp
namespace SqncR.Core.Agents;

public interface IAgent
{
    string Name { get; }
    AgentState State { get; }
    Task StartAsync(CancellationToken ct);
    Task StopAsync();
    Task PauseAsync();
    Task ResumeAsync();
}

public enum AgentState
{
    Idle,
    Running,
    Paused,
    Stopped
}
```

**File:** `src/SqncR.Core/Agents/AgentBase.cs`

```csharp
public abstract class AgentBase : IAgent
{
    protected readonly ActivitySource ActivitySource;
    protected AgentState _state = AgentState.Idle;
    
    public AgentState State => _state;
    
    protected void TransitionTo(AgentState newState)
    {
        using var activity = ActivitySource.StartActivity("AgentStateTransition");
        activity?.SetTag("agent.name", Name);
        activity?.SetTag("agent.state_from", _state.ToString());
        activity?.SetTag("agent.state_to", newState.ToString());
        
        _state = newState;
    }
}
```

**Checklist:**
- [ ] Create IAgent interface
- [ ] Create AgentBase with state management
- [ ] Add OpenTelemetry for state transitions
- [ ] Lifecycle methods (start, stop, pause, resume)

**Estimated Time:** 3 hours

---

### Task 5: Implement SessionManagerAgent

**File:** `src/SqncR.Core/Agents/SessionManagerAgent.cs`

```csharp
public class SessionManagerAgent : AgentBase
{
    private GenerationContext? _currentContext;
    private Dictionary<string, object> _userPreferences = new();
    
    public async Task<SessionInfo> StartSessionAsync()
    {
        TransitionTo(AgentState.Running);
        
        // Scan devices
        var devices = await _midiService.ListDevicesAsync();
        
        // Load user preferences
        // Initialize context
        
        _currentContext = new GenerationContext
        {
            SessionId = Guid.NewGuid(),
            AvailableDevices = devices,
            StartTime = DateTimeOffset.UtcNow
        };
        
        return new SessionInfo { SessionId = _currentContext.SessionId };
    }
    
    public void RememberPreference(string key, object value)
    {
        _userPreferences[key] = value;
    }
    
    public T? GetPreference<T>(string key)
    {
        return _userPreferences.TryGetValue(key, out var value) ? (T)value : default;
    }
}
```

**Checklist:**
- [ ] Session lifecycle management
- [ ] Device tracking
- [ ] User preference storage
- [ ] Context maintenance
- [ ] State traced in dashboard

**Estimated Time:** 4 hours

---

### Task 6: Implement CompositionAgent

**File:** `src/SqncR.Core/Agents/CompositionAgent.cs`

```csharp
public class CompositionAgent : AgentBase
{
    private CompositionState _compositionState = CompositionState.Intro;
    
    public async Task ComposeAsync(CompositionRequest request, CancellationToken ct)
    {
        // Autonomous composition loop
        var arc = PlanStructuralArc(request.Duration);
        
        foreach (var section in arc)
        {
            TransitionTo(section.State);
            await GenerateSectionAsync(section, ct);
        }
    }
    
    private StructuralArc PlanStructuralArc(TimeSpan duration)
    {
        // intro → build → peak → resolve
        // Calculate timing for each section
    }
}

enum CompositionState
{
    Intro, Building, Peak, Resolving, Outro
}
```

**Checklist:**
- [ ] Structural arc planning
- [ ] Section transitions
- [ ] Intensity curves
- [ ] State machine implementation
- [ ] Traced in dashboard

**Estimated Time:** 6 hours

---

### Task 7: Implement ListenerAgent

**File:** `src/SqncR.Core/Agents/ListenerAgent.cs`

```csharp
public class ListenerAgent : AgentBase
{
    private readonly IMidiService _midiService;
    
    public async Task StartListeningAsync(int inputDeviceIndex, CancellationToken ct)
    {
        TransitionTo(AgentState.Running);
        
        await _midiService.SubscribeToInputAsync(inputDeviceIndex, async midiEvent =>
        {
            if (midiEvent is NoteOnEvent noteOn)
            {
                // Analyze in real-time
                var key = await _theoryService.DetectKeyFromNotesAsync(_recentNotes);
                var chord = await _theoryService.AnalyzeChordAsync(_currentlyHeldNotes);
                
                // Emit events for CompositionAgent to react
                await OnKeyDetectedAsync(key);
                await OnChordDetectedAsync(chord);
            }
        });
    }
}
```

**Checklist:**
- [ ] MIDI input subscription
- [ ] Real-time note tracking
- [ ] Key detection from playing
- [ ] Chord detection from held notes
- [ ] Event emission for other agents
- [ ] Latency < 100ms

**Estimated Time:** 6 hours

---

### Task 8: Implement DeviceOrchestratorAgent

**File:** `src/SqncR.Core/Agents/DeviceOrchestratorAgent.cs`

```csharp
public class DeviceOrchestratorAgent : AgentBase
{
    public async Task OrchestratAsync(OrchestrationPlan plan, CancellationToken ct)
    {
        // Coordinate multiple devices
        // Assign roles to devices
        // Manage voice allocation
        // Balance levels
        
        foreach (var assignment in plan.DeviceAssignments)
        {
            await AssignDeviceToRoleAsync(assignment.Device, assignment.Role);
        }
        
        // Monitor and adjust
        while (!ct.IsCancellationRequested)
        {
            await BalanceDevicesAsync();
            await Task.Delay(1000, ct);
        }
    }
}
```

**Checklist:**
- [ ] Multi-device coordination
- [ ] Role assignment
- [ ] Voice allocation management
- [ ] Dynamic rebalancing
- [ ] Traced in dashboard

**Estimated Time:** 6 hours

---

## Definition of Done

- ✅ 15+ skills implemented
- ✅ All 4 agents working
- ✅ Complex workflows functional ("build over 3 minutes")
- ✅ Real-time MIDI input analysis
- ✅ All agents traced in dashboard
- ✅ Tests for all skills and agents
- ✅ Documentation updated

---

## Deliverables

1. **Advanced Skills** - Analysis, generation, transformation
2. **4 Agents** - Autonomous and coordinated
3. **Complex Workflows** - Multi-device, multi-minute compositions
4. **Real-Time Jamming** - Listen and respond mode
5. **Full Observability** - Agent state machines visible

---

**Sprint Status:** 🔲 Not Started  
**Updated:** January 29, 2026
