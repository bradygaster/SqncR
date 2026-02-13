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
