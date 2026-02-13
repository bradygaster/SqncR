# Decisions

> Shared brain for the team. All agents read this. Only Scribe writes (by merging inbox).

<!-- Decisions are appended below. Each starts with ### heading. -->

### 2026-02-13: Brady's original vision for SqncR
**By:** Brady (via Copilot)
**What:** Brady described SqncR as an experimental musical idea for coders who are also MIDI musicians. He wants it to be a way for coders with hardware or software MIDI instruments to hook them up and let the system play generative tunes. Ambitions include building VCV Rack patches that it then plays generatively, and generating live music for streams. He noted that a previous Claude approach was too conservative and wants "juice, new life, new ideas." The emphasis is on iteration and exploration before coding.
**Why:** User request — captured as the founding vision for the project. All team members should understand this as the creative direction.

### 2026-02-13: Two-path UX model — Hardware vs Software synth setup
**By:** Brady (via Copilot)
**What:** SqncR should support two paths: (A) Hardware path — user tells the system which MIDI devices are on which channels, describes their patches/sounds, system learns the setup and plays generatively. (B) Software path — user says they have VCV Rack or another software synth, system generates a patch, loads it, and plays generatively. Either path can be instrument-led (start from what you have) or idea-led (start from what you want to hear). The system plays continuously so the user can keep coding.
**Why:** User request — core UX direction for the product. All agents should understand this two-path model.

### 2026-02-13: Two-Path Model Uses Unified Instrument Abstraction
**By:** Mal
**Status:** Architectural Direction
**What:** The hardware MIDI path (Path A) and software synth generation path (Path B) converge at a single Instrument abstraction. They are not separate pipelines; they are two different ways to populate the same data model. Once an Instrument exists (whether from hardware discovery or patch generation), the generation engine treats it identically.

**Canonical Instrument Interface:**
```
Instrument {
  id: string
  type: 'hardware' | 'software'
  source: HardwareDevice | GeneratedVCVPatch
  capabilities: {
    pitchRange: [note_min, note_max]
    voiceCount: int
    timbre: string[]
    latency: ms
  }
  midiChannel: int
}
```

**MCP Tool Surface (Unified):**
- `list_devices()` — scan hardware
- `setup_hardware_instrument()` — register hardware
- `generate_synth_patch()` — create software instrument
- `list_instruments()` — what's configured?
- `start_generation()` — begin playback loop (works with any instrument)
- `modify_generation()` — change parameters mid-play
- `stop_generation()`

**Generation Loop Model:** Runs continuously in background, reads activeInstruments and generationState, generates note(s) each tick (~100ms), sends MIDI/OSC to outputs, responsive to modification queue (non-blocking).

**MVP Implementation Order:** Start with Path A (hardware MIDI). Path B (VCV generation) is a later addition that reuses the same generation engine.

**Rationale:**
1. **Simplicity**: One abstraction is simpler than branching logic.
2. **Reusability**: Generation engine works for any Instrument source.
3. **Iteration**: Path A → proven concept → Path B → richer feature set.
4. **Maintainability**: When adding a 3rd path, it's just another way to create an Instrument.
**Why:** Architectural decision that unifies two input paths into one coherent system. Simplifies implementation and future extension.

### 2026-02-13: Device Profile as Data-Driven Architecture
**By:** Wash
**Status:** Proposed
**What:** Device profiles should be data structures (YAML/JSON), not code. The generator queries profiles at runtime to respect hardware constraints, not the other way around.

**Profile Structure (Minimal Viable):**
```yaml
device_id: moog-sub-37-1
hardware_name: Moog Sub 37
patch_description: "Warm sub-bass, filter LFO"
role: bass  # bass | pad | lead | texture | percussion

# MIDI Connectivity
midi_channel: 1
polyphony_limit: 1  # -1 = unlimited

# Velocity Response
velocity_profile:
  curve: logarithmic  # linear | logarithmic | threshold
  min_velocity: 10
  max_velocity: 127
  preferred_dynamic_range: 50

# Optional: Parameter Modulation
cc_mappings:
  filter_cutoff: 74
  filter_resonance: 71
  lfo_rate: 76

# Estimates (user provides in setup interview)
approximate_latency_ms: 2
lowest_note: 27      # A0
highest_note: 88     # E7
supports_aftertouch: false
supports_velocity: true
sustain_pedal_on_cc64: false
```

**Where Profiles Live:** Persisted at `~/.sqncr/devices/{device_id}.yaml`. Loaded into memory at runtime, queried by generation engine. User management via MCP tool.

**Benefits:**
1. Generator is device-agnostic (doesn't hard-code device logic).
2. Easy to add devices (no code changes needed).
3. Extensible for future features (add fields; generator respects them automatically).
4. Testable (mock profiles in unit tests).
5. Respects user diversity (each setup is unique).

**Polyphony & Velocity Response:** Profiles capture per-device velocity curves and polyphony limits. Without profiles, generator treats all devices identically. With profiles, generator respects each device's curve → consistent, natural sounding across setup.

**Why:** Separates generation logic from hardware details. Users can add new devices without code changes. Profiles are discoverable and editable by users.

### 2026-02-13: Synth Engine Integration Paths Research
**By:** Inara (Synth/VCV Dev)
**Status:** Research complete. Awaiting Brady's direction.

**Executive Summary:** No single "perfect" synth engine exists. The right path depends on Brady's priority:
1. **Lowest barrier to entry?** → Sonic Pi (Ruby live-coding + OSC)
2. **Best "generate and hear" workflow?** → SuperCollider (OSC-driven synthesis)
3. **Most visual/traditional?** → VCV Rack 2 (if Brady wants to see/edit patches)
4. **Simplest for initial MVP?** → FluidSynth + MIDI (but creatively limited)
5. **Best integration with existing tools?** → Surge XT CLI + virtual MIDI

**Top Recommendations:**

**Sonic Pi** ⭐ Recommended for Simplicity
- **Headless:** Yes, with caveats (needs keep-alive OSC messages)
- **Patch Format:** Ruby code (live-coding, text-based)
- **Programmatic Gen:** Excellent (template Ruby code)
- **MIDI/OSC:** Both supported; OSC is listening
- **.NET Integration:** Send OSC messages to port 4560
- **Ambient Capability:** Excellent; designed for live coding + evolution

**SuperCollider** ⭐ Recommended for Deep Integration
- **Headless:** Yes, fully
- **Patch Format:** SynthDef text (human-readable)
- **Programmatic Gen:** Excellent (write SynthDef code templates)
- **MIDI/OSC:** Both supported; OSC is primary control method
- **.NET Integration:** Send OSC messages to port 57110 (scsynth)
- **Ambient Capability:** Excellent for evolving, generative drones

**VCV Rack 2** ⭐ Best for Visual + Generative
- **Headless:** Yes, with `-h` flag
- **Patch Format:** .vcv (Zstd tar archive containing JSON)
- **Programmatic Gen:** Hard (requires tar/zstd compression in .NET, but JSON is doable)
- **MIDI/OSC:** MIDI only (via MIDI-CV module + virtual port)
- **.NET Integration:** Generate JSON, compress, launch `Rack patch.vcv`, send MIDI via loopMIDI
- **Ambient Capability:** Excellent; can build evolving modular patches

**Other Engines:** Surge XT (traditional synth, medium integration), FluidSynth (simple but limited), CSound (powerful but steep learning curve).

**Tier 1 (Start Here):** SuperCollider OR Sonic Pi — fully headless, fully programmable, excellent ambient capability.  
**Tier 2 (Visual Feedback):** VCV Rack 2 + MIDI — if Brady wants to see the patches while they play.  
**Tier 3 (Traditional Workflow):** Surge XT CLI + virtual MIDI — if Brady prefers traditional synth interface.  
**Not Recommended:** FluidSynth (too limited), CSound (too steep).

**Next Steps:** Brady decides which engine resonates. Inara can build a minimal working example (e.g., SuperCollider → drone, or Sonic Pi → evolving pad).

**Why:** Comprehensive research enabling Path B (software synth) implementation. Provides Brady with informed choices and roadmap for synth engine integration.
