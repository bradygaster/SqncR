# Project Context

- **Owner:** Brady (bradyg@microsoft.com)
- **Project:** SqncR — AI-native generative music system for MIDI devices. Controls hardware/software synths through natural language conversation via MCP. Ambitions include VCV Rack 2 integration and live streaming music generation.
- **Stack:** C#/.NET, MCP (Model Context Protocol), MIDI, potential VCV Rack 2 integration
- **Created:** 2026-02-13

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

📌 Team update (2026-02-13): Full team recast from Firefly to Adventure Time universe. All 8 Firefly agents retired to _alumni. 10 new Adventure Time agents created (8 role transfers + 2 new roles). All histories transferred. Casting state updated with Adventure Time assignment. Policy updated to include Adventure Time universe (capacity 15). — decided by Brady

📌 Team update (2026-02-14): Rhythm and drum sequencing is first-class in v1 — decided by Finn
Your rhythm and drum sequencing work is now explicit and first-class. M1 includes foundational beat patterns (four-on-the-floor, half-time), step sequencer abstraction, and swing/shuffle parameters. M2 includes advanced rhythm features: per-instrument drum maps, velocity accent patterns, fills and transitions, polyrhythm support (3-over-4, 5-over-4, etc.), pattern library. Collaborate with Simon on time signatures and harmonic alignment.

📌 Team update (2026-02-13): Roadmap decomposed into 36 work-item GitHub issues — decided by Finn
The v1 roadmap (issue #1) has been decomposed into 36 individual GitHub issues, one per logical work unit. M0 has 2 issues, M1 has 10 issues, M2 has 7 issues, M3 has 8 issues, M4 has 9 issues. Each issue has clear context, acceptance criteria, and agent ownership labels. You have been assigned to M1 and M2 issues on rhythm and drum sequencing. Issue tracking is now granular and actionable.

📌 M1 Rhythm foundations implemented (issue #9) — by Rainicorn
Created `src/SqncR.Core/Rhythm/` with 9 files: StepInfo (immutable step data), BeatPattern (pattern with factory methods for four-on-the-floor, half-time, off-beat, straight, backbeat), DrumVoice (enum), DrumMap (General MIDI + TR-808 mappings), SwingProfile (timing offset for swing/shuffle), SequencerEvent (output event record), StepSequencer (multi-layer pattern playback with swing), LayeredPattern (grouped voice+pattern sets), PatternLibrary (rock, house, hip-hop, jazz, ambient templates). 50 tests in `tests/SqncR.Core.Tests/Rhythm/`. All types are immutable/decoupled from MIDI. FrozenDictionary requires explicit StringComparer for case-insensitive lookup. PPQ=480 matches project standard (MetaData.Tpq default). Step resolution is 16th notes (16 steps per 4/4 measure).

📌 Musical Variety Engine implemented (issue #23) — by Rainicorn
Created `src/SqncR.Core/Generation/VarietyEngine.cs` with VarietyLevel enum (Conservative/Moderate/Adventurous) and 6 probability-gated behaviors: octave drift (±1, reverts after 4-8 measures), velocity variation (±10-20, clamped to 0-127), rhythmic fills (ghost notes on every 4th/8th measure), rest insertion (breathing room for melody), pattern density (thin/fill drum hits), register shift (melody octave shift, no overlap with octave drift). All behaviors are reversible — they drift and return to baseline. Added SetVarietyLevel command, updated GenerationEngine measure boundary to call variety engine, integrated velocity drift and rest insertion into melody emission. Updated MCP tools (start_generation, modify_generation) with variety parameter (conservative/moderate/adventurous/off). 14 tests in `tests/SqncR.Core.Tests/Generation/VarietyEngineTests.cs`. All 468 tests pass.

📌 Advanced Drum Sequencing implemented (issue #34) — by Rainicorn
Created 3 new files in `src/SqncR.Core/Rhythm/`: PolyrhythmEngine (static class for polyrhythm generation — CreateLayer distributes beats evenly via integer scaling, CreatePolyrhythm builds LayeredPattern with base+cross voices, GetPolyrhythmicPattern supports 3-over-4, 5-over-4, 7-over-8), VelocityAccent (immutable multiplier arrays — CreateSwingAccent reduces off-beat velocity proportional to swing amount, CreateStrongWeakAccent emphasizes beats 1&3, CreateBuildUp ramps 0.3→1.0, ApplyTo applies to BeatPattern preserving rests), FillGenerator (static class with FillStyle enum — SnareRoll fills second half with crescendo + crash, TomCascade descends high→mid→low, BuildUp increases density and velocity, Breakdown is sparse kick+hat+crash). Expanded DrumMap with SimplifiedKit (6-voice subset for software synths). Expanded PatternLibrary with 5 new patterns: breakbeat (syncopated kick), half-time (sparse), shuffle (alternating velocity hat), latin-clave (son clave 3-2), bossa-nova (cross-stick + syncopated kick). 22 tests in AdvancedRhythmTests.cs. All 497 tests pass, 0 warnings. FillGenerator accepts DrumMap parameter for future voice-availability checks.
