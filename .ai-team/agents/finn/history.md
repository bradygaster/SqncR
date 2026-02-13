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

### 2026-02-14: v1 roadmap — what already exists determines milestone zero
The repo already has a working CLI that plays `.sqnc.yaml` files through real MIDI devices (SqncR.Core + SqncR.Midi + SqncR.Cli). This means M0 is already partially done — MIDI output and sequence parsing are proven. The roadmap should start from this foundation, not redesign it.

### 2026-02-14: MCP server is the critical path, not more CLI features
The entire value proposition — "talk to your studio while you code" — depends on the MCP server existing. Every milestone that doesn't move toward a working MCP server with a generation loop is a distraction. CLI is a dev tool, not the product.

### 2026-02-14: Generation loop before music theory depth
A simple generation loop that plays *something* through real hardware is worth more than a sophisticated music theory engine with no playback. Ship sound first, make it smart later. The existing SequencePlayer proves the MIDI path works; the generation loop replaces the file-based timeline with a live one.

### 2026-02-14: Streaming use case drives later milestones
Brady explicitly wants to stream generative music while coding. This implies: stable long-running playback, graceful transitions between musical ideas, and enough musical variety that it's interesting to listen to. These are quality-of-life features that belong after the core works.

### 2026-02-14: Rhythm and drum sequencing elevated to first-class feature in v1
Brady requested that rhythm and drum sequencing not remain implicit in Rainicorn's work, but become explicit, first-class features with their own work items in the roadmap. M1 now includes foundational beat patterns (four-on-the-floor, half-time), step sequencer abstraction, and swing/shuffle parameters. M2 now includes advanced rhythm work: per-instrument drum maps, velocity accent patterns, fills and transitions, polyrhythm support (3-over-4, 5-over-4, etc.), pattern library with reusable beat templates, and rhythmic theory collaboration between Rainicorn and Simon on time signatures and syncopation. This reflects the vision of "all sorts of opportunities" Brady wants to explore in rhythm design.

### 2026-02-14: Three-layer automated audio testing strategy integrated into v1
Brady requested automated audio verification testing as a first-class concern—not just unit tests, but tests that validate SqncR is producing the right sounds. Updated Issue #1 to embed testing milestones across M1–M4:
- **M1 (MIDI Foundation):** MIDI output capture framework with deterministic assertions (Lemongrab). Test harness intercepts MIDI messages; validates notes, velocities, timing, channels.
- **M2 (Audio Integration):** End-to-end signal chain validation (Peppermint Butler). Audio loopback capture proves Sonic Pi/VCV Rack signal is present and not silent. Device profile test fixtures.
- **M3 (Spectral Analysis):** FFT-based frequency detection (Peppermint Butler). Verify notes are sounding at expected pitches. Fuzzy matching with configurable tolerance: "I sent C4, is there energy at 261.6 Hz?"
- **M4 (Stability & Canary):** Long-running session audio monitoring, graceful failure recovery tests, memory/CPU profiling. Validates "still playing and sounds reasonable" over hours.
Added testing philosophy section to Issue #1: "Tests don't just check code — they listen to the music." This frames testing as part of the product, not a chore.

### 2026-02-14: v1 Roadmap restructured — software synthesis BEFORE hardware
Brady requested hardware MIDI integration be moved to the final milestone. Rationale: prove the generation engine works entirely in software (Sonic Pi, VCV Rack) before adding device profile complexity. Reordered Issue #1 milestones:
- M0: Proof of Life (unchanged)
- M1: The Engine Room (unchanged — core generation loop, music theory, foundational rhythm)
- M2: Software Synths (was M3, moved earlier — Sonic Pi OSC + VCV Rack patch generation, no hardware needed)
- M3: Stream-Ready (was M4, moved earlier — session persistence, stability, variety engine, long-running testing)
- M4: Know Your Gear (was M2, moved last — device profiles, conversational setup, multi-channel routing)
This ordering reflects the philosophy: establish that the core system *works* with pure software before integrating hardware constraints. Hardware is the final integration layer, not the foundation. All existing content preserved; testing strategy and rhythm work carry forward to new milestone positions.

### 2026-02-14: OpenTelemetry observability is a CORE requirement, not optional
Brady wants comprehensive real-time visibility into the music generation pipeline. Every musical event (note on/off, chord change, pattern trigger, tempo shift, transition, section change) must emit a span to the .NET OpenTelemetry SDK. This isn't "observability we'll add later" — it's woven into EVERY milestone starting with M0. The Aspire dashboard becomes the "control room" where users can watch their music being made in real-time: session span contains section spans which contain measure spans which contain beat spans which contain note spans (hierarchical). Per-subsystem ActivitySources: `SqncR.Generation`, `SqncR.Playback`, `SqncR.Sequencer`, `SqncR.Midi`, `SqncR.SonicPi`, `SqncR.VcvRack`, plus others as features grow. Custom metrics track generation latency, notes per second, active voices, pattern density, device-specific send latency. M1 establishes the plumbing and core spans. M2 extends to software synth traces (see Ruby code being sent to Sonic Pi, see patch loading into VCV Rack). M3 adds session-level traces spanning hours, variety engine decisions, stability snapshots. M4 completes with hardware device-specific telemetry and multi-channel trace correlation. This decision was driven by Brady's request: "each user event like changing notes or playing should have telemetry events associated with them and spans/activities. one should be able to look in the aspire dashboard and see the progress of a song being made/played/looped."

### 2026-02-14: Roadmap decomposed into 36 individual work-item GitHub issues
Brady requested the v1 roadmap (issue #1) be split into individual work-item-sized GitHub issues. Each milestone was decomposed into logical work units: M0 has 2 issues, M1 has 10 issues, M2 has 7 issues, M3 has 8 issues, M4 has 9 issues (total 36). Each issue has clear title (milestone-prefixed), context (references parent issue #1), acceptance criteria (derived from roadmap checkboxes), and agent ownership labels. Issue structure follows the pattern: Context -> What -> Acceptance Criteria -> Owner(s). All issues reference the parent design doc (issue #1, now labeled "design"). Milestone labels (M0-M4) and agent labels (agent:jake, agent:bmo, etc.) created for tracking. This decomposition makes the roadmap actionable: agents can pick up individual issues, track progress granularly, and close work items incrementally rather than tackling monolithic milestones.

📌 Team update (2026-02-13): Roadmap decomposed into 36 work-item GitHub issues — decided by Finn (you)
You completed the decomposition of issue #1 into 36 individual GitHub work items. This makes the v1 roadmap actionable and enables granular progress tracking across the team. All agents have been notified of their issue assignments.
