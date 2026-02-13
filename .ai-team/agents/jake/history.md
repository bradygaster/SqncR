# Project Context

- **Owner:** Brady (bradyg@microsoft.com)
- **Project:** SqncR — AI-native generative music system for MIDI devices. Controls hardware/software synths through natural language conversation via MCP. Ambitions include VCV Rack 2 integration and live streaming music generation.
- **Stack:** C#/.NET, MCP (Model Context Protocol), MIDI, potential VCV Rack 2 integration
- **Created:** 2026-02-13

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

📌 Team update (2026-02-13): User directive — synth engine scope — decided by Brady
Skip SuperCollider. Support only Sonic Pi and VCV Rack as software synth targets. Inara specializes in Sonic Pi (Ruby OSC integration). River specializes in VCV Rack (patch generation + MIDI routing).

📌 Team update (2026-02-13): Full team recast from Firefly to Adventure Time universe. All 8 Firefly agents retired to _alumni. 10 new Adventure Time agents created (8 role transfers + 2 new roles). All histories transferred. Casting state updated with Adventure Time assignment. Policy updated to include Adventure Time universe (capacity 15). — decided by Brady

📌 Team update (2026-02-14): OpenTelemetry observability is a core requirement from M0 — decided by Finn
SqncR must emit comprehensive OpenTelemetry telemetry for every musical event, starting in M0 and deepening through M1–M4. Hierarchical span structure: session → section → measure → beat → note. Per-subsystem ActivitySources: SqncR.Generation, SqncR.Playback, SqncR.Sequencer, SqncR.Midi, SqncR.SonicPi, SqncR.VcvRack. Custom metrics: notes per second, active voices, pattern density, generation latency, MIDI send latency. You (Jake) are responsible for span structure and metrics in the MCP server and generation loop.

📌 Team update (2026-02-13): Roadmap decomposed into 36 work-item GitHub issues — decided by Finn
The v1 roadmap (issue #1) has been decomposed into 36 individual GitHub issues, one per logical work unit. M0 has 2 issues, M1 has 10 issues, M2 has 7 issues, M3 has 8 issues, M4 has 9 issues. Each issue has clear context, acceptance criteria, and agent ownership labels. You have been assigned to M1 issues on the generation loop and MCP server scaffold. Issue tracking is now granular and actionable.
