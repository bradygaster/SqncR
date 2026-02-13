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

📌 Team update (2026-02-15): M0 session complete — OTel instrumentation pattern documented — M0 proof-of-life logged
M0 milestone complete. Your OTel ActivitySources in MidiService + SequencePlayer + OTLP exporter in CLI provides foundation for M1 telemetry expansion. Build is clean (0 warnings), 85 tests passing (up from 13). Pattern established: libraries use only System.Diagnostics, CLI is composition root for SDK packages. Session logged to `.ai-team/log/2026-02-13-m0-proof-of-life.md`.

📌 Issue #18: Software synth telemetry implemented
Created `SonicPiMetrics.cs` (Meter "SqncR.SonicPi") with OscMessagesSent counter, OscLatency histogram (µs), CodeGenerationTime histogram (µs), ActiveInstruments UpDownCounter. Created `VcvRackMetrics.cs` (Meter "SqncR.VcvRack") with PatchesGenerated counter, PatchGenerationTime histogram (ms), LaunchTime histogram (ms), IsRunning ObservableGauge. Enriched OscClient spans with osc.port, osc.endpoint, osc.message_size_bytes tags + OscLatency recording. Enriched VcvRackLauncher with vcvrack.patch_path, vcvrack.headless, vcvrack.process_id tags + LaunchTime recording. Added ActivitySource spans to RubyCodeGenerator (sonicpi.synth_name, sonicpi.fx_count, sonicpi.note_count) + CodeGenerationTime recording. Added spans to PatchBuilder.Build() and PatchTemplates (vcvrack.template, vcvrack.module_count, vcvrack.cable_count) + PatchGenerationTime/PatchesGenerated recording. Updated McpServer Program.cs to register SonicPi + VcvRack meters. Created 8 SonicPiMetricsTests + 8 VcvRackMetricsTests. Build: 0 errors, 0 warnings. Tests: 397 passing.

📌 Issue #26: Session-level telemetry implemented
Created `SessionTelemetry.cs` (ActivitySource "SqncR.Session", Meter "SqncR.Session") with session root span (StartSession/EndSession with session.id, session.start_time, initial.tempo, initial.scale tags), variety decision child spans (TraceVarietyDecision with variety.behavior, variety.detail, variety.measure_number tags), health snapshot child spans (RecordHealthSnapshot with health.tick_latency_avg_ms, health.missed_ticks, health.memory_mb, health.active_notes, health.uptime_seconds tags), and 4 metrics instruments: session.duration_seconds (ObservableGauge), session.total_notes (Counter), session.variety_changes (Counter), session.health_snapshots (Counter). Integrated into GenerationEngine: StartSession on play start, EndSession on stop, TraceVarietyDecision on variety engine mutations (octave drift, velocity drift, rest insertion), RecordHealthSnapshot every 40 measures (~5 min at 120 BPM), RecordNote on every drum/melody note emission. Updated McpServer Program.cs with "SqncR.Session" ActivitySource and Meter registration. Created 11 SessionTelemetryTests covering session lifecycle, tags, child spans, metric counters, and observable gauge. Build: 0 errors, 0 warnings. Tests: 454 passing.
