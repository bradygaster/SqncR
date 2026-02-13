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

### 2026-02-13: User directive — synth engine scope
**By:** Brady (via Copilot)
**What:** Skip SuperCollider. Support only Sonic Pi and VCV Rack as software synth targets. Each gets a dedicated specialist.
**Why:** User request — captured for team memory

### 2026-02-13: Claude is permanently out of scope
**By:** Brady (via Copilot)
**What:** Claude Desktop / Claude as an AI assistant is no longer part of the SqncR vision. All references to Claude as the conversational interface should be removed or replaced with generic MCP-compatible AI assistant references (GitHub Copilot, or any MCP client).
**Why:** User request — captured for team memory

### 2026-02-13: Hardware MIDI integration deferred to end
**By:** Brady (via Copilot)
**What:** Hardware MIDI device integration moves to the final milestone. Software synths (Sonic Pi, VCV Rack) come first — prove the music-making works entirely in software before adding hardware complexity.
**Why:** User request — "we have the software synths let's make sure all the music making is happening before we do the hardware part"

### 2026-02-13: Every musical event must have telemetry
**By:** Brady (via Copilot)
**What:** Every user-facing musical event — note played, chord changed, pattern triggered, loop started/stopped, tempo changed, fill inserted, transition fired — must emit OpenTelemetry spans/activities. The Aspire dashboard should show the "progress" of a song being made, played, and looped in real-time. This is not optional observability bolted on later — it's a core design requirement from M1 onward.
**Why:** User request — "one should be able to look in the aspire dashboard and see the progress of a song being made/played/looped"

### 2026-02-14: OpenTelemetry observability is a core requirement from M0
**By:** Finn (Lead)
**What:** SqncR must emit comprehensive OpenTelemetry telemetry for every musical event, starting in M0 and deepening through M1–M4. Every note on/off, chord change, pattern trigger, tempo shift, transition, and section change generates a span. Hierarchical span structure: session → section → measure → beat → note. Per-subsystem ActivitySources: `SqncR.Generation`, `SqncR.Playback`, `SqncR.Sequencer`, `SqncR.Midi`, `SqncR.SonicPi`, `SqncR.VcvRack`. Custom metrics: notes per second, active voices, pattern density, generation latency, MIDI send latency. The .NET Aspire dashboard is the "control room"—users can watch in real-time as their music is being generated, played, and evolved.

**Why:** 
1. **Visibility as a product feature.** Brady explicitly requested: "each user event like changing notes or playing should have telemetry events associated with them and spans/activities. one should be able to look in the aspire dashboard and see the progress of a song being made/played/looped." This isn't debugging instrumentation; it's user-facing transparency.
2. **Debugging complex async pipelines.** When something goes wrong in a multi-hour generative session, the Aspire dashboard provides the narrative: which notes were generated, when, by which decision logic, what MIDI events fired, which devices received them, and what the audio output was. Spans are the "black box recorder" for the music generation system.
3. **Performance visibility.** Custom metrics (latency histograms, throughput counters, device-specific send times) make bottlenecks obvious. Is generation slow? Is MIDI send to device sluggish? Are we hitting polyphony limits? The dashboard answers these instantly.
4. **Streaming integration.** In M3, Brady wants to broadcast the Aspire dashboard on a second screen during live Twitch streams. Audience sees the music being made in real-time. This is a differentiator.
5. **Architectural clarity.** Spans force us to think about the flow: where does a user request become a generation decision? When does that decision become MIDI? When does MIDI reach the device? When does audio appear? This clarity improves the entire system design.
6. **Test correlation.** In M1–M4, automated tests can pull span data to understand what the system *intended* to do (from spans) and correlate it with what it *actually* did (from audio capture or MIDI intercept). This is more powerful than assertions alone.

**Implementation Roadmap:**
- **M0:** Add `System.Diagnostics.ActivitySource` setup in CLI. Register with Aspire dashboard. Verify traces appear (no spans yet, just plumbing).
- **M1:** Generation loop emits hierarchical spans (session → section → measure → beat → note). Core metrics (notes/sec, active voices). Jake, Banana Guard own this.
- **M2:** Software synth traces (see Ruby being sent to Sonic Pi, see patch loading into VCV Rack). Extend ActivitySources (`SqncR.SonicPi`, `SqncR.VcvRack`).
- **M3:** Session-level traces spanning hours. Variety engine decisions traced. Stability snapshots. Aspire dashboard becomes the live stream overlay.
- **M4:** Hardware device-specific telemetry. Multi-channel trace correlation. Each device is a "fader" with latency, polyphony utilization, role balance.

**Who Needs to Know:**
- Jake (MCP server, generation loop) — responsible for span structure and metrics
- Banana Guard (observability specialist) — responsible for ActivitySource setup, exporter configuration, dashboard integration
- All milestone leads — each milestone advances observability alongside features

### 2026-02-14: Rhythm and drum sequencing is first-class in v1
**By:** Finn
**What:** Rhythm and drum sequencing work is now explicit and first-class in the v1 roadmap. M1 includes foundational beat patterns (four-on-the-floor, half-time), step sequencer abstraction, and swing/shuffle parameters. M2 includes advanced rhythm features: per-instrument drum maps (note-to-sound mappings), velocity accent patterns, fills and transitions, polyrhythm support (3-over-4, 5-over-4, etc.), pattern library (reusable beat templates), and rhythmic theory collaboration between Rainicorn (rhythm generation, syncopation) and Simon (time signatures, harmonic alignment).
**Why:** Brady explicitly requested that rhythm sequencing not be left implicit in Rainicorn's domain, but treated as a primary feature with dedicated work items and ownership clarity. The roadmap now reflects his vision for "all sorts of opportunities" in rhythm design—from basic patterns to sophisticated polyrhythmic structures. This makes rhythm work visible, scoped, and collaborative rather than emergent.

### 2026-02-14: Three-layer automated audio testing is a v1 requirement
**By:** Finn (on behalf of Brady)
**What:** SqncR will use a three-layer automated audio testing strategy integrated across M1–M4:

1. **MIDI Validation (M1 — Deterministic Foundation)**
   - Test harness intercepts MIDI messages before they hit devices
   - Deterministic assertions on notes, velocities, durations, channel assignments
   - No hardware required; runs in CI/CD
   - Owner: Lemongrab (Tester)

2. **Audio Capture & Signal Chain Validation (M2 — Integration)**
   - Audio loopback capture to validate Sonic Pi / VCV Rack / hardware synths are producing signal
   - End-to-end routing validation: confirms multi-channel MIDI flows correctly through device profiles
   - Proves the signal chain is not silent
   - Owner: Peppermint Butler (Audio/MIDI Test Engineer)

3. **Spectral Analysis (M3 — Fuzzy/Soft Validation)**
   - FFT-based frequency detection to verify the right notes are sounding
   - Example: "I sent MIDI note C4 (261.6 Hz), does the audio spectrum show peak energy near 261.6 Hz?"
   - Fuzzy matching with configurable tolerance (±5% default)
   - Soft validation: "is there a sound?" and "is it the right pitch?"
   - Owner: Peppermint Butler (Audio/MIDI Test Engineer)

4. **Long-Running Stability & Canary Tests (M4 — Reliability)**
   - Multi-hour session audio monitoring with spectral snapshots every 30 seconds
   - "Still playing and sounds reasonable" canary tests
   - Graceful failure recovery validation (e.g., MIDI device disconnect mid-session)
   - Memory and CPU profiling to detect leaks
   - Owners: Peppermint Butler (Audio/MIDI Test Engineer) + Banana Guard (Observability)

**Testing Philosophy:** "Tests don't just check code — they listen to the music." This is not a testing footnote—it's a core feature of SqncR. The product is about generating music that sounds right; the tests prove it does.

**Why:** Brady explicitly requested: *"i want the testing to be automated as the coding happens and tests run. i want the tester to be able to know what it expects to hear and to be able to validate it heard the right thing."* A purely unit-test-based approach would miss the core value: SqncR's job is to **make music that sounds good**. Unit tests on note generation logic don't prove the synth is actually playing. MIDI message capture doesn't prove the audio isn't silent. Spectral analysis doesn't exist in most music software testing, but it's the only way to validate "is the right pitch sounding?" This three-layer strategy ensures confidence at every level: foundation (MIDI tests), signal chain (audio capture), product (spectral analysis), and reliability (long-running tests). By M4, we have a product that doesn't just *claim* to work—it has proven, automated evidence that it sounds good and stays stable. This is especially important for streaming use cases where failure is visible and immediate.

**Impact on Roadmap:** Issue #1 has been updated to include testing items in all four milestones. Agent assignments are:
- Lemongrab: M1 MIDI testing framework
- Peppermint Butler: M2 audio integration + M3 spectral analysis + M4 long-running tests
- Banana Guard: M4 observability and profiling (collaborating with Peppermint Butler)

### 2026-02-14: Hardware MIDI deferred to final milestone
**By:** Finn (Lead)
**What:** The v1 roadmap has been restructured to defer hardware MIDI device setup, device profiles, and multi-channel routing to the final milestone (M4). Software synthesis via Sonic Pi and VCV Rack is now M2, and streaming-ready stability work is M3. The new arc is:
- M0: Proof of Life (foundation, mostly done)
- M1: The Engine Room (MCP server + generation loop + music theory + foundational rhythm)
- M2: Software Synths (Sonic Pi OSC + VCV Rack patch generation, no hardware dependencies)
- M3: Stream-Ready (session persistence, smooth transitions, stability, long-running testing)
- M4: Know Your Gear (device profiles, conversational setup, multi-channel routing)

**Why:** Prove the *core generation engine works* entirely in software before adding hardware complexity. The fastest path to validation is: get the MCP server talking → prove music generation works with pure software → add multi-instrument hardware complexity last. Hardware integration (device profiles, MIDI routing, conversational setup) is valuable but should be treated as a final integration layer, not the foundation. This ordering also simplifies testing: M1–M3 can be validated without hardware; M4 adds hardware-specific test fixtures. It also reflects Brady's priority of proving the music is interesting and stable before worrying about device constraints.

**Impact:** Issue #1 updated with reordered milestones. All existing content preserved; testing strategy and rhythm work carry forward. Team can now focus M1–M3 efforts on software validation, then integrate hardware in the final push.

### 2026-02-14: v1 roadmap structure — 5 milestones, MCP-first
**By:** Finn
**What:** The v1 roadmap is structured as 5 milestones (M0–M4). M0 validates what exists. M1 builds the MCP server and generation loop (the critical path). M2 adds software synthesis (Sonic Pi/VCV Rack). M3 adds streaming-quality features (session persistence, stability). M4 adds hardware integration and polish. Each milestone produces a usable, demoable system.
**Why:** Brady wants something compelling but shippable. Five milestones keeps it tight for a side project while hitting all the key beats: make noise → talk to it → add software synths → stream it → add hardware. MCP server is milestone 1 (not 0) because the existing CLI proves MIDI works and we shouldn't redo that. Proof of concept first, hardware complexity last.

### 2026-02-14: Cut ML, DAW integration, and web UI from v1
**By:** Finn
**What:** ML-based style transfer, DAW plugin integration (VST/Ableton Link), and any web dashboard UI are explicitly out of scope for v1. They can be v2 explorations.
**Why:** Each of these is a rabbit hole that would consume the entire project budget. v1 succeeds if you can talk to the system and hear music come out of your gear. ML doesn't make that happen faster. DAW integration is a different product. Web UI contradicts the "AI is the UI" principle.

### 2026-02-13: User directive — recast to Adventure Time + add test/audio specialists
**By:** Brady (via Copilot)
**What:** Recast entire team from Firefly universe to Adventure Time. Also added two new roles: Audio/MIDI Test Engineer (Peppermint Butler) for timing-sensitive testing, and Audio Interface Dev (Banana Guard) for OS-level audio I/O and streaming audio routing.
**Why:** User request — captured for team memory
