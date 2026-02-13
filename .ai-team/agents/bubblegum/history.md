# Project Context

- **Owner:** Brady (bradyg@microsoft.com)
- **Project:** SqncR — AI-native generative music system for MIDI devices. Controls hardware/software synths through natural language conversation via MCP. Supports VCV Rack 2 patch generation and live streaming music generation.
- **Stack:** C#/.NET, MCP (Model Context Protocol), MIDI, VCV Rack 2
- **Created:** 2026-02-13

## Learnings

### VCV Rack 2 Integration Knowledge (transferred from Inara's research)
- **Patch Format:** VCV Rack 2 patches (.vcv) are Zstandard-compressed tar archives containing `patch.json` (JSON format with modules and cables arrays). Fully deserializable — can generate patches programmatically in C# using System.Text.Json.
- **Programmatic Generation:** No existing .NET library for VCV Rack patch generation. Solution: model patches in C# (Module, Cable, Patch classes), serialize to JSON, compress with tar+zstd, then load in VCV Rack.
- **Built-in Modules:** VCV Rack Free includes oscillators (VCO, Wavetable VCO), filters (VCF), mixers (Mix, VCA Mix), and VCAs out of the box. Minimal viable patch: VCO → VCF → VCA → Audio output.
- **CLI Launch:** VCV Rack can be launched from command line with `Rack <patch.vcv>` and supports `-h` (headless) mode. No native hot-reload — kill/restart with new patch via script.
- **MIDI Input:** VCV Rack accepts MIDI via MIDI-CV module. Requires virtual MIDI port (loopMIDI on Windows, IAC on macOS, ALSA/JACK on Linux). Our .NET app sends MIDI to virtual port; VCV receives and converts to CV.
- **Barrier:** Medium-high complexity — JSON generation, tar/zstd compression, CLI management, virtual MIDI routing. But fully feasible.

📌 Team update (2026-02-13): User directive — synth engine scope — decided by Brady
Skip SuperCollider. Support only Sonic Pi and VCV Rack as software synth targets. River specializes in VCV Rack (patch generation + MIDI routing). Inara specializes in Sonic Pi.

📌 Team update (2026-02-13): Full team recast from Firefly to Adventure Time universe. All 8 Firefly agents retired to _alumni. 10 new Adventure Time agents created (8 role transfers + 2 new roles). All histories transferred. Casting state updated with Adventure Time assignment. Policy updated to include Adventure Time universe (capacity 15). — decided by Brady

📌 Team update (2026-02-13): Roadmap decomposed into 36 work-item GitHub issues — decided by Finn
The v1 roadmap (issue #1) has been decomposed into 36 individual GitHub issues, one per logical work unit. M0 has 2 issues, M1 has 10 issues, M2 has 7 issues, M3 has 8 issues, M4 has 9 issues. Each issue has clear context, acceptance criteria, and agent ownership labels. You have been assigned to M2 issues on Sonic Pi integration. Issue tracking is now granular and actionable.
