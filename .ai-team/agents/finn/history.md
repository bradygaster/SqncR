# Project Context

- **Owner:** Brady (bradyg@microsoft.com)
- **Project:** SqncR — AI-native generative music system for MIDI devices. Controls hardware/software synths through natural language conversation via MCP. Ambitions include VCV Rack 2 integration and live streaming music generation.
- **Stack:** C#/.NET, MCP (Model Context Protocol), MIDI, potential VCV Rack 2 integration
- **Created:** 2026-02-13

## Learnings

### 2026-02-14: Two-path model converges on unified Instrument abstraction
Brady presented two potential UX flows: (A) hardware MIDI discovery and setup, (B) software synth patch generation. Analysis shows these aren't separate pipelines—they're two ways to populate the same data model. Both end with an `Instrument` object (hardware or software) feeding into a unified generation loop. This simplifies architecture dramatically: one set of MCP tools, one generation engine, differentiation only at setup time. MVP should start with Path A (hardware MIDI) as it's simpler and proves the core concept before adding Path B complexity.

### 2026-02-14: Generation loop should be background async, non-blocking
The "continuously playing" feature requires a background service loop in the MCP server (ticking every ~100ms), not a blocking call. Modifications come through a queue and are read by the loop—user can keep coding while music plays. This is critical for the UX: "play generatively while I code" means the server stays responsive to new commands.

📌 Team update (2026-02-13): User directive — synth engine scope — decided by Brady
Skip SuperCollider. Support only Sonic Pi and VCV Rack as software synth targets. Inara specializes in Sonic Pi. River specializes in VCV Rack. Clarifies Path B implementation focus.

📌 Team update (2026-02-13): Device Profile as Data-Driven Architecture — decided by Wash
Profiles should be YAML/JSON structures (not hard-coded logic). Generator queries profiles at runtime to respect hardware constraints. Profile structure includes device ID, MIDI channel, polyphony limit, velocity response curve, CC mappings, latency estimate. Profiles live at `~/.sqncr/devices/{device_id}.yaml`. This enables device-agnostic generation engine and makes adding new devices trivial—no code changes needed.

📌 Team update (2026-02-13): Two-Path Model Uses Unified Instrument Abstraction — decided by Mal
Hardware and software paths converge on single Instrument data model. MCP tool surface is unified (no branching logic). Generation engine is device-agnostic. MVP starts with Path A (hardware MIDI); Path B (software synth) is a later addition reusing the same generation engine.

📌 Team update (2026-02-13): Full team recast from Firefly to Adventure Time universe. All 8 Firefly agents retired to _alumni. 10 new Adventure Time agents created (8 role transfers + 2 new roles). All histories transferred. Casting state updated with Adventure Time assignment. Policy updated to include Adventure Time universe (capacity 15). — decided by Brady
