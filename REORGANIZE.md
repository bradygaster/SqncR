# File Reorganization Script

**Moving documentation to organized structure**

## Current Structure (Root is messy)

```
SqncR/
├── AGENTIC_ARCHITECTURE.md
├── ARCHITECTURE.md
├── CONCEPT.md
├── CONTRIBUTING.md
├── DIAGRAMS.md               ← Split into separate files
├── DOCS_INDEX.md
├── MUSIC_THEORY.md
├── OBSERVABILITY.md
├── README.md                 ← Update for new structure
├── ROADMAP.md
├── SKILLS.md
└── sprints/
    ├── README.md
    ├── sprint_00_foundation.md
    ├── sprint_01_theory-and-midi.md
    ├── sprint_02_core-skills.md
    ├── sprint_03_cli-and-mcp.md
    ├── sprint_04_api-and-sdk.md
    ├── sprint_05_advanced-skills.md
    └── sprint_06_production.md
```

## Target Structure (Clean & Organized)

```
SqncR/
├── README.md                           ← Updated root README
├── .gitignore                          ← Comprehensive gitignore
├── src/                                ← Source code (empty for now)
│   └── README.md                       ← Placeholder
├── docs/                               ← All documentation
│   ├── README.md                       ← Docs index
│   ├── ARCHITECTURE.md
│   ├── AGENTIC_ARCHITECTURE.md
│   ├── CONCEPT.md
│   ├── CONTRIBUTING.md
│   ├── MUSIC_THEORY.md
│   ├── OBSERVABILITY.md
│   ├── ROADMAP.md
│   ├── SKILLS.md
│   ├── diagrams/                       ← Separated diagrams
│   │   ├── README.md
│   │   ├── system-overview.md
│   │   ├── transport-layer.md
│   │   ├── midi-message-flow.md
│   │   ├── skill-execution-flow.md
│   │   ├── agent-state-machines.md
│   │   ├── device-orchestration.md
│   │   └── telemetry-observability.md
│   └── sprints/                        ← Sprint plans
│       ├── README.md
│       ├── sprint_00_foundation.md
│       ├── sprint_01_theory-and-midi.md
│       ├── sprint_02_core-skills.md
│       ├── sprint_03_cli-and-mcp.md
│       ├── sprint_04_api-and-sdk.md
│       ├── sprint_05_advanced-skills.md
│       └── sprint_06_production.md
└── .sqncr/                             ← AI memory system
    ├── README.md
    ├── memory/
    │   ├── architecture.md
    │   ├── conventions.md
    │   ├── patterns.md (to be created)
    │   └── decisions.md (to be created)
    ├── todos/
    │   ├── README.md
    │   ├── current-sprint.md (when started)
    │   └── backlog.md (when started)
    └── commands/
        ├── README.md
        └── templates/ (to be created)
```

## PowerShell Script

```powershell
# SqncR Documentation Reorganization
# Run from: c:\src\SqncR\

Write-Host "🎵 SqncR Repository Reorganization" -ForegroundColor Cyan
Write-Host "==================================`n" -ForegroundColor Cyan

# Step 1: Delete old DIAGRAMS.md (replaced by separate files)
Write-Host "[1/3] Removing consolidated DIAGRAMS.md..." -ForegroundColor Yellow
if (Test-Path "DIAGRAMS.md") {
    Remove-Item "DIAGRAMS.md" -Force
    Write-Host "  ✅ Removed DIAGRAMS.md (now in docs/diagrams/)`n" -ForegroundColor Green
}

# Step 2: Move documentation files to docs/
Write-Host "[2/3] Moving documentation files to docs/..." -ForegroundColor Yellow

$docsToMove = @(
    "ARCHITECTURE.md",
    "AGENTIC_ARCHITECTURE.md",
    "CONCEPT.md",
    "CONTRIBUTING.md",
    "MUSIC_THEORY.md",
    "OBSERVABILITY.md",
    "ROADMAP.md",
    "SKILLS.md"
)

foreach ($doc in $docsToMove) {
    if (Test-Path $doc) {
        Move-Item $doc "docs\" -Force
        Write-Host "  ✅ Moved $doc" -ForegroundColor Green
    }
}

# Step 3: Move sprint plans
Write-Host "`n[3/3] Moving sprint plans to docs/sprints/..." -ForegroundColor Yellow

if (Test-Path "sprints\") {
    Get-ChildItem "sprints\" -Filter "*.md" | ForEach-Object {
        Move-Item $_.FullName "docs\sprints\" -Force
        Write-Host "  ✅ Moved $($_.Name)" -ForegroundColor Green
    }
    
    # Remove old sprints folder
    Remove-Item "sprints\" -Recurse -Force
    Write-Host "  ✅ Removed old sprints/ folder`n" -ForegroundColor Green
}

Write-Host "==================================`n" -ForegroundColor Cyan
Write-Host "✅ Reorganization Complete!" -ForegroundColor Green
Write-Host "`nNext Steps:" -ForegroundColor Cyan
Write-Host "  1. Review the new structure" -ForegroundColor White
Write-Host "  2. Update README.md with new paths" -ForegroundColor White
Write-Host "  3. Update DOCS_INDEX.md with new paths" -ForegroundColor White
Write-Host "  4. Git commit the changes`n" -ForegroundColor White
```

## Manual Steps After Script

### 1. Update Root README.md

Change all documentation links from:
```markdown
[ARCHITECTURE.md](ARCHITECTURE.md)
```

To:
```markdown
[ARCHITECTURE.md](docs/ARCHITECTURE.md)
```

### 2. Update DOCS_INDEX.md

Move to `docs/README.md` and update all paths.

### 3. Update Sprint Files

Update cross-references within sprint files to use new paths:
```markdown
# OLD
[ROADMAP.md](../ROADMAP.md)

# NEW  
[ROADMAP.md](../ROADMAP.md)
```

### 4. Git Commit

```bash
git add .
git commit -m "refactor: reorganize repository structure

- Move all docs to docs/ folder
- Split diagrams into separate files in docs/diagrams/
- Move sprints to docs/sprints/
- Add AI memory system in .sqncr/
- Add slash commands for efficiency
- Update all cross-references"
```

## Benefits

### Before (Root Clutter)
- ❌ 15+ files in root directory
- ❌ Mixed concerns (docs, code, config)
- ❌ Hard for AI to navigate
- ❌ Single large DIAGRAMS.md

### After (Organized)
- ✅ Clean root (README, .gitignore, folders)
- ✅ Docs in `docs/`
- ✅ Source in `src/`  
- ✅ AI memory in `.sqncr/`
- ✅ Separate diagram files
- ✅ Easy to navigate and maintain

---

**Ready to reorganize? Run the PowerShell script above!**

