# MIDI Message Flow

**From User Request to Hardware**

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

## Trace in Aspire Dashboard

```
SendMidiSkill (5.4ms)
  └─ SendMidiNoteOn (3.8ms)
     • device: Polyend Synth MIDI 1
     • channel: 1
     • note: 60 (C4)
     • velocity: 80
     • latency: 3.2ms
```

## Performance Characteristics

- **Target Latency:** < 10ms total
- **Typical Latency:** 3-5ms
- **MIDI I/O Latency:** 2-4ms
- **Observability Overhead:** < 1ms

## Key Features

1. **Full Observability** - Every MIDI message is traced with OpenTelemetry
2. **Precision Tagging** - Device, channel, note, velocity all captured
3. **Latency Measurement** - Precise timing for performance monitoring
4. **Real-time Dashboard** - View in Aspire Dashboard as it happens

---

**See Also:**
- [Telemetry & Observability](telemetry-observability.md)
- [Skill Execution Flow](skill-execution-flow.md)
- [../OBSERVABILITY.md](../OBSERVABILITY.md)
