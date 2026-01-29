# SqncR Documentation Index

**Last Updated:** January 29, 2026 (Added sprint plans)

## Core Documentation

### Getting Started
- [README.md](README.md) - Project overview, installation, and quick start guide
- [CONTRIBUTING.md](CONTRIBUTING.md) - Development workflow, standards, and community guidelines

### Architecture & Design
- [CONCEPT.md](CONCEPT.md) - High-level vision, philosophy, and example workflows
- [ARCHITECTURE.md](ARCHITECTURE.md) - AI-native system design and MCP architecture
- [AGENTIC_ARCHITECTURE.md](AGENTIC_ARCHITECTURE.md) - Detailed skills, agents, and MCP server specifications
- [MUSIC_THEORY.md](MUSIC_THEORY.md) - Music theory foundation, conversational design patterns, and device profiles
- [OBSERVABILITY.md](OBSERVABILITY.md) - .NET Aspire + OpenTelemetry observability strategy
- [SKILLS.md](SKILLS.md) - Complete catalog of all available skills
- [ROADMAP.md](ROADMAP.md) - Implementation roadmap and TODO lists

### Sprint Plans
- [sprints/README.md](sprints/README.md) - Sprint overview and index
- [Sprint 00: Foundation](sprints/sprint_00_foundation.md) - Project setup and Aspire configuration
- [Sprint 01: Theory & MIDI](sprints/sprint_01_theory-and-midi.md) - Music theory library and MIDI service
- [Sprint 02: Core Skills](sprints/sprint_02_core-skills.md) - MVP skills and service facade
- [Sprint 03: CLI & MCP](sprints/sprint_03_cli-and-mcp.md) - CLI tool and MCP server transports
- [Sprint 04: API & SDK](sprints/sprint_04_api-and-sdk.md) - REST API and SDK library
- [Sprint 05: Advanced Skills](sprints/sprint_05_advanced-skills.md) - Advanced skills and agents
- [Sprint 06: Production](sprints/sprint_06_production.md) - Device-specific skills and polish

## Reference Documentation

### Device Profiles
Device profiles are documented in:
- [AGENTIC_ARCHITECTURE.md - Device Abstraction Layer](AGENTIC_ARCHITECTURE.md#device-abstraction-layer)
- [MUSIC_THEORY.md - Device Profile: Polyend Synth](MUSIC_THEORY.md#device-profile-polyend-synth)

**Currently Documented Devices:**
- [Polyend Synth](https://polyend.com/synth/) - Multi-engine polyphonic synthesizer
- [Polyend MESS](https://polyend.com/mess/) - Multi-FX step sequencer pedal
- [Polyend Play+](https://polyend.com/play/) - Sampler and sequencer
- [Moog Mother-32](https://www.moogmusic.com/products/mother-32) - Semi-modular analog synthesizer
- [Moog DFAM](https://www.moogmusic.com/products/dfam-drummer-another-mother) - Analog percussion synthesizer
- [Sonoclast MAFD](https://sonoclast.com/products/mafd/) - MIDI adapter for Moog DFAM

### Skills Documentation
Complete skill catalog documented in:
- [SKILLS.md](SKILLS.md) - All available skills with examples
- [AGENTIC_ARCHITECTURE.md - Layer 1: Skills](AGENTIC_ARCHITECTURE.md#layer-1-skills-discrete-composable-tasks)

**Skill Categories:**
- **Musical Intelligence** - vibe-to-music, chord-progression, voice-leading, scale-selector, harmonic-analysis
- **Device Control** - list-devices, device-selector, send-midi, configure-midi-routing
- **Analysis** - analyze-song, detect-key, detect-tempo, analyze-harmony
- **Generation** - polyrhythm-generator, arpeggio-generator, bass-line-generator, melody-generator, rhythm-generator
- **Transformation** - transpose, invert-chord, quantize, humanize, modal-interchange
- **Session Management** - save-session, load-session, list-sessions, export-midi
- **Utility** - configure-lights, calculate-interval, note-to-frequency, tempo-tap

### Agents Documentation
Agents are documented in:
- [AGENTIC_ARCHITECTURE.md - Layer 2: Agents](AGENTIC_ARCHITECTURE.md#layer-2-agents-autonomous-stateful-goal-oriented)

**Planned Agents:**
- `agent-session-manager` - Maintains musical session state and coherence
- `agent-composition` - High-level compositional decisions and orchestration
- `agent-listener` - Real-time MIDI input analysis and adaptation
- `agent-device-orchestrator` - Multi-device ensemble management

### MCP Servers Documentation
MCP servers are documented in:
- [AGENTIC_ARCHITECTURE.md - Layer 3: MCP Servers](AGENTIC_ARCHITECTURE.md#layer-3-mcp-servers-stateful-services)

**Planned MCP Servers:**
- `sqncr-core` - Main MCP server (generation, session management)
- `sqncr-theory` - Music theory computations
- `sqncr-devices` - Device registry and profiles

## GitHub Templates

### Issue Templates
- [Bug Report](.github/ISSUE_TEMPLATE/bug_report.yml) - Report bugs and issues
- [Feature Request](.github/ISSUE_TEMPLATE/feature_request.yml) - Suggest new features
- [Device Profile Request](.github/ISSUE_TEMPLATE/device_profile.yml) - Request device support

### Pull Request Templates
- [Pull Request Template](.github/pull_request_template.md) - Standard PR checklist

## External Resources

### Music Theory
- [Circle of Fifths](https://en.wikipedia.org/wiki/Circle_of_fifths) - Harmonic relationships
- [Voice Leading](https://en.wikipedia.org/wiki/Voice_leading) - Smooth chord transitions
- [Modal Interchange](https://en.wikipedia.org/wiki/Borrowed_chord) - Borrowing chords from parallel modes

### MIDI Specifications
- [MIDI Association](https://www.midi.org/) - Official MIDI standards
- [MIDI Implementation](https://www.midi.org/specifications) - MIDI protocol specifications

### MCP Protocol
- [Model Context Protocol](https://modelcontextprotocol.io/) - Official MCP documentation
- [MCP SDK (TypeScript)](https://github.com/modelcontextprotocol/typescript-sdk) - TypeScript implementation

### Development Tools
- [.NET 9 SDK](https://dotnet.microsoft.com/download) - .NET platform
- [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) - Distributed application framework
- [OpenTelemetry](https://opentelemetry.io/) - Observability framework
- [Melanchall.DryWetMidi](https://github.com/melanchall/drywetmidi) - .NET MIDI library
- [MCP.NET SDK](https://github.com/modelcontextprotocol/csharp-sdk) - Model Context Protocol for C#
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/) - Data access

## Project Status

### Current Phase: Planning Complete ✅
- ✅ Core documentation complete
- ✅ Agentic architecture defined
- ✅ Device abstraction layer designed
- ✅ Music theory foundation documented
- ✅ Observability strategy defined
- ✅ Sprint plans created
- 🔲 Implementation not started

### Next Steps
**Sprint 00: Foundation & Project Setup** (2 weeks)
1. Initialize .NET 9 solution with Aspire
2. Create all projects (Core, Midi, Theory, State, AppHost)
3. Set up OpenTelemetry instrumentation
4. Verify Aspire Dashboard working
5. Establish development workflow

See [sprints/README.md](sprints/README.md) for complete sprint breakdown.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for:
- How to add new documentation
- Documentation standards and style guide
- Keeping this index up to date

---

**Note:** This index should be updated whenever documentation is created, modified, or removed. See [CONTRIBUTING.md - Documentation Standards](CONTRIBUTING.md#documentation-standards) for details.
