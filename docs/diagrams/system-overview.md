# System Overview Diagram

**High-Level SqncR Architecture**

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

## Key Components

### User Interfaces (Transport Layer)
- **CLI Tool** - Command-line interface (`sqncr.exe`)
- **Claude Desktop** - Model Context Protocol integration
- **REST API** - HTTP/JSON interface
- **SDK Library** - .NET package for direct integration

### SqncR Core Services
- **SqncRService** - Main facade that coordinates all operations
- **Skill Registry** - 43+ composable music skills
- **Agent System** - 4 autonomous agents for complex workflows

### Domain Services
- **MIDI Service** - Device I/O using DryWetMidi library
- **Theory Service** - Music theory computations
- **State Service** - Session persistence with EF Core + SQLite

### Infrastructure
- **OpenTelemetry** - Distributed tracing, metrics, and logs
- **Aspire Dashboard** - Real-time observability at http://localhost:15888

### Hardware
Supports multiple MIDI devices including Polyend, Moog, and other manufacturers.

---

**See Also:**
- [Transport Layer Architecture](transport-layer.md)
- [MIDI Message Flow](midi-message-flow.md)
- [Device Orchestration](device-orchestration.md)
