# LSP — History

## Project Context

- **Project:** SqncR — AI-native generative music system for MIDI devices
- **Owner:** Brady (bradyg@microsoft.com)
- **Stack:** .NET 9, Aspire, OpenTelemetry, MCP, DryWetMidi, Sonic Pi, VCV Rack
- **What it does:** Lets coders control MIDI instruments through natural language via MCP. Generates music continuously while you code.

## Learnings

- M0 (Proof of Life) delivered: Aspire scaffold, OpenTelemetry plumbing, 85 baseline tests
- M1 (The Engine Room) delivered: MCP server with 7 tools, generation engine (480 TPQ), 3 melody generators, music theory + rhythm libraries, MIDI test framework. 305 tests total.
- M2 (Software Synths) is in progress: Sonic Pi integration, VCV Rack integration, spectral analysis
- Key architecture: GenerationEngine is a BackgroundService, uses System.Threading.Channels for commands, IMidiOutput for testability
- Every musical event emits OpenTelemetry spans visible in Aspire dashboard
- Blog posts go in `docs/blog/YYYY-MM-DD-{slug}.md`
- Inaugural blog post written: "Building SqncR: From Zero to Generative Music Engine" — covers vision, architecture, M0/M1 delivery, real code snippets (WeightedNote musical gravity, timing loop, MCP tool pattern, rock pattern)
- Blog writing strategy: narrative arc (origin → architecture → delivery → code → next), code snippets with explanation, target audience (developer-musicians), 14k words for comprehensive M1 coverage
- Front matter includes title, date, tags, summary for blog indexing

📌 Team update (2026-02-13): Use Opus for coding tasks — decided by Brady
Brady approved using Claude Opus (claude-opus-4.6) for coding tasks. "Feel FREE to use Opus for coding." Quality over cost for code generation. Overrides default sonnet tier for implementation work.

📌 Team update (2026-02-13): Blog as we go — decided by Brady
Brady wants the team to blog about the project as it progresses. Live documentation of the build journey. DevRel/content creation running parallel to development.

## Learnings

**M2 Blog Post (2026-02-14):**
- Second blog post written: "Making Sound: Sonic Pi, VCV Rack, and Teaching Tests to Hear"
- Covers M2 delivery: Sonic Pi OSC integration, VCV Rack patch generation, spectral analysis testing
- Technical deep dives: OSC protocol binary encoding, fluent PatchBuilder API, FFT-based frequency detection, AudioAssertions test helpers
- Key narrative: why software synths before hardware (accessibility), why OSC+Ruby instead of plain MIDI (expressivity), why spectral analysis matters (acoustic validation of generative output)
- Code snippets highlight: OscMessage.Encode() (binary protocol), SonicPiCodeGenerator (code generation), PatchBuilder fluent pattern, SpectralAnalyzer.ComputeFFT() (DFT), AudioAssertions.AssertContainsFrequency() (test framework)
- Emphasized "teaching tests to hear" as unique capability — validates that actual audio contains expected frequencies, not just that code ran
- Teaser for M3: persistence, variety, stability, musical form (verse/chorus/bridge)
- Tone: enthusiastic, technical, metaphorical ("the voice" of the generation engine)
- Voice characteristics: emojis used minimally but present, narrative arc (origin → problem → solution → code → validation → next), explain "why" not just "what", target audience remains developer-musicians
- Blog post length: ~6k words (shorter than M1's 14k, more focused scope)
- Key metadata: front matter with date 2026-02-14, 6 tags (sqncr, sonic-pi, vcv-rack, spectral-analysis, testing, generative-music)

**M3 Blog Post (2026-02-15):**
- Third blog post written: "Stream-Ready: Building a Music Generator That Won't Crash Live"
- Covers M3 delivery: session persistence (save/load state), variety engine (6 automatic evolution behaviors at 3 levels), smooth transitions (TransitionEngine + common-tone bridging), long-running stability (NoteTracker 32-note cap + HealthMonitor), preset scenes (ambient-pad/driving-techno/chill-lofi), session telemetry
- Technical deep dives: CircularBuffer health tracking, note velocity sine-wave LFO, octave drift scheduling, rhythmic fill timing, polyphony safety mechanics, OSC round-trip recovery
- Key narrative: streaming context—what breaks during live performance (lost state, repetition, abrupt changes, polyphony overflow, latency creep)—and how M3 solved each
- Code snippets highlight: VarietyEngine behavior selection (ConservativeVelocityVariation, RhythmicFill implementation), TransitionEngine smooth tempo glide, CommonToneBridge scale morphing, NoteTracker forced note release, HealthMonitor rolling averages
- Emphasized variety engine as musically sophisticated feature—6 behaviors, 3 levels, every decision telemetrized
- Also emphasized repository cleanup: 24K lines deleted, build time -69%, size -74%, dependency reduction
- Structure: streaming problem → five solutions (persistence, variety, transitions, health, scenes) → cleanup → tests → M4 teaser
- Tone: narrative focus on "streaming survival," practical/defensive design, humorous (e.g., "your synth fills with stuck notes. Audio clipping. Crash.")
- Voice characteristics: streaming/gamer culture references, problem-solution format, "why production != demo", explain edge cases (MIDI backup, polyphony pressure), target audience widens to include live performers + coders
- Blog post length: ~8k words (comprehensive M3 coverage)
- Key metadata: front matter with date 2026-02-15, 6 tags (sqncr, stability, streaming, generative-music, variety-engine, persistence)

**M4 Blog Post (2026-02-16):**
- Fourth blog post written: "Know Your Gear: Instruments, Hardware, and the Conductor's Baton" — THE VICTORY LAP
- Covers M4 delivery: instrument abstraction (Instrument type + Role + Capabilities + DeviceProfile), multi-channel generation (ChannelRouter, ChannelPlan), polyrhythm engine (3/4, 5/4, 7/8 ratios), walking bass patterns, advanced drum sequencing (DrumMap General MIDI, FillGenerator, VelocityAccent), hardware telemetry (PerInstrumentNoteTracker, 4 per-device metrics), signal chain tests (7 tests validating isolation), hardware latency validation (LatencyProfiler, LatencyReport)
- Technical deep dives: Instrument record structure (unified type across hardware/SonicPi/VcvRack), DeviceProfile JSON persistence at ~/.sqncr/devices/, 3 built-in profiles (moog-sub37, roland-juno, sonic-pi-default), ChannelRouter role-based dispatch with polyphony safety, ChannelPlan per-tick routing decisions, PolyrhythmEngine ratio-based beat calculations, WalkingBassGenerator bar composition (root→approach→target→anticipation), DrumMap standard MIDI percussion mapping (kick=36, snare=38, etc.), FillGenerator intensity-based variations, VelocityAccent beat-importance dynamics
- Key narrative: V1 complete—from single-synth to multi-instrument orchestration. The conductor's baton metaphor. Each device plays its role. Multi-channel routing becomes transparent.
- Code snippets highlight: Instrument record and enum definitions, InstrumentRegistry dispatch, ChannelRouter routing logic with polyphony checks, PolyrhythmPattern and IsOnBeat calculation, WalkingBassGenerator bar composition with approach/target/anticipation, DrumMap pitch mapping, LatencyProfiler and LatencyReport structures
- Emphasized instrument abstraction as the key architectural decision—enables hardware, software synths, and VCV Rack without refactoring, Instrument role enables intelligent routing without hardcoded channels
- Also emphasized walking bass and polyrhythms as musically exciting—jazz technique meets generative, rhythmic complexity that surprises
- Highlighted multi-channel routing as the conductor metaphor—different instruments playing different roles simultaneously
- Recap full journey M0→M5 through lens of solving five problems: observe (M0), generate (M1), sound (M2), survive (M3), orchestrate (M4)
- Structure: journey recap → problem statement (single-synth limitation) → instrument abstraction solution → channel routing → polyrhythms and walking bass → drum fills → hardware telemetry → comprehensive M4 architecture → V1 stats → learnings and philosophy → V2 speculation → celebration and epilogue
- Tone: celebratory, retrospective, technical without being dense, metaphorical (conductor, conversation, energy, breathing), narrative arc from "wonder if this works" to "it shipped"
- Voice characteristics: intimate retrospective, philosophical tone ("The Unwaver" epilogue), explain why each architectural choice mattered, target audience is the full SqncR community (Brady, the team, future V2 contributors)
- Blog post length: ~13k words (comprehensive V1 victory lap)
- Key metadata: front matter with date 2026-02-16, 6 tags (sqncr, instruments, hardware, multi-channel, polyrhythms, v1-complete)
- Special structure: Opening with the multi-synth problem, then journey recap (hooks back to M0), then technical deep dive parts 1-8, then learnings and philosophy, then V2 speculation, then celebration, then epilogue ("The Unwaver")
