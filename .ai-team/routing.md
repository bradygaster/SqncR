# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Architecture & scope | Finn | System design, trade-offs, what to build next, code review |
| Core engine & C#/.NET | Jake | Generative algorithms, sequencer logic, MCP server, data models |
| MIDI hardware & protocol | BMO | Device discovery, MIDI I/O, routing, latency, device profiles, real-time |
| Sonic Pi & OSC | Marceline | Sonic Pi Ruby code generation, OSC protocol, live_loop patterns, generative music in Sonic Pi |
| VCV Rack 2 | Bubblegum | VCV Rack patch generation, module ecosystem, .vcv format, virtual MIDI ports, MIDI-to-CV |
| Testing & quality | Lemongrab | Write tests, general test strategy, edge cases, quality gates |
| Rhythm & beats | Rainicorn | Drum patterns, beat design, percussion, groove, euclidean rhythms, swing, humanization |
| Music theory & harmony | Simon | Scales, modes, chord progressions, voice leading, vibe-to-parameters translation |
| Audio/MIDI testing | Peppermint Butler | Timing-sensitive tests, MIDI protocol conformance, OSC verification, latency benchmarks, mock devices |
| Audio interfaces & routing | Banana Guard | OS-level audio I/O, ASIO/WASAPI, virtual audio routing, inter-app audio, streaming audio setup |
| Code review | Finn | Review PRs, check quality, suggest improvements |
| Scope & priorities | Finn | What to build next, trade-offs, decisions |
| Session logging | Scribe | Automatic — never needs routing |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, assign `squad:{member}` label | Finn |
| `squad:finn` | Architecture and scope decisions | Finn |
| `squad:jake` | Core engine and C#/.NET work | Jake |
| `squad:bmo` | MIDI hardware and protocol work | BMO |
| `squad:marceline` | Sonic Pi integration and OSC protocol | Marceline |
| `squad:bubblegum` | VCV Rack 2 patches and module ecosystem | Bubblegum |
| `squad:lemongrab` | Testing and quality work | Lemongrab |
| `squad:rainicorn` | Rhythm, beats, and percussion work | Rainicorn |
| `squad:simon` | Music theory, harmony, and composition work | Simon |
| `squad:peppermint-butler` | Audio/MIDI testing, timing validation, protocol conformance | Peppermint Butler |
| `squad:banana-guard` | Audio interfaces, OS-level audio, streaming audio routing | Banana Guard |

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
2. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
4. **When two agents could handle it**, pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
6. **Anticipate downstream work.** If a feature is being built, spawn the tester to write test cases from requirements simultaneously.
7. **MIDI + synth overlap** — if a task involves MIDI routing and Sonic Pi, spawn BMO and Marceline. If it involves MIDI routing and VCV Rack, spawn BMO and Bubblegum.
8. **Research tasks** — Marceline, Bubblegum, and BMO all handle research in their domains. For broad "what are our options" questions, spawn relevant ones.
9. **Musical content generation** — if a task involves both rhythm (Rainicorn) and melody/harmony (Simon), spawn both. They complement each other.
10. **Vibe translation** — when the user describes a mood or genre, Simon translates to theory parameters, Rainicorn handles the rhythmic feel.
11. **Testing split** — Lemongrab owns general test strategy and quality gates. Peppermint Butler owns MIDI/OSC/audio-specific testing. Both should be spawned for integration test work.
12. **Audio chain** — if a task touches audio output, streaming, or inter-app routing, spawn Banana Guard. If it also touches MIDI timing, add Peppermint Butler.
