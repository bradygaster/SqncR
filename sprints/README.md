# SqncR Sprint Plans

**Priority-based implementation.** Wow moments first.

---

## Sprint Structure

| Sprint | Priority | Focus | Status |
|--------|----------|-------|--------|
| [P0-play-songs](./P0-play-songs.md) | **First** | Play .sqnc.yaml to hardware | Ready |
| [P1-save-sessions](./P1-save-sessions.md) | 1 | Save/load sessions | Waiting for P0 |
| [P2-music-theory](./P2-music-theory.md) | 2 | Note, Scale, Chord types | Waiting for P1 |
| [P3-first-generation](./P3-first-generation.md) | 3 | Generate music from text | Waiting for P2 |
| [P4-transports](./P4-transports.md) | 4 | CLI polish, MCP server | Waiting for P3 |
| [P5-production](./P5-production.md) | 5 | Tests, CI/CD, release | Waiting for P4 |

---

## Philosophy

**Wow moments first.**

P0 gets music playing through hardware in ~1 week. Everything else builds on that foundation.

Each sprint depends on the previous one. Complete P0 before starting P1.

---

## What Changed

Previous structure had 7 sequential sprints (sprint_00 through sprint_06) that frontloaded infrastructure before any wow moments.

New structure:
- **P0:** Play files → immediate demo value
- **P1:** Save sessions → persistence
- **P2:** Music theory → unlocks generation
- **P3:** First generation → the real magic
- **P4:** MCP/CLI → talk to Claude
- **P5:** Production → ship it

Previous sprint docs archived at [../docs/archive/sprints-v1/](../docs/archive/sprints-v1/).

---

## Estimated Timeline

| Sprint | Duration | Cumulative |
|--------|----------|------------|
| P0 | ~1 week | Week 1 |
| P1 | ~1 week | Week 2 |
| P2 | ~1-2 weeks | Week 3-4 |
| P3 | ~2 weeks | Week 5-6 |
| P4 | ~2 weeks | Week 7-8 |
| P5 | ~2 weeks | Week 9-10 |

**v1.0 target:** ~10 weeks from start of P0.

---

## How to Use This

1. Start with [P0-play-songs](./P0-play-songs.md)
2. Complete all tasks in order
3. Validate definition of done
4. Move to next sprint

Each sprint doc contains:
- Goal and wow moment
- Tasks with code examples
- Definition of done
- Dependencies on previous sprints

---

## Milestones

### Milestone 1: First Sound (End of P0)
- [ ] `sqncr play` outputs MIDI to hardware
- [ ] Can list devices, select by name or index
- [ ] Example files play correctly

### Milestone 2: Persistence (End of P1)
- [ ] Sessions saved to SQLite
- [ ] Load sessions and resume
- [ ] Device preferences remembered

### Milestone 3: Generation (End of P3)
- [ ] Natural language → MIDI output
- [ ] Skill framework operational
- [ ] 6 MVP skills working

### Milestone 4: AI Integration (End of P4)
- [ ] MCP server working
- [ ] Claude Desktop can control playback
- [ ] Interactive REPL mode

### Milestone 5: v1.0 (End of P5)
- [ ] Full test coverage
- [ ] CI/CD pipeline
- [ ] Published packages
- [ ] Documentation complete

---

## Current Sprint

**Active:** None (starting P0)
**Next:** P0 - Play Songs

---

## See Also

- [../docs/000-INDEX.md](../docs/000-INDEX.md) - Documentation index
- [../docs/003-ARCHITECTURE.md](../docs/003-ARCHITECTURE.md) - Architecture details
- [../CONTRIBUTING.md](../CONTRIBUTING.md) - Development guidelines

---

*Last updated: January 29, 2026*
