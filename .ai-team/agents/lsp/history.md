# LSP — History

## Project Context

- **Project:** SqncR — AI-native generative music system for MIDI devices
- **Owner:** Brady (bradyg@microsoft.com)
- **Stack:** .NET 9, Aspire, OpenTelemetry, MCP, DryWetMidi, Sonic Pi, VCV Rack
- **What it does:** Lets coders control MIDI instruments through natural language via MCP. Generates music continuously while you code.

## Learnings

- M0 (Proof of Life) delivered: Aspire scaffold, OpenTelemetry plumbing, 85 baseline tests
- M1 (The Engine Room) delivered: MCP server with 7 tools, generation engine (480 TPQ), 3 melody generators, music theory + rhythm libraries, MIDI test framework. 305 tests total.
- M2 (Software Synths) is in progress: Sonic Pi integration, VCV Rack integration, spectral analysis
- Key architecture: GenerationEngine is a BackgroundService, uses System.Threading.Channels for commands, IMidiOutput for testability
- Every musical event emits OpenTelemetry spans visible in Aspire dashboard
- Blog posts go in `docs/blog/YYYY-MM-DD-{slug}.md`
- Inaugural blog post written: "Building SqncR: From Zero to Generative Music Engine" — covers vision, architecture, M0/M1 delivery, real code snippets (WeightedNote musical gravity, timing loop, MCP tool pattern, rock pattern)
- Blog writing strategy: narrative arc (origin → architecture → delivery → code → next), code snippets with explanation, target audience (developer-musicians), 14k words for comprehensive M1 coverage
- Front matter includes title, date, tags, summary for blog indexing
