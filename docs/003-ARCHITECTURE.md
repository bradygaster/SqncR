# SqncR Architecture

How SqncR is structured and why.

---

## Core Concept

SqncR is a **service-first, transport-agnostic** system. One core, multiple interfaces.

```
┌─────────────────────────────────────────────────┐
│                  SqncRService                   │
│         (Skills, generation, session)           │
└────────────────────┬────────────────────────────┘
                     │
         ┌───────────┼───────────┐
         │           │           │
    ┌────┴────┐ ┌────┴────┐ ┌────┴────┐
    │   CLI   │ │   MCP   │ │   API   │
    │ sqncr   │ │ Server  │ │  (REST) │
    └─────────┘ └─────────┘ └─────────┘
```

All business logic lives in the core. Transports are thin.

---

## Project Structure

```
src/
├── SqncR.Cli/        # Command-line tool
├── SqncR.Core/       # Business logic, skills
├── SqncR.Midi/       # MIDI I/O with DryWetMidi
├── SqncR.Theory/     # Music theory (Note, Scale, Chord)
├── SqncR.State/      # SQLite persistence
└── SqncR.McpServer/  # MCP protocol server
```

---

## Key Components

### SqncRService
Central facade. All operations go through here.

### Skills
Discrete, composable capabilities:
- `chord-progression` - Generate progressions
- `bass-line-generator` - Create bass lines
- `drone-generator` - Ambient drones

### MidiService
Wraps DryWetMidi. Device enumeration, note sending.

### SequencePlayer
Plays .sqnc.yaml files with correct timing.

### SessionService
SQLite persistence for saved sessions.

---

## Technology Stack

| Component | Technology |
|-----------|-----------|
| Runtime | .NET 9 |
| MIDI | Melanchall.DryWetMidi |
| CLI | System.CommandLine |
| Persistence | EF Core + SQLite |
| MCP | MCP.NET SDK |
| YAML | YamlDotNet |

---

## Design Principles

1. **Transport-agnostic** - Same core for CLI, MCP, API
2. **Skills are stateless** - Easy to test, compose
3. **Observable** - OpenTelemetry instrumentation
4. **Device-agnostic** - Profiles, not hardcoding
5. **Theory-aware** - Real music concepts, not just notes

---

## See Also

- [008-AGENTIC](./008-AGENTIC.md) - Skills and agents deep dive
- [../ARCHITECTURE.md](../ARCHITECTURE.md) - Original detailed architecture doc
- [../AGENTIC_ARCHITECTURE.md](../AGENTIC_ARCHITECTURE.md) - Full agentic design
