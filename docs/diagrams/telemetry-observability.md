# Telemetry & Observability

## OpenTelemetry Spans Hierarchy

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

## Aspire Dashboard View

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

## Telemetry Metrics Map

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

## Key Observability Features

### Distributed Tracing
- ✅ Every request traced from entry to MIDI hardware
- ✅ Skill composition visible in spans
- ✅ MIDI latency measured precisely
- ✅ Agent state transitions tracked

### Metrics
- **Counters**: Messages sent, successes, errors
- **Histograms**: Latency distributions, execution times
- **Gauges**: Active connections, sessions, devices

### Structured Logging
- ✅ Consistent log format across services
- ✅ Correlation IDs for request tracking
- ✅ Contextual data in every log entry
- ✅ Log levels: Debug, Info, Warning, Error

### Dashboard Integration
- ✅ Real-time visualization in Aspire Dashboard
- ✅ Trace timeline view
- ✅ Metrics charts and graphs
- ✅ Log aggregation and filtering

## Performance Targets

| Metric | Target | Typical |
|--------|--------|---------|
| **MIDI Latency** | < 10ms | 3-5ms |
| **Skill Execution** | < 50ms | 10-30ms |
| **Request to Sound** | < 100ms | 40-60ms |
| **Trace Overhead** | < 5% | 2-3% |

---

**See Also:**
- [MIDI Message Flow](midi-message-flow.md)
- [../OBSERVABILITY.md](../OBSERVABILITY.md)
- [Aspire Dashboard Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard)
