# SqncR Architecture - AI-Native Music Service

## Core Concept
SqncR is an **AI-conversational music service** that runs alongside your coding workflow. Chat with it in Claude/Copilot while you code, and it controls your MIDI setup in real-time. Built as an MCP server with extensible tools.

## Target User
Technical musicians who:
- Code with AI assistants all day
- Have MIDI gear/VSTs ready to go
- Want generative music while working
- Think in APIs, protocols, and real-time systems
- Appreciate the efficiency of conversational interfaces

## Architecture Philosophy

### AI-First Design
```
[You Coding in IDE] ←→ [Claude/Copilot with MCP]
                              ↓
                         [SqncR MCP Server]
                              ↓
                    [MIDI Hardware/Software]
                              ↓
                         [Sound Output]
```

**The Experience:**
- Left monitor: VSCode with Copilot
- Right monitor: Claude Desktop with SqncR MCP server connected
- You chat: "list my midi devices"
- Claude calls SqncR tool, shows you devices
- You: "start an ambient drone on the Moog, keep it minimal"
- Music starts playing through your actual hardware
- You keep coding, occasionally tweaking: "add some texture"

### MCP Server Architecture

SqncR exposes tools/resources via Model Context Protocol:

```typescript
// MCP Tools (callable by AI)
{
  "list_midi_devices": {},
  "setup_instrument": {
    "device": "string",
    "description": "string",
    "role": "bass|pad|lead|texture|drums"
  },
  "start_generation": {
    "description": "string",  // natural language
    "instruments": ["string"],
    "key": "string",
    "tempo": "number",
    "intensity": "0-10"
  },
  "modify_generation": {
    "instruction": "string"  // "make it darker", "add rhythmic elements"
  },
  "stop_generation": {},
  "get_current_state": {},
  "save_session": {
    "name": "string"
  },
  "load_session": {
    "name": "string"
  }
}
```

### Extensible Tool System

Each capability is a discrete tool/skill:

**Tier 1: Core MIDI (executable tools)**
- `sqncr-list-devices.exe` - Scan and list MIDI devices
- `sqncr-send-midi.exe` - Send raw MIDI messages
- `sqncr-monitor.exe` - Listen to MIDI input
- `sqncr-routing.exe` - Virtual MIDI routing management

**Tier 2: Generation Service (long-running)**
- `sqncr-server.exe` - Main MCP server
  - Manages generation state
  - Handles AI-to-MIDI translation
  - Keeps context of current musical state
  - Exposes MCP tools

**Tier 3: Intelligence (plugins/modules)**
- Music theory engine (scales, chords, progressions)
- Pattern generators (algorithmic composition)
- ML models (style transfer, continuation)
- Analysis (real-time harmonic analysis)

## Service Design

### MCP Server Implementation

```typescript
// sqncr-server structure
class SqncRServer {
  // MCP protocol handlers
  tools: ToolRegistry
  resources: ResourceRegistry
  
  // Core services
  midiManager: MIDIManager
  generationEngine: GenerationEngine
  contextManager: ContextManager
  instrumentRegistry: InstrumentRegistry
  
  // State
  currentSession: Session
  activeGenerators: Generator[]
}
```

### Multi-Protocol Support

**MCP (Primary)**

Learn more: [Model Context Protocol](https://modelcontextprotocol.io/), [MCP Specification](https://spec.modelcontextprotocol.io/)

```json
{
  "mcpServers": {
    "sqncr": {
      "command": "sqncr-server.exe",
      "args": ["--mcp"],
      "env": {
        "SQNCR_CONFIG": "~/.sqncr/config.json"
      }
    }
  }
}
```

**GitHub Copilot Skills**
```yaml
# sqncr.copilot-skill.yaml
name: SqncR Music Generation
description: Control MIDI devices and generate music
tools:
  - name: list_midi_devices
    type: executable
    path: ./tools/sqncr-list-devices.exe
  - name: generate_music
    type: service
    endpoint: http://localhost:8765/generate
```

**Standalone CLI**
```bash
sqncr list-devices
sqncr setup --device "Moog" --role bass --description "warm analog bass"
sqncr generate "ambient drone in Cm, slow and spacious"
sqncr modify "add more movement"
sqncr stop
```

## Technical Stack Considerations

### Language Choice: C# / .NET 9+

**Why .NET + Aspire:**
- **Observability First**: OpenTelemetry built-in, Aspire Dashboard for real-time visualization
- **Performance**: Low-latency MIDI with modern .NET runtime
- **Type Safety**: Strong typing for music theory, device profiles, MIDI messages
- **Distributed**: Aspire orchestrates services (MCP server, MIDI handler, theory engine)
- **Tooling**: Excellent IDE support, hot reload, debugging
- **Production Ready**: ASP.NET Core, Entity Framework Core, battle-tested

### Key Libraries

**Aspire & Observability:**
- [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) - Distributed application framework
- [OpenTelemetry](https://opentelemetry.io/) - Tracing, metrics, logs
- [Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard) - Real-time monitoring

**MIDI:**
- [Melanchall.DryWetMidi](https://github.com/melanchall/drywetmidi) - Comprehensive .NET MIDI library
  - Cross-platform MIDI I/O
  - High-resolution timing
  - Device management
  - MIDI file support
- [NAudio.Midi](https://github.com/naudio/NAudio) - Alternative option
- Virtual MIDI: [LoopMIDI](https://www.tobias-erichsen.de/software/loopmidi.html) (Windows), CoreMIDI (macOS)

**MCP Protocol:**
- [MCP.NET SDK](https://github.com/modelcontextprotocol/csharp-sdk) - C# implementation
- ASP.NET Core for server hosting
- gRPC or SignalR for real-time communication

**Music Theory:**
- Custom C# library inspired by [`tonal`](https://github.com/tonaljs/tonal)
- Value types for performance (Note, Interval, Scale, Chord)
- Immutable data structures
- Strongly typed throughout

**State Management:**
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/) with SQLite
- Session persistence
- Device registry
- User presets

**AI/ML (Future):**
- [ML.NET](https://dotnet.microsoft.com/en-us/apps/machinelearning-ai/ml-dotnet) - .NET machine learning
- [ONNX Runtime](https://onnxruntime.ai/) - Cross-platform ML inference
- Or keep algorithmic initially

## Service Architecture

### Process Model

```
┌─────────────────────────────────────────┐
│  Claude Desktop / Copilot CLI           │
│  (AI Assistant)                         │
└─────────────┬───────────────────────────┘
              │ MCP Protocol
              ↓
┌─────────────────────────────────────────┐
│  SqncR MCP Server (sqncr-server.exe)    │
│  - Tool handlers                        │
│  - Session management                   │
│  - Generation coordination              │
└─────────────┬───────────────────────────┘
              │
        ┌─────┴─────┬──────────────┐
        ↓           ↓              ↓
    [MIDI Out]  [Generator]   [Analyzer]
        │           │              │
        ↓           ↓              ↓
    Hardware    Algorithms    MIDI Input
```

### State Management

```typescript
interface Session {
  id: string
  instruments: Instrument[]
  currentGeneration: GenerationConfig | null
  musicalContext: {
    key: string
    scale: string
    tempo: number
    timeSignature: string
    intensity: number
  }
  history: Action[]
}

interface Instrument {
  deviceId: string
  name: string
  role: 'bass' | 'pad' | 'lead' | 'texture' | 'drums' | 'other'
  description: string
  midiChannel: number
  characteristics: {
    range: [number, number]  // MIDI note range
    velocity: [number, number]
    timbre: string[]  // tags: warm, bright, dark, etc.
  }
}
```

## Conversational Workflows

### Initial Setup Flow
```
You: "list my midi devices"
SqncR: Shows devices via MCP tool
You: "setup device 2 as a warm pad, call it Prophet"
SqncR: Registers instrument
You: "what instruments do I have configured?"
SqncR: Shows instrument registry
```

### Generation Flow
```
You: "generate something ambient while I code"
SqncR: Asks clarifying questions or starts with defaults
You: "use the Prophet, keep it in minor, slow"
SqncR: Starts generation, music plays
You: (coding for 10 minutes)
You: "make it a bit brighter"
SqncR: Adjusts generation parameters
```

### Advanced Flow
```
You: "I'm going to play some chords, listen and complement me"
SqncR: Starts monitoring your MIDI input
You: (plays Cmaj7 - Am7)
SqncR: Detects progression, generates complementary bass line
You: "nice, keep that going but add some texture"
SqncR: Adds subtle textural layer on another instrument
```

## Development Phases

### Phase 0: Proof of Concept
- Single executable: `sqncr-list-devices.exe`
- Shows MIDI devices as JSON
- Callable from CLI or as MCP tool
- Validates the tool-based approach

### Phase 1: Basic MCP Server
- `sqncr-server.exe` with MCP protocol
- Tools: list_devices, send_midi, start_simple_generator
- Simple algorithmic generator (arpeggio, drone)
- Configurable via conversation

### Phase 2: Instrument Intelligence
- Setup and describe instruments
- Persistent configuration
- Role-aware generation
- Musical context tracking

### Phase 3: Advanced Generation
- Multiple generation algorithms
- Real-time modification
- Listen and adapt to input
- Session save/load

### Phase 4: ML Integration
- Style models
- Pattern learning
- Collaborative generation
- Community presets

## Configuration

### User Config (~/.sqncr/config.json)
```json
{
  "midi": {
    "virtualPorts": true,
    "latencyMs": 5
  },
  "instruments": [
    {
      "name": "Moog",
      "deviceId": "Moog Sub 37",
      "role": "bass",
      "description": "Analog bass synth, warm and fat",
      "defaultChannel": 1
    }
  ],
  "presets": {
    "ambient_drone": {
      "tempo": 60,
      "key": "Cm",
      "scale": "minor",
      "intensity": 3
    }
  }
}
```

## Why This Architecture Works

**For the AI-native musician:**
- No context switching from coding
- Natural language control
- Stateful sessions
- Extensible via new tools
- Works in any AI assistant that supports MCP/skills

**For development:**
- Start simple (single .exe tools)
- Add complexity incrementally
- Each tool is testable independently
- Clear separation of concerns
- Community can add generators/tools

**For music creation:**
- Low latency (dedicated MIDI handling)
- Flexible (algorithmic + ML)
- Transparent (see what's happening)
- Interruptible (stop/modify anytime)

## Open Questions

1. **State persistence**: SQLite? JSON files? In-memory only?
2. **ML models**: Local ONNX? Cloud API? Start without?
3. **Virtual MIDI**: Bundle driver? User installs separately?
4. **Web UI**: Optional dashboard for visual feedback?
5. **DAW integration**: Also target Ableton Link, VST hosting?

## Next: MVP Definition

The minimal version that's actually useful:
- List MIDI devices
- Send notes to a device via conversation
- Simple drone/arpeggio generator
- Start/stop/modify via MCP tools
- Save one instrument config

This proves the concept and we build from there.
