# Project Context

- **Owner:** Brady (bradyg@microsoft.com)
- **Project:** SqncR — AI-native generative music system for MIDI devices. Controls hardware/software synths through natural language conversation via MCP. Supports Sonic Pi and VCV Rack 2 integration and live streaming music generation.
- **Stack:** C#/.NET, MCP (Model Context Protocol), MIDI, Sonic Pi (OSC), VCV Rack 2
- **Created:** 2026-02-13

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

📌 Team update (2026-02-13): Full team recast from Firefly to Adventure Time universe. All 8 Firefly agents retired to _alumni. 10 new Adventure Time agents created (8 role transfers + 2 new roles). All histories transferred. Casting state updated with Adventure Time assignment. Policy updated to include Adventure Time universe (capacity 15). — decided by Brady

📌 Team update (2026-02-14): OpenTelemetry observability is a core requirement from M0 — decided by Finn
SqncR must emit comprehensive OpenTelemetry telemetry for every musical event. Hierarchical span structure: session → section → measure → beat → note. Per-subsystem ActivitySources: SqncR.Generation, SqncR.Playback, SqncR.Sequencer, SqncR.Midi, SqncR.SonicPi, SqncR.VcvRack. Custom metrics: notes per second, active voices, pattern density, generation latency, MIDI send latency. You are responsible for ActivitySource setup, exporter configuration, and Aspire dashboard integration.

📌 Team update (2026-02-14): Three-layer automated audio testing is a v1 requirement — decided by Finn
You are responsible for M4 observability and profiling in collaboration with Peppermint Butler. The testing strategy spans M1 (MIDI validation), M2 (audio capture), M3 (spectral analysis), and M4 (long-running stability). Tests don't just check code—they listen to the music. Custom metrics track generation latency, notes per second, active voices, pattern density, and device-specific send latency visible in the Aspire dashboard.

📌 Team update (2026-02-13): Roadmap decomposed into 36 work-item GitHub issues — decided by Finn
The v1 roadmap (issue #1) has been decomposed into 36 individual GitHub issues, one per logical work unit. M0 has 2 issues, M1 has 10 issues, M2 has 7 issues, M3 has 8 issues, M4 has 9 issues. Each issue has clear context, acceptance criteria, and agent ownership labels. You have been assigned to M0 and M4 issues on observability infrastructure and profiling. Issue tracking is now granular and actionable.

📌 M0 OpenTelemetry plumbing implemented (issue #3)
- `SqncR.Midi/MidiService.cs` has `ActivitySource("SqncR.Midi")` wrapping SendNoteOn, SendNoteOff, AllNotesOff
- `SqncR.Core/SequencePlayer.cs` has `ActivitySource("SqncR.Playback")` wrapping PlayAsync (parent span) and each note event (child spans)
- Library projects use only `System.Diagnostics` — no OTel SDK dependency in libraries
- CLI project (`SqncR.Cli`) has OpenTelemetry SDK packages: `OpenTelemetry`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Extensions.Hosting` (all 1.11.2)
- CLI `Program.cs` creates `TracerProvider` with OTLP exporter pointed at `OTEL_EXPORTER_OTLP_ENDPOINT` env var (default `http://localhost:4317`)
- CLI emits a `cli.startup` test trace on every invocation for pipeline verification
- Pattern: ActivitySources are `internal static readonly` fields on the class that owns the operations. Tags follow `{subsystem}.{attribute}` naming (e.g., `midi.channel`, `note.name`)
- The `TracerProvider` is explicitly disposed before CLI exit to flush pending spans
