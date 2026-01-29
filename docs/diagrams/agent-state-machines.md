# Agent State Machines

## CompositionAgent State Machine

```mermaid
stateDiagram-v2
    [*] --> Idle
    
    Idle --> Analyzing: StartComposition()
    Analyzing --> Planning: AnalysisComplete
    Planning --> Intro: PlanReady
    
    Intro --> Building: IntroComplete
    Building --> Peak: BuildupComplete
    Peak --> Resolving: PeakComplete
    Resolving --> Outro: ResolveComplete
    Outro --> Idle: OutroComplete
    
    Analyzing --> Idle: Stop()
    Planning --> Idle: Stop()
    Intro --> Paused: Pause()
    Building --> Paused: Pause()
    Peak --> Paused: Pause()
    Resolving --> Paused: Pause()
    
    Paused --> Intro: Resume()
    Paused --> Building: Resume()
    Paused --> Peak: Resume()
    Paused --> Resolving: Resume()
    Paused --> Idle: Stop()
    
    note right of Analyzing
        Detect key, tempo
        Analyze user input
    end note
    
    note right of Planning
        Plan structural arc
        Assign devices to roles
        Calculate timing
    end note
    
    note right of Intro
        Sparse, minimal
        Establish key/mood
    end note
    
    note right of Building
        Add layers
        Increase complexity
        Build tension
    end note
    
    note right of Peak
        Maximum density
        All devices active
        Climax moment
    end note
    
    note right of Resolving
        Reduce layers
        Release tension
        Return to simplicity
    end note
```

## ListenerAgent Real-Time Analysis

```mermaid
sequenceDiagram
    participant User as User Playing
    participant Agent as ListenerAgent
    participant Theory as TheoryService
    participant Comp as CompositionAgent
    
    User->>Agent: MIDI Note Input
    activate Agent
    
    Agent->>Agent: Add to RecentNotes buffer
    
    Agent->>Theory: DetectKeyFromNotes(recent)
    activate Theory
    Theory-->>Agent: Key: Dm (80% confidence)
    deactivate Theory
    
    Agent->>Theory: AnalyzeChord(held notes)
    activate Theory
    Theory-->>Agent: Chord: Dm7
    deactivate Theory
    
    Agent->>Comp: OnKeyDetected(Dm)
    Agent->>Comp: OnChordDetected(Dm7)
    
    Comp->>Comp: Adjust generation to D minor
    Comp-->>User: Complementary harmony plays
    
    deactivate Agent
    
    Note over Agent,Comp: Latency: < 100ms
```

## Agent States and Transitions

### CompositionAgent States

| State | Description | Actions |
|-------|-------------|---------|
| **Idle** | No active composition | Awaiting start command |
| **Analyzing** | Analyzing user request | Parse intent, detect parameters |
| **Planning** | Planning structure | Create structural arc, assign devices |
| **Intro** | Playing introduction | Minimal, establish mood |
| **Building** | Building tension | Add layers, increase complexity |
| **Peak** | Maximum intensity | All devices, climax |
| **Resolving** | Releasing tension | Remove layers, simplify |
| **Outro** | Ending composition | Return to silence |
| **Paused** | Temporarily stopped | Can resume |

### Transitions

- **Start** - User initiates composition
- **Stop** - User stops composition (returns to Idle)
- **Pause** - Temporary suspension
- **Resume** - Continue from pause point
- **Automatic** - Phase completion triggers next phase

### Observability

All state transitions are:
- ✅ Traced with OpenTelemetry
- ✅ Tagged with current/next state
- ✅ Timed for duration in each state
- ✅ Visible in Aspire Dashboard

---

**See Also:**
- [Agentic Architecture](../AGENTIC_ARCHITECTURE.md)
- [Device Orchestration](device-orchestration.md)
