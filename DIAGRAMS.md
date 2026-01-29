# SqncR Visual Architecture Guide

**Comprehensive visual documentation of SqncR architecture, flows, and telemetry**

---

## Table of Contents

- [System Overview](#system-overview)
- [Transport Layer Architecture](#transport-layer-architecture)
- [MIDI Message Flow](#midi-message-flow)
- [Skill Execution Flow](#skill-execution-flow)
- [Agent State Machines](#agent-state-machines)
- [Telemetry & Observability](#telemetry--observability)
- [User Workflows](#user-workflows)
- [Device Orchestration](#device-orchestration)
- [Data Flow](#data-flow)

---

## System Overview

### High-Level Architecture

```mermaid
flowchart TB
    subgraph "User Interfaces"
        CLI["CLI Tool<br/>sqncr.exe"]
        Claude["Claude Desktop<br/>via MCP"]
        API["REST API<br/>HTTP Client"]
        SDK["SDK Library<br/>.NET App"]
    end
    
    subgraph "SqncR Core Services"
        SqncRService["SqncRService<br/>Main Facade"]
        SkillRegistry["Skill Registry<br/>43+ Skills"]
        AgentSystem["Agent System<br/>4 Autonomous Agents"]
    end
    
    subgraph "Domain Services"
        MidiService["MIDI Service<br/>DryWetMidi"]
        TheoryService["Music Theory<br/>Scales, Chords, etc."]
        StateService["State Management<br/>EF Core + SQLite"]
    end
    
    subgraph "Infrastructure"
        Telemetry["OpenTelemetry<br/>Traces, Metrics, Logs"]
        Aspire["Aspire Dashboard<br/>http://localhost:15888"]
    end
    
    subgraph "Hardware"
        Polyend["Polyend Synth"]
        Moog["Moog Devices"]
        MESS["Polyend MESS"]
        Play["Polyend Play+"]
    end
    
    CLI --> SqncRService
    Claude --> SqncRService
    API --> SqncRService
    SDK --> SqncRService
    
    SqncRService --> SkillRegistry
    SqncRService --> AgentSystem
    
    SkillRegistry --> MidiService
    SkillRegistry --> TheoryService
    AgentSystem --> MidiService
    AgentSystem --> StateService
    
    MidiService --> Telemetry
    TheoryService --> Telemetry
    AgentSystem --> Telemetry
    
    Telemetry --> Aspire
    
    MidiService --> Polyend
    MidiService --> Moog
    MidiService --> MESS
    MidiService --> Play
    
    style SqncRService fill:#f0e1ff
    style SkillRegistry fill:#e1ffe1
    style AgentSystem fill:#ffe1e1
    style Telemetry fill:#e1f5ff
```

---

## Transport Layer Architecture

### All Transports Use Same Core

```mermaid
flowchart LR
    subgraph "Transport Layer"
        direction TB
        CLI["CLI<br/>System.CommandLine"]
        MCP["MCP Server<br/>MCP.NET SDK"]
        API["REST API<br/>ASP.NET Core"]
        SDK["SDK<br/>Fluent API"]
    end
    
    subgraph "Service Layer (Transport-Agnostic)"
        direction TB
        Core["SqncR.Core<br/>Business Logic"]
        Skills["Skills<br/>Composable"]
        Agents["Agents<br/>Autonomous"]
    end
    
    subgraph "Domain Layer"
        direction TB
        MIDI["MIDI Service"]
        Theory["Theory Service"]
        State["State Service"]
    end
    
    CLI --> Core
    MCP --> Core
    API --> Core
    SDK --> Core
    
    Core --> Skills
    Core --> Agents
    
    Skills --> MIDI
    Skills --> Theory
    Agents --> State
    
    style Core fill:#f0e1ff
    style CLI fill:#ffe1e1
    style MCP fill:#ffe1e1
    style API fill:#ffe1e1
    style SDK fill:#ffe1e1
```

**Key Insight:** SqncR.Core has ZERO transport dependencies. Add new transports (SSH, gRPC, WebSocket) without changing Core!

---

## MIDI Message Flow

### From User Request to Hardware

```mermaid
sequenceDiagram
    participant User
    participant SqncR as SqncRService
    participant Skill as SendMidiSkill
    participant MIDI as MidiService
    participant OTel as OpenTelemetry
    participant Device as Polyend Synth
    participant Dashboard as Aspire Dashboard
    
    User->>SqncR: "Play C4 on Polyend"
    activate SqncR
    
    SqncR->>Skill: Execute(device, note, velocity)
    activate Skill
    
    Skill->>OTel: StartActivity("SendMidiSkill")
    OTel->>Dashboard: Trace Start
    
    Skill->>MIDI: SendNoteOnAsync(0, 1, 60, 80)
    activate MIDI
    
    MIDI->>OTel: StartActivity("SendMidiNoteOn")
    MIDI->>OTel: SetTag("device_name", "Polyend Synth")
    MIDI->>OTel: SetTag("channel", 1)
    MIDI->>OTel: SetTag("note", 60)
    MIDI->>OTel: SetTag("velocity", 80)
    
    Note over MIDI: Measure latency
    MIDI->>Device: MIDI NOTE_ON
    Device-->>MIDI: (sound plays)
    
    MIDI->>OTel: SetTag("latency_ms", 3.2)
    MIDI->>OTel: EndActivity
    deactivate MIDI
    
    Skill->>OTel: EndActivity
    deactivate Skill
    
    SqncR-->>User: "Playing C4"
    deactivate SqncR
    
    OTel->>Dashboard: Complete Trace
    Dashboard->>Dashboard: Display trace tree
```

**Trace in Aspire Dashboard:**
```
SendMidiSkill (5.4ms)
  └─ SendMidiNoteOn (3.8ms)
     • device: Polyend Synth MIDI 1
     • channel: 1
     • note: 60 (C4)
     • velocity: 80
     • latency: 3.2ms
```

---

## Skill Execution Flow

### How Skills Are Discovered and Executed

```mermaid
flowchart TD
    Start([User Request]) --> Parse[Parse Request]
    Parse --> Identify[Identify Required Skills]
    
    Identify --> Multi{Multiple Skills?}
    
    Multi -->|Yes| Orchestrate[Orchestrate Execution]
    Multi -->|No| Single[Execute Single Skill]
    
    Orchestrate --> Registry[Query SkillRegistry]
    Single --> Registry
    
    Registry --> GetSkill[Get Skill by Name]
    GetSkill --> Exists{Skill Exists?}
    
    Exists -->|No| Error[Throw SkillNotFoundException]
    Exists -->|Yes| CreateActivity[Create Activity Span]
    
    CreateActivity --> TagInput[Tag with Input Parameters]
    TagInput --> Execute[Execute Skill Logic]
    
    Execute --> Success{Success?}
    
    Success -->|Yes| TagSuccess[Tag success=true]
    Success -->|No| TagError[Tag success=false<br/>Record exception]
    
    TagSuccess --> EndActivity[End Activity]
    TagError --> EndActivity
    
    EndActivity --> Return[Return Result]
    Return --> End([Complete])
    
    Error --> End
    
    style Registry fill:#e1ffe1
    style CreateActivity fill:#e1f5ff
    style Execute fill:#f0e1ff
```

### Skill Composition Example

```mermaid
flowchart LR
    Request["User: 'ambient drone in Am, darker'"] --> VibeSkill["skill-vibe-to-music"]
    VibeSkill --> ScaleSkill["skill-scale-selector"]
    ScaleSkill --> ChordSkill["skill-chord-progression"]
    ChordSkill --> DeviceSkill["skill-device-selector"]
    DeviceSkill --> SendSkill["skill-send-midi"]
    SendSkill --> Result["Music Playing"]
    
    VibeSkill -.->|"mode: phrygian<br/>brightness: -0.3"| ScaleSkill
    ScaleSkill -.->|"A Phrygian scale"| ChordSkill
    ChordSkill -.->|"Am, Bb, Dm"| DeviceSkill
    DeviceSkill -.->|"Polyend Synth<br/>Channel 1"| SendSkill
    
    style VibeSkill fill:#ffe1e1
    style ScaleSkill fill:#e1ffe1
    style ChordSkill fill:#e1ffe1
    style DeviceSkill fill:#f0e1ff
    style SendSkill fill:#e1f5ff
```

---

## Agent State Machines

### CompositionAgent State Machine

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

### ListenerAgent Real-Time Analysis

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

---

## Telemetry & Observability

### OpenTelemetry Spans Hierarchy

```mermaid
flowchart TD
    Request["HTTP Request<br/>POST /api/generate"] --> SqncR["SqncRService.GenerateAsync<br/>(25ms)"]
    
    SqncR --> Skill1["skill-list-devices<br/>(8ms)"]
    SqncR --> Skill2["skill-vibe-to-music<br/>(2ms)"]
    SqncR --> Skill3["skill-chord-progression<br/>(5ms)"]
    SqncR --> Skill4["skill-device-selector<br/>(3ms)"]
    SqncR --> Skill5["skill-send-midi<br/>(7ms)"]
    
    Skill1 --> MIDI1["MidiService.ListDevices<br/>(6ms)"]
    Skill5 --> MIDI2["MidiService.SendNoteOn<br/>(4ms)"]
    
    MIDI2 --> Device["MIDI Hardware I/O<br/>(3ms)"]
    
    style Request fill:#e1f5ff
    style SqncR fill:#f0e1ff
    style Skill1 fill:#e1ffe1
    style Skill2 fill:#e1ffe1
    style Skill3 fill:#e1ffe1
    style Skill4 fill:#e1ffe1
    style Skill5 fill:#e1ffe1
    style MIDI2 fill:#ffe1e1
```

**Aspire Dashboard View:**
```
Trace: d4f2e1a8-9b3c-4d5e-8f9a-1b2c3d4e5f6a
Duration: 25ms

SqncRService.GenerateAsync (25ms)
├─ skill-list-devices (8ms)
│  └─ MidiService.ListDevices (6ms)
│     • devices_found: 4
│     • scan_time_ms: 5.8
├─ skill-vibe-to-music (2ms)
│  • concept: "darker"
│  • mode: "phrygian"
│  • brightness: -0.3
├─ skill-chord-progression (5ms)
│  • key: "A"
│  • mode: "minor"
│  • chords: ["Am7", "Dm7", "Fmaj7", "E7"]
├─ skill-device-selector (3ms)
│  • role: "bass"
│  • selected: "Moog Mother-32"
│  • reasoning: "Analog warmth perfect for sub-bass"
└─ skill-send-midi (7ms)
   └─ MidiService.SendNoteOn (4ms)
      • device: "Polyend Synth MIDI 1"
      • channel: 1
      • note: 60
      • velocity: 80
      • latency_ms: 3.2
```

### Telemetry Metrics Map

```mermaid
flowchart TB
    subgraph "Metrics Collected"
        M1["midi.messages.sent<br/>(Counter)"]
        M2["midi.latency_ms<br/>(Histogram)"]
        M3["skill.execution_time_ms<br/>(Histogram)"]
        M4["skill.success_count<br/>(Counter)"]
        M5["skill.error_count<br/>(Counter)"]
        M6["agent.state_transitions<br/>(Counter)"]
        M7["devices.connected<br/>(Gauge)"]
        M8["generation.active_sessions<br/>(Gauge)"]
    end
    
    subgraph "Dimensions (Tags)"
        D1["device_name"]
        D2["device_type"]
        D3["channel"]
        D4["skill_name"]
        D5["agent_name"]
        D6["agent_state"]
        D7["success"]
    end
    
    M1 --> D1
    M1 --> D2
    M1 --> D3
    
    M2 --> D1
    M2 --> D3
    
    M3 --> D4
    M4 --> D4
    M5 --> D4
    
    M6 --> D5
    M6 --> D6
    
    M7 --> D2
    
    style M1 fill:#e1ffe1
    style M2 fill:#ffe1e1
    style M3 fill:#e1f5ff
    style M4 fill:#e1ffe1
    style M5 fill:#ffe1e1
```

---

## User Workflows

### Workflow 1: "List My MIDI Devices"

```mermaid
sequenceDiagram
    participant User
    participant CLI as CLI/MCP/API
    participant SqncR as SqncRService
    participant Skill as ListDevicesSkill
    participant MIDI as MidiService
    participant Registry as DeviceRegistry
    participant OTel as OpenTelemetry
    
    User->>CLI: "list my midi devices"
    CLI->>SqncR: ListDevicesAsync()
    SqncR->>Skill: ExecuteAsync()
    
    Skill->>OTel: StartActivity("skill-list-devices")
    
    Skill->>MIDI: ListDevicesAsync()
    MIDI->>MIDI: Enumerate OS MIDI ports
    MIDI->>Registry: MatchProfiles(portNames)
    
    Registry-->>MIDI: Matched profiles
    MIDI-->>Skill: Device list
    
    Skill->>OTel: SetTag("device_count", 4)
    Skill->>OTel: EndActivity
    
    Skill-->>SqncR: Result(devices)
    SqncR-->>CLI: devices
    CLI-->>User: Display table
    
    Note over User: Index | Device Name | Type | Channels<br/>0 | Polyend Synth | Synth | 1,2,3<br/>1 | Moog Mother-32 | Synth | 1<br/>2 | MAFD | Controller | 1<br/>3 | Polyend MESS | FX | 1
```

### Workflow 2: "Generate Ambient Drone in A Minor"

```mermaid
flowchart TD
    Start([User Request]) --> Parse["Parse: 'ambient drone in A minor'"]
    
    Parse --> Vibe["skill-vibe-to-music<br/>concept: ambient"]
    Vibe --> Scale["skill-scale-selector<br/>key: A, mode: minor"]
    Scale --> Device["skill-device-selector<br/>role: pads"]
    
    Device --> Select{Device Selected}
    Select -->|Polyend Synth| Channel["Determine channel"]
    Select -->|User specifies| Channel
    
    Channel --> Progression["skill-chord-progression<br/>Am, Dm, F, E"]
    Progression --> Generate["Generate MIDI sequence"]
    
    Generate --> Send1["skill-send-midi<br/>Send Am chord"]
    Send1 --> Send2["skill-send-midi<br/>Send Dm chord"]
    Send2 --> Send3["skill-send-midi<br/>Send F chord"]
    Send3 --> Send4["skill-send-midi<br/>Send E chord"]
    
    Send4 --> Loop{Continue?}
    Loop -->|Yes| Send1
    Loop -->|No| Stop([Stop])
    
    style Vibe fill:#e1ffe1
    style Scale fill:#e1ffe1
    style Device fill:#f0e1ff
    style Progression fill:#e1ffe1
    style Send1 fill:#ffe1e1
    style Send2 fill:#ffe1e1
    style Send3 fill:#ffe1e1
    style Send4 fill:#ffe1e1
```

### Workflow 3: "Make It Darker" (Real-Time Modification)

```mermaid
sequenceDiagram
    participant User
    participant SqncR as SqncRService
    participant Session as SessionManager
    participant Vibe as VibeToMusicSkill
    participant Comp as CompositionAgent
    participant MIDI as MidiService
    
    Note over User: Music currently playing in Am
    
    User->>SqncR: ModifyAsync("darker")
    SqncR->>Session: GetCurrentContext()
    Session-->>SqncR: Context(key: Am, devices: [Polyend])
    
    SqncR->>Vibe: Execute("darker", context)
    Vibe-->>SqncR: mode: phrygian, brightness: -0.3
    
    SqncR->>Comp: ModifyGeneration(phrygian, -0.3)
    
    Comp->>Comp: Transition: Am → A Phrygian
    Comp->>MIDI: Update note selection
    Comp->>MIDI: Lower velocities (-15)
    Comp->>MIDI: Drop pitch register
    
    MIDI->>MIDI: Polyend Synth
    
    Note over User: Music shifts to darker Phrygian mode
```

---

## Device Orchestration

### Multi-Device Coordination

```mermaid
flowchart TB
    Request["User: 'build something over 3 minutes'"] --> Agent["CompositionAgent"]
    
    Agent --> Plan["Plan Structural Arc"]
    Plan --> Timeline["Timeline:<br/>0:00-1:00 Intro<br/>1:00-2:00 Build<br/>2:00-2:30 Peak<br/>2:30-3:00 Resolve"]
    
    Timeline --> Orch["DeviceOrchestratorAgent"]
    
    Orch --> Assign["Assign Devices to Roles"]
    
    Assign --> D1["Polyend Synth<br/>Role: Pads<br/>Channels: 1,2"]
    Assign --> D2["Moog Mother-32<br/>Role: Bass<br/>Channel: 4"]
    Assign --> D3["Polyend MESS<br/>Role: FX/Texture<br/>Channel: 5"]
    Assign --> D4["Polyend Play+<br/>Role: Percussion<br/>Channels: 6-8"]
    
    D1 --> Intro["0:00-1:00 INTRO"]
    D2 --> Intro
    
    Intro --> Build["1:00-2:00 BUILD"]
    D3 --> Build
    D4 --> Build
    
    Build --> Peak["2:00-2:30 PEAK"]
    Peak --> Resolve["2:30-3:00 RESOLVE"]
    Resolve --> End["Music Complete"]
    
    style Agent fill:#ffe1e1
    style Orch fill:#f0e1ff
    style D1 fill:#e1ffe1
    style D2 fill:#e1ffe1
    style D3 fill:#e1ffe1
    style D4 fill:#e1ffe1
```

### Device State & Voice Allocation

```mermaid
stateDiagram-v2
    [*] --> Scanning: Scan Devices
    
    Scanning --> Available: Devices Found
    Available --> Assigned: Assign to Role
    
    Assigned --> Active: Start Playing
    Active --> Assigned: Stop Playing
    
    Active --> VoiceAllocation: Check Polyphony
    VoiceAllocation --> Active: Voices Available
    VoiceAllocation --> Throttled: Voices Exhausted
    
    Throttled --> Active: Voice Released
    
    Assigned --> Available: Unassign
    Available --> Disconnected: Device Removed
    Disconnected --> [*]
    
    note right of Scanning
        Query OS MIDI ports
        Match to profiles
    end note
    
    note right of Assigned
        Role: bass, chords, pads, etc.
        Channel: 1-16
        Profile: device capabilities
    end note
    
    note right of VoiceAllocation
        Polyend Synth: 8 voices
        Moog Mother-32: 1 voice
        Track active notes
        Queue if exhausted
    end note
```

---

## Data Flow

### From User Intent to Sound

```mermaid
flowchart LR
    subgraph "Input Layer"
        U1["Natural Language<br/>'ambient drone in Am'"]
        U2["Structured Request<br/>{key:'Am', style:'ambient'}"]
    end
    
    subgraph "Intelligence Layer"
        Parse["Intent Parser"]
        Vibe["Vibe-to-Music<br/>Skill"]
        Theory["Music Theory<br/>Service"]
    end
    
    subgraph "Generation Layer"
        Scale["Scale<br/>Generation"]
        Chord["Chord<br/>Progression"]
        Voice["Voice<br/>Leading"]
    end
    
    subgraph "Device Layer"
        Select["Device<br/>Selector"]
        Map["Channel<br/>Mapping"]
    end
    
    subgraph "Output Layer"
        MIDI["MIDI<br/>Messages"]
        Hardware["Hardware<br/>Synths"]
        Sound["Sound<br/>Waves"]
    end
    
    U1 --> Parse
    U2 --> Parse
    Parse --> Vibe
    Vibe --> Theory
    
    Theory --> Scale
    Scale --> Chord
    Chord --> Voice
    
    Voice --> Select
    Select --> Map
    
    Map --> MIDI
    MIDI --> Hardware
    Hardware --> Sound
    
    style U1 fill:#e1f5ff
    style Parse fill:#f0e1ff
    style Theory fill:#e1ffe1
    style Voice fill:#ffe1e1
    style MIDI fill:#ffe1e1
    style Sound fill:#e1ffe1
```

### Session State Data Model

```mermaid
erDiagram
    Session ||--o{ GenerationConfig : has
    Session ||--o{ DeviceAssignment : manages
    Session ||--o{ UserPreference : stores
    Session ||--|| MusicalContext : maintains
    
    GenerationConfig ||--o{ ChordProgression : defines
    GenerationConfig ||--|| TempoInfo : has
    GenerationConfig ||--|| KeyInfo : has
    
    DeviceAssignment ||--|| MidiDevice : references
    DeviceAssignment ||--|| DeviceRole : has
    
    MidiDevice ||--|| DeviceProfile : matches
    
    Session {
        guid SessionId PK
        datetime StartTime
        datetime EndTime
        string CurrentState
    }
    
    GenerationConfig {
        guid ConfigId PK
        guid SessionId FK
        string Key
        string Mode
        int Tempo
        string Style
    }
    
    DeviceAssignment {
        guid AssignmentId PK
        guid SessionId FK
        int DeviceIndex
        string Role
        int Channel
    }
    
    MidiDevice {
        int Index PK
        string PortName
        string ProfileId
        string Manufacturer
        string Type
    }
```

---

## Summary: Key Architectural Patterns

### 1. Transport-Agnostic Core
✅ **Pattern:** All transports call same SqncR.Core  
✅ **Benefit:** Add new interfaces without touching business logic  
✅ **Observable:** Every transport shows same traces in dashboard

### 2. Skill Composition
✅ **Pattern:** Small, focused skills combined for complex workflows  
✅ **Benefit:** Testable, reusable, composable  
✅ **Observable:** Each skill is a separate span with tags

### 3. Event-Driven Agents
✅ **Pattern:** Agents communicate via events, maintain state machines  
✅ **Benefit:** Autonomous behavior, coordinated without tight coupling  
✅ **Observable:** State transitions traced

### 4. OpenTelemetry First
✅ **Pattern:** Instrumentation built into framework (SkillBase, AgentBase)  
✅ **Benefit:** Every operation visible by default  
✅ **Observable:** Complete distributed trace from request to MIDI hardware

### 5. Device Abstraction
✅ **Pattern:** Device profiles separate from device control logic  
✅ **Benefit:** Add new devices via configuration, not code  
✅ **Observable:** Device operations tagged with profile metadata

---

## See Also

- [ARCHITECTURE.md](ARCHITECTURE.md) - Detailed architecture documentation
- [AGENTIC_ARCHITECTURE.md](AGENTIC_ARCHITECTURE.md) - Skills and agents deep dive
- [OBSERVABILITY.md](OBSERVABILITY.md) - OpenTelemetry implementation details
- [SKILLS.md](SKILLS.md) - Complete skills catalog
- [ROADMAP.md](ROADMAP.md) - Implementation roadmap

---

**Last Updated:** January 29, 2026
