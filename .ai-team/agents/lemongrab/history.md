# Project Context

- **Owner:** Brady (bradyg@microsoft.com)
- **Project:** SqncR — AI-native generative music system for MIDI devices. Controls hardware/software synths through natural language conversation via MCP. Ambitions include VCV Rack 2 integration and live streaming music generation.
- **Stack:** C#/.NET, MCP (Model Context Protocol), MIDI, potential VCV Rack 2 integration
- **Created:** 2026-02-13

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

📌 Team update (2026-02-13): Full team recast from Firefly to Adventure Time universe. All 8 Firefly agents retired to _alumni. 10 new Adventure Time agents created (8 role transfers + 2 new roles). All histories transferred. Casting state updated with Adventure Time assignment. Policy updated to include Adventure Time universe (capacity 15). — decided by Brady

📌 Team update (2026-02-14): Three-layer automated audio testing is a v1 requirement — decided by Finn
You are responsible for M1 MIDI testing framework. Test harness intercepts MIDI messages before they hit devices. Deterministic assertions on notes, velocities, durations, channel assignments. No hardware required; runs in CI/CD. The testing strategy spans all 4 milestones: M2 audio capture, M3 spectral analysis, M4 long-running stability.

📌 Team update (2026-02-13): Roadmap decomposed into 36 work-item GitHub issues — decided by Finn
The v1 roadmap (issue #1) has been decomposed into 36 individual GitHub issues, one per logical work unit. M0 has 2 issues, M1 has 10 issues, M2 has 7 issues, M3 has 8 issues, M4 has 9 issues. Each issue has clear context, acceptance criteria, and agent ownership labels. You have been assigned to M1 issues on MIDI testing framework. Issue tracking is now granular and actionable.

## Learnings — M0 Test Baseline

- Test convention: one test class per source class, file named `{SourceClass}Tests.cs` in `tests/SqncR.Core.Tests/`
- xUnit with `[Theory]`/`[InlineData]` for parameterized tests, `[Fact]` for single assertions
- Example YAML files live in `examples/` — reference via `Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "examples")` for relative paths from test bin output
- Key test files: `NoteParserTests.cs` (NoteParser coverage incl. enharmonics, range, round-trip), `SequenceParserTests.cs` (SequenceParser coverage incl. all 5 example files, meta/patterns/sections/arrange/devices/intent)
- SpecFlow/Gherkin tests in `tests/SqncR.Specs/` cover `PatternValidator` — a placeholder implementation lives inside `PatternValidationSteps.cs` and needs to move to `SqncR.Core`
- Known bug: `NoteParser` doesn't handle octave wrapping for B# and Cb enharmonics (B#3 computes to 48 instead of 60, Cb4 to 71 instead of 59)
- Known limitation: `SequenceParser` can't deserialize YAML files using `{ choice: [...] }` in note fields — `NoteEvent.Note` is typed as `string` but choice constructs produce a mapping node. Affects `another-brick-in-the-wall.sqnc.yaml` and `little-fluffy-clouds.sqnc.yaml`
- Baseline: 39 tests before M0 work → 85 tests after (72 Core + 13 Specs), all passing
- `SqncR.AppHost.csproj` emits `error MSB4057: The target "VSTest" does not exist` when running `dotnet test` at solution root — pre-existing, not a test project issue

📌 Team update (2026-02-15): NoteEvent deserialization model evolution planned — M0 proof-of-life logged
M0 milestone complete. Your 59 new SequenceParser tests + expanded NoteParser coverage establishes confidence baseline (85 tests passing, up from 13). Known limitation filed: NoteEvent.Note must support object types for `{ choice: [...] }` constructs to parse 2 of 5 example files. Model evolution planned for M1 before full fixture validation. Session logged to `.ai-team/log/2026-02-13-m0-proof-of-life.md`.
