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

### VCV Rack Integration Implementation (Issue #16)
- **SqncR.VcvRack project:** Built the full VCV Rack integration layer with patch model (VcvModule, VcvCable, VcvPatch), fluent PatchBuilder API, ModuleLibrary (8 VCV Rack Free modules), PatchTemplates (BasicSynth, AmbientPad, BassSynth), and VcvRackLauncher (process management with ActivitySource tracing).
- **JSON serialization:** Used `System.Text.Json.Nodes` (JsonObject/JsonArray) instead of source-generated `JsonSerializerContext` — source generation cannot handle `Dictionary<string, object>` with polymorphic values in nested structures. JsonNode API works cleanly for building VCV Rack's expected JSON format.
- **Zstd compression:** Used `ZstdSharp.Port` NuGet + `System.Formats.Tar` for tar+zstd .vcv file generation. Falls back to raw JSON on compression failure.
- **Port naming:** ModuleLibrary defines friendly port names (e.g. "V/Oct", "Gate", "Saw") mapped to VCV Rack port indices, enabling the fluent Cable API to resolve connections by name.
- **loopMIDI:** VcvRackLauncher has configurable `MidiPortName` property (default "loopMIDI Port") for virtual MIDI port awareness on Windows.
- **Tests:** 21 tests (13 PatchBuilder + 8 Serialization) — all passing. 381 total tests across solution, 0 failures.

### MCP Tool Integration (Issue #17)
- **VcvRackTool.cs:** Created 5 MCP tools — `generate_patch`, `launch_vcv_rack`, `stop_vcv_rack`, `vcv_rack_status`, `list_templates`. Follows established `[McpServerToolType]` static class pattern with `ActivitySource` tracing and `[Description]` attributes for AI discoverability.
- **DI Registration:** Added `VcvRackLauncher` as singleton in `Program.cs`, added `SqncR.VcvRack` ActivitySource to OTel tracing config, added project reference from McpServer → VcvRack.
- **generate_patch flow:** Template name → `PatchTemplates` factory → `VcvPatch.SaveAs()` → returns file path + module list. Defaults to temp directory if no output path specified.
- **Error handling:** All tools return user-friendly error strings (not exceptions) matching the established pattern in `GenerationTool.cs`.
- **Build:** 0 errors, 0 warnings. All 381 tests pass (1 pre-existing timing flake in `TempoChange_MidPlay_NotesComeFaster`).

📌 Team update (2026-02-14): VCV Rack Patch Serialization Uses JsonNode API — decided by Bubblegum
Your VcvPatch.ToJson() uses System.Text.Json.Nodes (JsonObject/JsonArray) for building VCV Rack patch JSON. Source-generated JsonSerializerContext cannot handle Dictionary<string, object> with polymorphic nested values. JsonNode API avoids reflection complexity, gives full control over output structure, and is fully AOT/trimming-compatible. Other team members working with VCV Rack patches should use the same approach. Port names in ModuleLibrary are friendly strings mapped to integer port indices—use PatchBuilder.Cable() for name-based wiring.
