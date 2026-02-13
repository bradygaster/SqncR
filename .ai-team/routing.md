# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Architecture & scope | Mal | System design, trade-offs, what to build next, code review |
| Core engine & C#/.NET | Kaylee | Generative algorithms, sequencer logic, MCP server, data models |
| MIDI hardware & protocol | Wash | Device discovery, MIDI I/O, routing, latency, device profiles, real-time |
| VCV Rack & synth engines | Inara | VCV Rack 2 patches, plugin research, synth engine alternatives, OSC/MIDI bridge |
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
| `squad:inara` | VCV Rack, synth engines, and sound design research | Inara |
| `squad:jayne` | Testing and quality work | Jayne |

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
2. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
4. **When two agents could handle it**, pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
6. **Anticipate downstream work.** If a feature is being built, spawn the tester to write test cases from requirements simultaneously.
7. **MIDI + synth overlap** — if a task involves both MIDI routing and synth engine integration, spawn both Wash and Inara.
8. **Research tasks** — Inara and Wash both handle research in their domains. For broad "what are our options" questions, spawn both.
