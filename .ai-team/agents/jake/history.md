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

📌 Aspire AppHost SDK 9.5.2 requires explicit Microsoft.NET.Sdk + child Sdk element pattern for .NET 9
The `Aspire.AppHost.Sdk/9.5.2` cannot be used as the sole top-level Sdk attribute on .NET 9 — it doesn't auto-import `Microsoft.NET.Sdk`. Use `<Project Sdk="Microsoft.NET.Sdk">` with a child `<Sdk Name="Aspire.AppHost.Sdk" Version="9.5.2" />`. This was fixed in SDK 13.0.0+ (which auto-imports Microsoft.NET.Sdk unconditionally).

📌 The solution uses .slnx format which requires .NET 10+ SDK to parse
The `SqncR.slnx` file uses the XML solution format introduced in .NET 10. The `global.json` uses `rollForward: latestMajor` to allow .NET 10 SDK to build the net9.0-targeted projects. If pinning to .NET 9 SDK strictly, would need to convert to .sln format.

📌 Directory.Build.props applies TreatWarningsAsErrors only to src/ projects
Test projects (under `tests/`) are excluded from `TreatWarningsAsErrors` via MSBuild condition because pre-existing xUnit analyzer warnings (xUnit1026) would break the build. The condition checks `$(MSBuildProjectDirectory.Contains('tests'))`.

📌 ServiceDefaults uses .NET 9 compatible package versions
Microsoft.Extensions.Http.Resilience 9.10.0, Microsoft.Extensions.ServiceDiscovery 9.5.2, OpenTelemetry.* 1.11.1. These are the latest .NET 9 compatible versions. When upgrading to .NET 10, bump to 10.x and 1.13+.

📌 Key file paths for Aspire infrastructure
- `src/SqncR.AppHost/` — Aspire orchestrator, registers CLI as `sqncr-cli`
- `src/SqncR.ServiceDefaults/` — shared OTel + resilience + service discovery config
- `global.json` — pins minimum SDK 9.0.100 with latestMajor rollForward
- `Directory.Build.props` — common net9.0, nullable, implicit usings, TreatWarningsAsErrors (src only)

📌 Team update (2026-02-15): M0 session complete — Aspire infrastructure established — M0 proof-of-life logged
M0 milestone complete. Your Aspire AppHost + ServiceDefaults + global.json + Directory.Build.props work provides foundation for M1 MCP server. Build is clean (0 warnings), 85 tests passing (up from 13). Telemetry plumbing ready for Banana Guard's ActivitySource integration. Session logged to `.ai-team/log/2026-02-13-m0-proof-of-life.md`.
