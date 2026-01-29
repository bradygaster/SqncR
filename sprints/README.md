# SqncR Sprint Plans

**Implementation broken into 6 focused sprints**

---

## Sprint Overview

| Sprint | Duration | Focus | Status |
|--------|----------|-------|--------|
| [Sprint 00](sprint_00_foundation.md) | 2 weeks | Foundation & Project Setup | 🔲 Not Started |
| [Sprint 01](sprint_01_theory-and-midi.md) | 2 weeks | Music Theory & MIDI Foundation | 🔲 Not Started |
| [Sprint 02](sprint_02_core-skills.md) | 2 weeks | Core Skills & Service Facade | 🔲 Not Started |
| [Sprint 03](sprint_03_cli-and-mcp.md) | 2 weeks | CLI & MCP Transports | 🔲 Not Started |
| [Sprint 04](sprint_04_api-and-sdk.md) | 2 weeks | REST API & SDK Library | 🔲 Not Started |
| [Sprint 05](sprint_05_advanced-skills.md) | 3 weeks | Advanced Skills & Agents | 🔲 Not Started |
| [Sprint 06](sprint_06_production.md) | 2 weeks | Device-Specific Skills & Polish | 🔲 Not Started |

**Total Duration:** ~15 weeks to v1.0

---

## Sprint 00: Foundation & Project Setup
**Goal:** Initialize .NET solution with Aspire

**Key Deliverables:**
- .NET 9 solution with all projects
- Aspire AppHost running
- OpenTelemetry configured
- Development workflow established

**[View Sprint Plan →](sprint_00_foundation.md)**

---

## Sprint 01: Music Theory & MIDI Foundation
**Goal:** Build core music theory library and MIDI service

**Key Deliverables:**
- Note, Scale, Chord, Interval value types
- MIDI device scanning with DryWetMidi
- Device profile system
- Polyend Synth profile working
- Send MIDI notes with < 10ms latency

**[View Sprint Plan →](sprint_01_theory-and-midi.md)**

---

## Sprint 02: Core Skills & Service Facade
**Goal:** Implement MVP skills and service layer

**Key Deliverables:**
- Skill framework (ISkill, SkillBase, SkillRegistry)
- 6 core skills implemented
- SqncRService facade
- "Generate ambient drone" works end-to-end

**[View Sprint Plan →](sprint_02_core-skills.md)**

---

## Sprint 03: CLI & MCP Transports
**Goal:** Build CLI tool and MCP server

**Key Deliverables:**
- sqncr.exe CLI tool
- MCP server for Claude/Copilot
- Both use same SqncR.Core
- Verified transport independence

**[View Sprint Plan →](sprint_03_cli-and-mcp.md)**

---

## Sprint 04: REST API & SDK Library
**Goal:** Complete all 4 transport layers

**Key Deliverables:**
- REST API with Swagger
- .NET SDK with fluent API
- All 4 transports operational (CLI, MCP, API, SDK)
- Transport independence verified

**[View Sprint Plan →](sprint_04_api-and-sdk.md)**

---

## Sprint 05: Advanced Skills & Agents
**Goal:** Advanced skills and autonomous agents

**Key Deliverables:**
- 15+ advanced skills (analysis, generation, transformation)
- All 4 agents (SessionManager, Composition, Listener, DeviceOrchestrator)
- Complex multi-device workflows
- Real-time jamming capability

**[View Sprint Plan →](sprint_05_advanced-skills.md)**

---

## Sprint 06: Device-Specific Skills & Polish
**Goal:** Production ready with all devices supported

**Key Deliverables:**
- All 6 device profiles complete
- 10+ device-specific skills
- Production packaging (Docker, NuGet, executables)
- Complete documentation
- CI/CD pipeline
- v1.0 Release

**[View Sprint Plan →](sprint_06_production.md)**

---

## Milestones

### Milestone 1: Foundation (End of Sprint 00)
- ✅ .NET solution compiles
- ✅ Aspire Dashboard runs
- ✅ Development workflow established

### Milestone 2: MVP (End of Sprint 02)
- ✅ Music theory library working
- ✅ MIDI I/O working
- ✅ Basic generation functional
- ✅ Observable in dashboard

### Milestone 3: Multi-Transport (End of Sprint 04)
- ✅ All 4 transports operational
- ✅ Same Core used by all
- ✅ Transport independence verified

### Milestone 4: Production (End of Sprint 06)
- ✅ All features complete
- ✅ All devices supported
- ✅ Production packaged
- ✅ Documentation complete
- ✅ v1.0 Released

---

## Working Agreement

**Sprint Duration:** 2 weeks (3 weeks for Sprint 05)

**Sprint Cadence:**
- **Day 1:** Sprint planning, task breakdown
- **Daily:** Standup (async via commits)
- **Day 10:** Mid-sprint check-in
- **Day 14:** Sprint review & demo
- **Day 14:** Retrospective
- **Day 14:** Next sprint planning

**Definition of Done (All Sprints):**
- All tasks complete
- Tests passing
- Code reviewed
- Documentation updated
- Aspire Dashboard shows features working
- Demo prepared

---

## Current Sprint

**Active:** None (planning phase)  
**Next:** Sprint 00 - Foundation & Project Setup

---

## See Also

- [ROADMAP.md](../ROADMAP.md) - Overall implementation roadmap
- [CONTRIBUTING.md](../CONTRIBUTING.md) - Development guidelines
- [ARCHITECTURE.md](../ARCHITECTURE.md) - Architecture details

---

**Updated:** January 29, 2026
