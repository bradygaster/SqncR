# Device Orchestration

## Multi-Device Coordination

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

## Device State & Voice Allocation

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

## Device Roles

### Musical Roles

| Role | Description | Typical Devices |
|------|-------------|-----------------|
| **Bass** | Low-frequency foundation | Moog Mother-32, Polyend Synth (bass engine) |
| **Pads** | Sustained harmonic texture | Polyend Synth (pad mode) |
| **Lead** | Melodic focus | Any synth with bright timbre |
| **Chords** | Harmonic accompaniment | Polyphonic synths |
| **Texture** | Atmospheric elements | FX processors, samplers |
| **Percussion** | Rhythmic elements | Drum machines, DFAM, Play+ |

### Orchestration Strategies

1. **Role-Based Assignment**
   - Assign devices based on their characteristics
   - Match timbre to musical role
   - Consider polyphony requirements

2. **Layer Management**
   - Intro: 1-2 devices
   - Build: Add 1 device at a time
   - Peak: All devices active
   - Resolve: Remove devices progressively

3. **Voice Allocation**
   - Track active notes per device
   - Respect polyphony limits
   - Queue notes if voices exhausted
   - Release voices intelligently

4. **Timing Coordination**
   - Synchronize across devices
   - Tempo-locked timing
   - Phase-aligned patterns
   - Coordinated stops/starts

---

**See Also:**
- [Agent State Machines](agent-state-machines.md)
- [Device Profiles](../MUSIC_THEORY.md)
- [System Overview](system-overview.md)
