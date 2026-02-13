# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Architecture & scope | Mal | System design, trade-offs, what to build next, code review |
| Core engine & C#/.NET | Kaylee | Generative algorithms, sequencer logic, MCP server, data models |
| MIDI hardware & protocol | Wash | Device discovery, MIDI I/O, routing, latency, device profiles, real-time |
| Sonic Pi & OSC | Inara | Sonic Pi Ruby code generation, OSC protocol, live_loop patterns, generative music in Sonic Pi |
| VCV Rack 2 | River | VCV Rack patch generation, module ecosystem, .vcv format, virtual MIDI ports, MIDI-to-CV |
| Rhythm & beats | Zoe | Drum patterns, beat design, percussion, groove, euclidean rhythms, swing, humanization |
| Music theory & harmony | Book | Scales, modes, chord progressions, voice leading, vibe-to-parameters translation |
| Testing & quality | Jayne | Write tests, MIDI protocol validation, edge cases, integration tests |
| Code review | Mal | Review PRs, check quality, suggest improvements |
| Scope & priorities | Mal | What to build next, trade-offs, decisions |
| Session logging | Scribe | Automatic — never needs routing |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, assign `squad:{member}` label | Mal |
| `squad:mal` | Architecture and scope decisions | Mal |
| `squad:kaylee` | Core engine and C#/.NET work | Kaylee |
| `squad:wash` | MIDI hardware and protocol work | Wash |
| `squad:inara` | Sonic Pi integration and OSC protocol | Inara |
| `squad:river` | VCV Rack 2 patches and module ecosystem | River |
| `squad:jayne` | Testing and quality work | Jayne |
| `squad:zoe` | Rhythm, beats, and percussion work | Zoe |
| `squad:book` | Music theory, harmony, and composition work | Book |

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
2. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
4. **When two agents could handle it**, pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
6. **Anticipate downstream work.** If a feature is being built, spawn the tester to write test cases from requirements simultaneously.
7. **MIDI + synth overlap** — if a task involves MIDI routing and Sonic Pi, spawn Wash and Inara. If it involves MIDI routing and VCV Rack, spawn Wash and River.
8. **Research tasks** — Inara and Wash both handle research in their domains. For broad "what are our options" questions, spawn both.
9. **Musical content generation** — if a task involves both rhythm (Zoe) and melody/harmony (Book), spawn both. They complement each other.
10. **Vibe translation** — when the user describes a mood or genre, Book translates to theory parameters, Zoe handles the rhythmic feel.
