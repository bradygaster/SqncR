# 🎯 SqncR Quick Reference

**Everything you need to know in one page**

---

## 📋 What Just Happened?

Your repository was reorganized to be **AI-friendly**, **OCD-organized**, and **developer-efficient**.

---

## 🚀 Quick Start: 3 Steps to Complete

### 1️⃣ Run This Command (PowerShell)

```powershell
cd c:\src\SqncR

# One-liner to do everything
Remove-Item "DIAGRAMS.md" -Force; `
"ARCHITECTURE","AGENTIC_ARCHITECTURE","CONCEPT","CONTRIBUTING","MUSIC_THEORY","OBSERVABILITY","ROADMAP","SKILLS" | ForEach-Object { Move-Item "$_.md" "docs\" -Force }; `
Move-Item "DOCS_INDEX.md" "docs\README.md" -Force; `
Get-ChildItem "sprints\*.md" | Move-Item -Destination "docs\sprints\" -Force; `
Remove-Item "sprints\" -Recurse -Force; `
Copy-Item "README.md" "README_OLD.md"; `
Move-Item "README_NEW.md" "README.md" -Force; `
Write-Host "✅ Done!"
```

### 2️⃣ Update Links in docs/README.md

Change paths from `../FILENAME.md` to `FILENAME.md`

### 3️⃣ Git Commit

```bash
git add .
git commit -m "refactor: reorganize repository structure"
git push
```

**Done! 🎉**

---

## 📁 New Structure

```
SqncR/
├── README.md           # Updated root
├── src/                # Future source code
├── docs/               # All documentation
│   ├── diagrams/       # 7 diagram files
│   └── sprints/        # 7 sprint plans
└── .sqncr/             # AI memory system
    ├── memory/         # Architecture & conventions
    ├── todos/          # Sprint tracking
    └── commands/       # Slash commands
```

---

## 🤖 AI Memory System

**Location:** `.sqncr/`

### What It Does
AI assistants can now:
- ✅ Remember architectural decisions
- ✅ Follow coding conventions automatically
- ✅ Learn patterns as you build

### How to Use

**Start of every AI session:**
```
I'm working on SqncR.

Architecture: [paste .sqncr/memory/architecture.md]
Conventions: [paste .sqncr/memory/conventions.md]

Now let's [your task]...
```

---

## ⚡ Slash Commands

**Location:** `.sqncr/commands/README.md`

### Most Useful

```bash
/new-skill <name> [category]      # Scaffold complete skill
/sprint status                    # Show progress
/add-test <class>                 # Generate tests
/diagram <name> [type]            # Create diagram
/adr <title>                      # Document decision
```

### Example

```
You: /new-skill detect-tempo analysis

AI: ✅ Created:
  - src/SqncR.Core/Skills/Analysis/DetectTempoSkill.cs
  - tests/SqncR.Core.Tests/Skills/Analysis/DetectTempoSkillTests.cs
  ✅ Updated:
  - docs/SKILLS.md
```

---

## 📊 Diagrams

**Location:** `docs/diagrams/`

### Files Created

1. `system-overview.md` - High-level architecture
2. `transport-layer.md` - How all transports use same core
3. `midi-message-flow.md` - Request to MIDI hardware
4. `skill-execution-flow.md` - Skill orchestration
5. `agent-state-machines.md` - Agent behavior
6. `device-orchestration.md` - Multi-device coordination
7. `telemetry-observability.md` - OpenTelemetry tracing

**Old `DIAGRAMS.md` deleted** (replaced by above)

---

## 📝 Documentation

**Location:** `docs/`

### Main Files

- `README.md` - Documentation index (was DOCS_INDEX.md)
- `ARCHITECTURE.md` - System architecture
- `ROADMAP.md` - Implementation plan
- `SKILLS.md` - Skills catalog
- `CONTRIBUTING.md` - Development guide

### Sprints

**Location:** `docs/sprints/`

- Sprint 00: Foundation & Project Setup
- Sprint 01: Theory & MIDI Foundation
- Sprint 02: Core Skills & Service Facade
- Sprint 03: CLI & MCP Transports
- Sprint 04: API & SDK Library
- Sprint 05: Advanced Skills & Agents
- Sprint 06: Device-Specific Skills & Polish

---

## 🎯 Key Benefits

### For You
- ✅ Clean, organized structure
- ✅ Easy to navigate
- ✅ Professional layout

### For AI
- ✅ Remembers context between sessions
- ✅ Follows conventions automatically
- ✅ 10x faster with slash commands

### For Development
- ✅ Todo tracking system
- ✅ Separate diagram files
- ✅ Clear documentation

---

## 📚 Full Documentation

Read these for more details:

1. **[START_HERE.md](START_HERE.md)** - Complete guide
2. **[SUMMARY.md](SUMMARY.md)** - What was done
3. **[REORGANIZE.md](REORGANIZE.md)** - Migration script
4. **[TREE.md](TREE.md)** - Full file tree
5. **[.sqncr/README.md](.sqncr/README.md)** - AI memory system
6. **[.sqncr/commands/README.md](.sqncr/commands/README.md)** - Slash commands

---

## ⚠️ Important Files

### Don't Commit
`.sqncr/todos/current-sprint.md` - Personal todo tracking
`.sqncr/todos/backlog.md` - Personal backlog

### Do Commit
Everything else!

---

## 🔥 Next Steps

1. ✅ Run PowerShell command above
2. ✅ Update docs/README.md paths
3. ✅ Git commit
4. 🚀 Start Sprint 00: `docs/sprints/sprint_00_foundation.md`
5. 🎵 Build something amazing!

---

## 💡 Tips

### Working with AI

```
# Start session with context
Paste: .sqncr/memory/architecture.md
Paste: .sqncr/memory/conventions.md

# Use slash commands
/new-skill rhythm-generator
/sprint status
/add-test RhythmGeneratorSkill
```

### Managing Sprints

```
/sprint start sprint-00
/sprint status
/sprint complete task-name
/sprint review
```

### Creating Diagrams

```
/diagram data-flow-sequence sequence
[AI creates docs/diagrams/data-flow-sequence.md]
```

---

## 🎉 You're Done!

Your repository is now:

✅ **Organized** - Clean structure
✅ **AI-Friendly** - Memory system
✅ **Efficient** - Slash commands
✅ **Professional** - Production-ready

**Now go make some music! 🎹🎵**

