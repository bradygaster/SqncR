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

### 2026-02-14: v1 roadmap structure — 5 milestones, MCP-first
**By:** Finn
**What:** The v1 roadmap is structured as 5 milestones (M0–M4). M0 validates what exists. M1 builds the MCP server and generation loop (the critical path). M2 adds software synthesis (Sonic Pi/VCV Rack). M3 adds streaming-quality features (session persistence, stability). M4 adds hardware integration and polish. Each milestone produces a usable, demoable system. Hardware MIDI device setup, device profiles, and multi-channel routing are deferred to M4, the final milestone. Software synthesis via Sonic Pi and VCV Rack is M2.
**Why:** Brady wants something compelling but shippable. Five milestones keeps it tight for a side project while hitting all the key beats: make noise → talk to it → add software synths → stream it → add hardware. MCP server is milestone 1 (not 0) because the existing CLI proves MIDI works and we shouldn't redo that. Proof of concept first, hardware complexity last. Prove the core generation engine works entirely in software before adding hardware complexity.

### 2026-02-14: Cut ML, DAW integration, and web UI from v1
**By:** Finn
**What:** ML-based style transfer, DAW plugin integration (VST/Ableton Link), and any web dashboard UI are explicitly out of scope for v1. They can be v2 explorations.
**Why:** Each of these is a rabbit hole that would consume the entire project budget. v1 succeeds if you can talk to the system and hear music come out of your gear. ML doesn't make that happen faster. DAW integration is a different product. Web UI contradicts the "AI is the UI" principle.

### 2026-02-13: User directive — recast to Adventure Time + add test/audio specialists
**By:** Brady (via Copilot)
**What:** Recast entire team from Firefly universe to Adventure Time. Also added two new roles: Audio/MIDI Test Engineer (Peppermint Butler) for timing-sensitive testing, and Audio Interface Dev (Banana Guard) for OS-level audio I/O and streaming audio routing.
**Why:** User request — captured for team memory

### 2026-02-13: Roadmap decomposed into 36 work-item GitHub issues

**By:** Finn (Lead)

**What:** The v1 roadmap (issue #1) has been decomposed into 36 individual GitHub issues, one per logical work unit. Each milestone was broken down as follows:
- **M0 (Proof of Life):** 2 issues — CLI verification, OpenTelemetry plumbing
- **M1 (The Engine Room):** 10 issues — MCP server scaffold, generation loop, core tools, music theory, rhythm, telemetry, MIDI testing
- **M2 (Software Synths):** 7 issues — Sonic Pi integration, VCV Rack integration, MCP tools, synth telemetry, spectral analysis, audio tests
- **M3 (Stream-Ready):** 8 issues — session persistence, smooth transitions, variety engine, stability, scenes, session telemetry, long-running tests
- **M4 (Know Your Gear):** 9 issues — instrument abstraction, device profiles, conversational setup, multi-channel generation, role-aware selection, drum sequencing, hardware telemetry, signal chain tests

Each issue follows a consistent structure:
- **Context:** References parent roadmap (issue #1)
- **What:** Brief description of the work
- **Acceptance Criteria:** Checkboxes derived from roadmap items
- **Owner(s):** Agent names and roles

All issues are labeled with:
- Milestone label (M0, M1, M2, M3, M4)
- Agent ownership label(s) (agent:jake, agent:bmo, agent:marceline, agent:bubblegum, agent:lemongrab, agent:rainicorn, agent:simon, agent:peppermint-butler, agent:banana-guard)

Issue #1 (the original roadmap) is now labeled as "design" to distinguish it as a planning document.

**Why:** 
1. **Actionability:** Roadmap checkboxes are not granular enough for issue tracking. Individual issues allow agents to claim work, track progress, and close items incrementally.
2. **Ownership clarity:** Each issue has explicit agent assignments. No ambiguity about who owns what.
3. **Progress visibility:** 36 closed issues is more satisfying and legible than 5 milestone checkboxes.
4. **Dependencies:** Issues can reference each other. Example: MCP tools depend on generation loop; tests depend on implementation.
5. **Prioritization:** Issues can be reordered, delayed, or cut without restructuring the entire roadmap.
6. **Team coordination:** Multiple agents can work in parallel without merge conflicts or stepping on toes.

This decomposition makes the roadmap *executable* rather than aspirational. Each issue is small enough to complete in a focused work session, large enough to deliver value. The team now has a backlog.

### 2026-02-15: NoteEvent.Note must support non-string YAML constructs
**By:** Lemongrab
**What:** The `NoteEvent.Note` property is typed as `string`, but 2 of 5 example .sqnc.yaml files use `{ choice: [...] }` mapping constructs for note values (e.g., `note: { choice: [D2, A1] }`). These files (`another-brick-in-the-wall.sqnc.yaml`, `little-fluffy-clouds.sqnc.yaml`) fail to deserialize through `SequenceParser`. The `Note` field needs to become `object` or a union type that can represent both plain note names and choice/weighted-random constructs. Similarly, the `Pattern` field in `SequenceEntry` has the same issue with `{ choice: [...], weights: [...] }`.
**Why:** 40% of example files can't be parsed. This blocks any feature that needs to load those sequences. The model needs to evolve before M1 work can treat all example files as valid test fixtures. Filed as a known limitation with regression tests documenting the current failure.

### 2026-02-14: Aspire infrastructure uses SDK 9.5.2 with explicit Microsoft.NET.Sdk pattern
**By:** Jake
**What:** The Aspire AppHost project (`src/SqncR.AppHost/`) uses the explicit two-SDK pattern: `<Project Sdk="Microsoft.NET.Sdk">` with a child `<Sdk Name="Aspire.AppHost.Sdk" Version="9.5.2" />` element, plus `Aspire.Hosting.AppHost` 9.5.2 package reference. ServiceDefaults uses OpenTelemetry 1.11.1 and Microsoft.Extensions 9.x packages. The CLI project is registered in the AppHost as `sqncr-cli`. A `Directory.Build.props` at the repo root sets common properties (net9.0, nullable, implicit usings, TreatWarningsAsErrors for src/ only). A `global.json` pins minimum SDK 9.0.100 with `latestMajor` rollForward to support the .slnx format.
**Why:** The Aspire dashboard is the "control room" for SqncR — every musical event needs to be visible there. This M0 work establishes the plumbing so Banana Guard can wire up ActivitySources and exporters, and so every subsequent milestone has OTel infrastructure ready. The two-SDK pattern is required because Aspire.AppHost.Sdk 9.5.2 doesn't auto-import Microsoft.NET.Sdk (fixed in 13.0.0+). TreatWarningsAsErrors is scoped to src/ to avoid breaking builds on pre-existing xUnit analyzer warnings in test code.

### 2026-02-13: User directive — track issue lifecycle on GitHub
**By:** Brady (via Copilot)
**What:** Mark GitHub issues as in-progress when work starts and close them when work is complete. Use labels, assignees, and issue state transitions to keep the backlog accurate. The board should always reflect reality.
**Why:** User request — Brady wants issue status to be actively managed, not just used for planning.

### 2026-02-14: OpenTelemetry instrumentation pattern — System.Diagnostics in libraries, OTel SDK only in CLI
**By:** Banana Guard
**What:** Library projects (SqncR.Core, SqncR.Midi) use only `System.Diagnostics.ActivitySource` and `Activity` for instrumentation — no OpenTelemetry NuGet packages. The CLI project is the composition root that adds OpenTelemetry SDK (`OpenTelemetry`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Extensions.Hosting`) and registers the ActivitySources by name. ActivitySources are declared as `internal static readonly` fields on the owning class. Span tags follow `{subsystem}.{attribute}` naming. The OTLP endpoint defaults to `http://localhost:4317` (Aspire dashboard) and is overridable via `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable.
**Why:** This is the canonical .NET pattern for distributed tracing. Libraries shouldn't take hard dependencies on specific telemetry exporters — they just emit activities. The host process decides where traces go. This keeps the library packages lightweight and avoids version conflicts. It also means any future host (AppHost, test harness, MCP server) can collect the same traces by registering the same source names.

### 2026-02-15: MCP Server uses official ModelContextProtocol C# SDK with stdio transport
**By:** Jake (Core Dev)
**What:** The `SqncR.McpServer` project uses the official `ModelContextProtocol` NuGet package (v0.8.0-preview.1) from the MCP project. The server uses `Microsoft.Extensions.Hosting` with `AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly()`. Tools are declared as static methods on classes annotated with `[McpServerToolType]` / `[McpServerTool]`. DI services (like `MidiService`) are injected as tool method parameters. All console logging is routed to stderr (stdout is reserved for JSON-RPC). The server is registered in Aspire AppHost as `sqncr-mcp`. The package is in preview — breaking changes are expected before 1.0.
**Why:** This establishes the canonical pattern for all future MCP tools in SqncR. Everyone adding tools (generation loop, software synth control, etc.) should follow this attribute-based pattern. The SDK handles the full MCP lifecycle (initialize, capabilities, shutdown, tool dispatch) so we don't need to implement JSON-RPC ourselves.

### 2026-02-15: Music Theory Foundations — SqncR.Theory library design
**By:** Simon (Music Theorist)
**What:** Created `SqncR.Theory` as a pure .NET 9 class library containing the foundational music theory types for SqncR:

1. **Interval.cs** — Named constants for all 13 intervals (Unison through Octave) with `GetName()` lookup. Used as building blocks for scale and chord construction.

2. **Scale.cs** — Sealed record with `RootNote`, `Name`, `Intervals`. Factory methods for 10 scale types: Major, Natural Minor, Harmonic Minor, Melodic Minor, Pentatonic Major, Pentatonic Minor, Blues, Whole Tone, Diminished (half-whole), Chromatic. Query methods: `ContainsNote(midiNote)` uses pitch-class math (mod 12), `GetNearestScaleNote(midiNote)` snaps to closest scale tone (rounds down on ties), `GetNotesInOctave(octave)` returns MIDI numbers within a given octave.

3. **Mode.cs** — All 7 modes of the major scale (Ionian–Locrian) implemented as interval rotations. `FromMajorScale(root, index)` plus named convenience methods.

4. **ScaleLibrary.cs** — Case-insensitive registry with 19 entries (10 scales + 7 modes + 2 aliases). `Get(name, root)`, `Exists(name)`, `AvailableScales`.

**Design decisions:**
- All types are immutable (records, readonly). Music theory data is value data.
- Pure computation — no side effects, no MIDI, no I/O.
- Pitch-class arithmetic is mod-12, consistent with NoteParser's C4=60 convention.
- `GetNearestScaleNote` rounds down on equidistant ties — a musical choice that favors resolution downward (common in voice leading).
- Scale intervals are stored as semitone offsets from root (0-based, all < 12). This makes mode rotation a simple array shift.
- SqncR.Core references SqncR.Theory (not the reverse) so the generation engine can use theory types.

**Why:** These types are the foundation for all music generation. Scales constrain which notes the generator can choose. Modes provide tonal color. The query API (`ContainsNote`, `GetNearestScaleNote`) will be used by the generation loop to ensure output stays musically coherent. The `ScaleLibrary` enables natural-language requests like "play in Dorian" via MCP tools.

### 2026-02-15: Rhythm types live in SqncR.Core, not SqncR.Theory
**By:** Rainicorn
**What:** All rhythm/beat/sequencer types are in `src/SqncR.Core/Rhythm/`. This is intentional: rhythm is engine-level (how to play), not theory-level (what to play). The boundary is: BeatPattern, StepSequencer, SwingProfile, DrumMap, PatternLibrary → Core. Scales, chords, key, harmony → Theory. Simon's Theory work and Rainicorn's rhythm work are parallel and decoupled.
**Why:** Rhythm drives the generation loop directly — it produces tick-timed events that feed into MIDI output. Theory informs *which notes* to play but not *when* or *how hard*. Keeping them separate avoids circular dependencies and makes it clear where to put new code. If it grooves, it's Core. If it resolves, it's Theory.

### 2026-02-15: Rhythm types produce SequencerEvents, not MIDI directly
**By:** Rainicorn
**What:** The rhythm subsystem outputs `SequencerEvent` records (tick, step index, drum voice, velocity, probability). It never imports or references MIDI types. The MIDI layer is responsible for mapping DrumVoice → MIDI note (via DrumMap) and sending NoteOn/NoteOff messages. This keeps rhythm logic testable without any MIDI dependency.
**Why:** Decoupling rhythm from MIDI means the same patterns work for software synths (Sonic Pi, VCV Rack) that don't use MIDI note numbers directly. It also makes unit testing trivial — no MIDI ports needed.

### 2026-02-15: PPQ=480 is the project standard for tick-based timing
**By:** Rainicorn
**What:** The StepSequencer defaults to 480 ticks per quarter note, matching `MetaData.Tpq` in Sequence.cs. All tick calculations assume this unless overridden. At 16-step resolution in 4/4, each step = 120 ticks, each measure = 1920 ticks.
**Why:** Consistency across the generation loop. If the sequencer and the sequence parser disagree on PPQ, timing will drift. 480 is the de facto MIDI standard and matches what's already in the codebase.

### 2026-02-15: FrozenDictionary needs explicit comparer for case-insensitive lookup
**By:** Rainicorn
**What:** When converting a `Dictionary<string, T>` with `StringComparer.OrdinalIgnoreCase` to `FrozenDictionary`, you must pass the comparer to `.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase)`. The comparer from the source dictionary is not automatically carried over.
**Why:** Discovered when PatternLibrary.Get("HOUSE") failed despite the source dictionary using case-insensitive comparison. This is a .NET gotcha worth remembering.

### 2026-02-15: IMidiOutput interface extraction and MockMidiOutput test double
**By:** Lemongrab (Tester)
**What:** Extracted `IMidiOutput` interface from `MidiService` in `src/SqncR.Midi/Testing/`. The interface covers `SendNoteOn`, `SendNoteOff`, `AllNotesOff`, and `CurrentDeviceName`. `MidiService` now implements `IMidiOutput` with zero behavioral change. `SequencePlayer` constructor changed from `MidiService` to `IMidiOutput`. `MockMidiOutput` is a thread-safe test double that captures every MIDI event with relative timing (Stopwatch from first event, ConcurrentQueue for thread safety). It lives alongside the interface in `src/SqncR.Midi/Testing/`.
**Why:** MidiService wraps DryWetMidi hardware access — untestable without real devices. The interface lets tests inject MockMidiOutput instead. All new tests run without MIDI hardware, pure software, CI/CD safe. Generation loop runs async; ConcurrentQueue ensures no data races in event capture. Issue #12 (scale-aware validation) and all M1 MIDI tests build on this framework.

### 2026-02-14: Spectral Analysis Uses MathNet.Numerics with Parabolic Interpolation
**Date:** 2026-02-14
**Author:** Peppermint Butler (Audio/MIDI Test Engineer)
**Issue:** #19

## Context

Needed FFT-based frequency detection for audio test assertions. Chose `MathNet.Numerics` for FFT (well-maintained, no native dependencies, CI-friendly). Applied Hanning window before FFT to reduce spectral leakage. Used parabolic interpolation on magnitude peaks for sub-bin frequency resolution.

## Decision

- **Library:** `MathNet.Numerics 5.0.0` — pure .NET, no native deps, runs everywhere without hardware
- **Window function:** Hanning — good general-purpose choice for music signals
- **Peak detection:** Local maxima with parabolic interpolation for better frequency accuracy
- **Default tolerance:** ±5% — works well for single tones and chords at 44100 Hz / 1s duration
- **Location:** `src/SqncR.Testing/` — shared library, NOT a test project. Test projects reference it.
- **Assertions:** Throw `Xunit.Sdk.XunitException` with descriptive messages including detected peaks

## Impact

All future audio integration tests (M2, M3, M4) should reference `SqncR.Testing` for spectral analysis. The `AudioAssertions` class provides the primary API for test authors. `ToneGenerator` is useful for self-testing the analysis pipeline without real audio hardware.

### 2026-02-15: Sonic Pi OSC Protocol — No External NuGet
**By:** Marceline
**Date:** 2026-02-15
**Status:** Implemented
**Issue:** #14

## What
The Sonic Pi OSC integration uses raw UDP + manual OSC byte encoding rather than an external NuGet package. The `OscMessage` class is ~65 lines of code implementing just enough of OSC 1.0 to send string-argument messages.

## Why
- Sonic Pi only needs two OSC messages: `/run-code` (two string args: GUID + Ruby code) and `/stop-all-jobs` (no args).
- The OSC 1.0 wire format for strings is trivial: null-terminated, 4-byte-aligned ASCII.
- Adding a NuGet dependency for this would be over-engineering. Zero external dependencies keeps the project lean.
- If we later need richer OSC (e.g., for SuperCollider integration), we can revisit.

## Impact
- `SqncR.SonicPi` has zero NuGet dependencies (only references `SqncR.Core`).
- The `OscMessage` class is `internal` — implementation detail, not part of the public API.
- Anyone adding new OSC endpoints just adds a method to `OscMessage`.

### 2026-02-13: Use Opus for coding tasks
**By:** Brady (via Copilot)
**What:** Brady approved using Claude Opus (claude-opus-4.6) for coding tasks. "Feel FREE to use Opus for coding."
**Why:** User request — quality over cost for code generation. Overrides default sonnet tier for implementation work.

### 2026-02-13: Blog as we go
**By:** Brady (via Copilot)
**What:** Brady wants the team to blog about the project as it progresses. Hire a dedicated blogger agent.
**Why:** User request — live documentation of the build journey. DevRel/content creation running parallel to development.

### 2026-02-14: VCV Rack Patch Serialization Uses JsonNode API
**Date:** 2026-02-14
**Author:** Bubblegum (VCV Rack Specialist)
**Issue:** #16

## Context
VCV Rack patches require a specific JSON format with nested structures (modules with params arrays, position arrays, cables with port IDs). Attempted source-generated `JsonSerializerContext` but it cannot handle `Dictionary<string, object>` with polymorphic nested values (lists, arrays, dictionaries).

## Decision
Use `System.Text.Json.Nodes` (`JsonObject` / `JsonArray`) for building VCV Rack patch JSON. This avoids reflection-based serialization complexity while giving full control over the output structure.

## Consequences
- Clean, readable code in `VcvPatch.ToJson()`
- No AOT/trimming issues since JsonNode is fully supported
- Other team members working with VCV Rack patches should use the same approach
- Port names in ModuleLibrary are friendly strings mapped to integer port indices — use `PatchBuilder.Cable()` for name-based wiring


### 2026-02-14: VCV Rack MCP Tool Design
**Agent:** Bubblegum  
**Date:** 2026-02-14  
**Issue:** #17

## Context
Needed to expose VCV Rack patch generation and process management to AI assistants via MCP tools.

## Decision
- All 5 VCV Rack tools live in a single `VcvRackTool.cs` static class (matching `GenerationTool.cs` pattern which groups related tools).
- `generate_patch` does NOT require `VcvRackLauncher` — it only needs `PatchTemplates` and `VcvPatch.SaveAs()`. This keeps patch generation decoupled from process management.
- Template selection uses a simple string switch (`basic`/`ambient`/`bass`) rather than an enum, since MCP tool params are strings and the AI needs to pass them by name.
- Default output path uses `Path.GetTempPath()` so patches can be generated without the user specifying a directory.

## Alternatives Considered
- Separate tool classes per operation (rejected: too many files for 5 related tools)
- Accepting a full patch JSON as input (rejected: too complex for AI; templates are the right abstraction)

### 2026-02-16: Sonic Pi OscClient as Singleton in MCP Server DI
**Date:** 2026-02-16
**Author:** Jake (Core Dev)
**Issue:** #15

## Context
The MCP server needs to communicate with Sonic Pi via OSC. `OscClient` wraps a `UdpClient` and sends messages to `localhost:4560`.

## Decision
Register `OscClient` as a singleton in the DI container with default constructor (port 4560, host 127.0.0.1). All Sonic Pi MCP tools receive it via parameter injection, matching the established pattern (e.g., `MidiService`, `GenerationEngine`).

## Rationale
- **Singleton** because `OscClient` holds a `UdpClient` — creating per-request would waste sockets and leak if not disposed.
- **Default port 4560** is Sonic Pi's standard OSC listen port. Configuration override can be added later if needed.
- **No factory/options pattern** — keeps it simple for now. If we need configurable ports (e.g., multiple Sonic Pi instances), we can add `IOptions<SonicPiOptions>` later.
- **FX chain defaults to mix: 0.5** — a reasonable middle ground. Individual mix control can be added as a future enhancement.

## Status
Implemented and merged into MCP server.
