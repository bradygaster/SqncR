# SqncR Documentation Index

**Last Updated:** January 29, 2026 (Added comprehensive skills catalog)

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

### Current Phase: Planning & Architecture
- ✅ Core documentation complete
- ✅ Agentic architecture defined
- ✅ Device abstraction layer designed
- ✅ Music theory foundation documented
- ⏳ MCP server implementation (not started)
- ⏳ Skills implementation (not started)
- ⏳ Agents implementation (not started)

### Next Steps
1. Initialize .NET 9 solution with Aspire
2. Create Aspire AppHost project
3. Set up OpenTelemetry instrumentation
4. Implement MIDI service with DryWetMidi
5. Build music theory library with observability
6. Create MCP server with C# SDK
7. Test with Aspire Dashboard

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for:
- How to add new documentation
- Documentation standards and style guide
- Keeping this index up to date

---

**Note:** This index should be updated whenever documentation is created, modified, or removed. See [CONTRIBUTING.md - Documentation Standards](CONTRIBUTING.md#documentation-standards) for details.
