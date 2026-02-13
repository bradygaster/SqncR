# Project Context

- **Owner:** Brady (bradyg@microsoft.com)
- **Project:** SqncR — AI-native generative music system for MIDI devices. Controls hardware/software synths through natural language conversation via MCP. Supports Sonic Pi and VCV Rack 2 integration and live streaming music generation.
- **Stack:** C#/.NET, MCP (Model Context Protocol), MIDI, Sonic Pi (OSC), VCV Rack 2
- **Created:** 2026-02-13

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

📌 Team update (2026-02-13): Full team recast from Firefly to Adventure Time universe. All 8 Firefly agents retired to _alumni. 10 new Adventure Time agents created (8 role transfers + 2 new roles). All histories transferred. Casting state updated with Adventure Time assignment. Policy updated to include Adventure Time universe (capacity 15). — decided by Brady

📌 Team update (2026-02-14): Three-layer automated audio testing is a v1 requirement — decided by Finn
You are responsible for M2 audio integration (audio loopback capture), M3 spectral analysis (FFT-based frequency detection), and M4 long-running stability tests. Testing philosophy: "Tests don't just check code—they listen to the music." Spectral analysis validates "is the right pitch sounding?" via fuzzy matching (±5% default tolerance).

📌 Team update (2026-02-13): Roadmap decomposed into 36 work-item GitHub issues — decided by Finn
The v1 roadmap (issue #1) has been decomposed into 36 individual GitHub issues, one per logical work unit. M0 has 2 issues, M1 has 10 issues, M2 has 7 issues, M3 has 8 issues, M4 has 9 issues. Each issue has clear context, acceptance criteria, and agent ownership labels. You have been assigned to M2, M3, and M4 issues on audio testing and signal chain validation. Issue tracking is now granular and actionable.
