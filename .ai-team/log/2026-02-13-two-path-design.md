# Session: Two-Path Design & Team Expansion

**Date:** 2026-02-13  
**Requested by:** Brady  

## Participants

- **Mal** — Architect: Two-path unified abstraction design
- **Wash** — Hardware MIDI specialist: Device profiles, DryWetMidi, tick-based playback
- **Inara** — Synth/VCV Dev: Synth engine research (VCV Rack 2, SuperCollider, Sonic Pi, Surge XT, FluidSynth)
- **Zoe** — Rhythm/Beats (new team member)
- **Book** — Music Theorist (new team member)

## What Happened

### Two-Path Architecture Overview
Brady described the core vision: SqncR should support two paths for music generation.
- **Path A (Hardware):** User tells the system which MIDI devices are on which channels, describes their patches/sounds. System learns the setup and plays generatively.
- **Path B (Software):** User specifies VCV Rack or another software synth. System generates a patch, loads it, and plays generatively.

### Mal's Architectural Decision
Proposed **unified Instrument abstraction** as the convergence point.
- Both Path A and Path B populate the same `Instrument` data model.
- Single MCP tool surface (no branching logic).
- Generation engine is device-agnostic.
- MVP: Start with Path A (hardware MIDI). Path B is a later addition.

### Wash's Hardware MIDI Deep Dive
Analyzed device connectivity, MIDI profiles, and playback strategy.
- **Device Discovery:** DryWetMidi for scanning connected MIDI devices.
- **Profiles as Data:** Device profiles stored as YAML/JSON (not hard-coded logic).
  - Each profile captures velocity curves, polyphony limits, CC mappings, latency.
  - Generator queries profiles at runtime; supports diverse hardware.
- **Playback Model:** Tick-based (continuous background loop, ~100ms ticks).
  - Non-blocking modification queue (live parameter changes).
  - Respects hardware constraints per-tick.

### Inara's Synth Engine Research
Evaluated six synth engines and VCV Rack 2 integration for Path B.
- **SuperCollider:** OSC-driven, excellent for ambient synthesis.
- **Sonic Pi:** Ruby live-coding, lowest barrier to entry, recommended for MVP.
- **VCV Rack 2:** Visual + generative, harder to generate patches (tar/zstd + JSON).
- **Surge XT, FluidSynth, CSound:** Evaluated; ranked by ease of integration.
- **Recommendation:** Sonic Pi or SuperCollider as Tier 1 (fully headless, fully programmable).

### Team Expansion
- **Zoe** (Rhythm/Beats): Joins to handle rhythmic patterns and beat generation.
- **Book** (Music Theorist): Joins to guide harmonic content and theory-informed generation.

## Key Decisions Made

1. **Unified Instrument abstraction** (Mal): Single data model for hardware and software sources.
2. **Data-driven device profiles** (Wash): YAML/JSON profiles, not hard-coded device logic.
3. **Synth engine evaluation complete** (Inara): Sonic Pi recommended for MVP (Ruby OSC integration).
4. **MVP Scope:** Start with Path A (hardware MIDI) + basic algorithmic generation.

## Facts

- DryWetMidi supports device discovery and standard MIDI I/O on Windows.
- Device profiles will live in `~/.sqncr/devices/{device_id}.yaml`.
- Tick-based generation loop runs in background; modification queue is non-blocking.
- Sonic Pi (Ruby) is lowest barrier to entry for software synth path.
- SuperCollider (OSC) is deeper alternative for ambient/generative synthesis.
