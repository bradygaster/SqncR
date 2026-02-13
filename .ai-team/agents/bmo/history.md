# Project Context

- **Owner:** Brady (bradyg@microsoft.com)
- **Project:** SqncR — AI-native generative music system for MIDI devices. Controls hardware/software synths through natural language conversation via MCP. Ambitions include VCV Rack 2 integration and live streaming music generation.
- **Stack:** C#/.NET, MCP (Model Context Protocol), MIDI, potential VCV Rack 2 integration
- **Created:** 2026-02-13

## Learnings

### 2026-02-13: MIDI Device Discovery & Profile Architecture
- Device profiles should be **data, not code**: model as YAML/JSON with name, channel, patch description, polyphony limit, velocity response curve, CC mappings, latency estimate.
- Minimum viable profile: DeviceId, HardwareName, PatchDescription, Role (bass/pad/lead/texture), MidiChannel, PolyphonyLimit, VelocityProfile (curve + dynamic range).
- VelocityProfile explicitly models the device's response (linear/logarithmic/threshold) because devices vary wildly — a 127 from one device isn't the same as 127 from another.
- Setup interview should be hybrid: auto-detect MIDI outputs, then ask per-device conversational questions (character, polyphony, velocity sensitivity). Save profiles for quick recall.
- **Timing constraint**: 480 TPQ (MIDI standard) at 120 BPM = ~1 ms per tick. Hardware latency (2-10 ms) dominates; don't attempt latency compensation without measuring individual devices.

📌 Team update (2026-02-13): User directive — synth engine scope — decided by Brady
Skip SuperCollider. Support only Sonic Pi and VCV Rack as software synth targets. Each gets a dedicated specialist. Impacts device profile work—profiles now feed into two generation paths.

### 2026-02-13: Real-Time Playback Architecture
- Playback engine should be tick-based, not millisecond-based. Use PeriodicTimer to drive a metronome, increment a global tick counter each cycle.
- All devices sync to one clock; generator produces independent voices per device (no coordination yet, add later).
- Polyphony limiting should drop notes silently (safer than note stealing). Track active notes per device/channel.
- Velocity scaling should respect each device's VelocityProfile — map generated 0-127 to device's actual curve and preferred dynamic range.
- No external clock support initially; SqncR drives timing. Virtual clock sync can be added when VCV Rack 2 integration happens.

### 2026-02-13: .NET MIDI Library Decision
- **Stick with DryWetMidi** (already in use, excellent API, MIDI 2.0 ready, actively maintained, MIT licensed).
- Already using `OutputDevice.GetAll()` for enumeration (good). Add `ControlChangeEvent`, `ProgramChangeEvent`, and `InputDevice` listening for modulation and play-along.
- Device enumeration is static (doesn't auto-refresh if user hot-plugs); require server restart for new devices (OK for now).
- Two separate code paths: SMF playback (SequencePlayer) vs. realtime generation (new engine) — don't confuse them.

📌 Team update (2026-02-13): Two-Path Model Uses Unified Instrument Abstraction — decided by Mal
Hardware and software paths converge on single Instrument data model. MCP tool surface is unified (no branching logic). Generation engine is device-agnostic. MVP starts with Path A (hardware MIDI); Path B (software synth) is a later addition reusing the same generation engine.

📌 Team update (2026-02-13): Synth Engine Integration Paths Research — decided by Inara
Comprehensive research complete on SuperCollider, Sonic Pi, VCV Rack 2, Surge XT, FluidSynth, CSound. Sonic Pi recommended for simplicity (Ruby OSC integration, lowest barrier to entry). SuperCollider recommended for deep synthesis. VCV Rack 2 best for visual + generative. Inara can build minimal working examples once Brady decides which path resonates.

📌 Team update (2026-02-13): Full team recast from Firefly to Adventure Time universe. All 8 Firefly agents retired to _alumni. 10 new Adventure Time agents created (8 role transfers + 2 new roles). All histories transferred. Casting state updated with Adventure Time assignment. Policy updated to include Adventure Time universe (capacity 15). — decided by Brady

📌 Team update (2026-02-14): Hardware MIDI deferred to final milestone — decided by Finn
Your unified Instrument abstraction remains the core architectural pattern. However, hardware MIDI device setup and multi-channel routing move to M4. M0–M3 prove the system works entirely in software (Sonic Pi, VCV Rack paths); M4 integrates hardware via device profiles and conversational setup. The abstraction scales from software-only (M2–M3) to multi-instrument hardware (M4) without design changes.
