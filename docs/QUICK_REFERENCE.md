# SqncR Developer Quick Reference

**Keep this handy while coding**

---

## Before You Start Coding

### 1. Check Documentation Requirements
```bash
# Review the documentation checklist
cat CONTRIBUTING.md | grep -A 50 "Documentation Checklist"
```

### 2. Create Feature Branch
```bash
git checkout main
git pull
git checkout -b feature/your-feature-name
# or: fix/issue-description
# or: docs/what-youre-documenting
```

### 3. Copy Checklists
Copy relevant checklists from CONTRIBUTING.md to track your progress.

---

## While Coding

### Documentation As You Go

**Every time you add/change:**

| Code Change | Update These Docs |
|-------------|-------------------|
| New skill | SKILLS.md, AGENTIC_ARCHITECTURE.md |
| New agent | AGENTIC_ARCHITECTURE.md, DIAGRAMS.md |
| New device profile | AGENTIC_ARCHITECTURE.md, device profile doc |
| Change architecture | ARCHITECTURE.md, DIAGRAMS.md |
| Change flow | DIAGRAMS.md |
| Add API endpoint | API reference (auto-generated) |
| Add CLI command | CLI reference |
| Add MCP tool | MCP tools reference |
| User-visible change | README.md, CHANGELOG.md |
| New doc | DOCS_INDEX.md |

### Quick Commands

```bash
# Run tests
dotnet test

# Build solution
dotnet build

# Run Aspire (when implemented)
cd src/SqncR.AppHost
dotnet run

# Lint markdown
markdownlint '**/*.md' --ignore node_modules

# Check links
markdown-link-check README.md
```

---

## Before Creating PR

### 1. Run Quality Checks
```bash
# Build
dotnet build

# Tests
dotnet test

# Markdown lint
markdownlint '**/*.md' --ignore node_modules

# Link check (check files you changed)
markdown-link-check README.md
markdown-link-check SKILLS.md
```

### 2. Documentation Checklist
```markdown
- [ ] XML comments on public APIs
- [ ] DOCS_INDEX.md updated (if new docs)
- [ ] CHANGELOG.md updated
- [ ] Sprint plan updated (if applicable)
- [ ] Diagrams updated (if flows changed)
- [ ] Examples added (if applicable)
```

### 3. Commit Messages
```bash
# Format:
git commit -m "feat(scope): add new skill for polyrhythm generation

Implemented skill-polyrhythm-generator that creates
mathematically correct polyrhythmic patterns.

- Supports n-against-m patterns
- Calculates sync points
- Includes OpenTelemetry tracing

Closes #123"
```

**Types:** feat, fix, docs, style, refactor, perf, test, chore, skill

### 4. Create PR
- Use PR template in `.github/pull_request_template.md`
- Fill out ALL sections
- Check ALL checkboxes that apply
- Link related issues

---

## Sprint-Specific Documentation

### Active Sprint: [Sprint Number]

**Required Docs This Sprint:**
- [ ] [Doc 1]
- [ ] [Doc 2]

**See:** `sprints/sprint_XX_name.md` for full list

---

## Common Documentation Patterns

### Adding a Skill

1. **Code:** Implement in `src/SqncR.Core/Skills/`
2. **Update SKILLS.md:**
   ```markdown
   ### skill-your-skill
   **Brief description**
   
   **Sample Prompts:**
   - *"user would say this"*
   
   ```yaml
   name: Your Skill
   description: What it does
   inputs: ...
   outputs: ...
   ```
3. **Update AGENTIC_ARCHITECTURE.md** (add to skills list)
4. **Update CHANGELOG.md**
5. **Add XML comments** to code
6. **Write tests**

### Adding a Device Profile

1. **Code:** Implement in `src/SqncR.Midi/Devices/Profiles/`
2. **Create doc:** `docs/devices/your-device.md`
3. **Update AGENTIC_ARCHITECTURE.md** (device profiles section)
4. **Update DOCS_INDEX.md**
5. **Update CHANGELOG.md**
6. **Add links** to manufacturer MIDI docs

### Adding a Diagram

1. **Use Mermaid** (no ASCII art)
2. **Add to DIAGRAMS.md** (or appropriate doc)
3. **Cross-reference** from related docs
4. **Add "See Also"** section

---

## Definition of Done

**Every task must have:**
- [ ] Code implemented and tested
- [ ] XML comments on public APIs
- [ ] Unit tests passing
- [ ] Integration tests passing (if applicable)
- [ ] Documentation updated
- [ ] CHANGELOG.md entry added
- [ ] PR created and reviewed
- [ ] Aspire Dashboard traces verified (when applicable)

---

## Tech Debt

**If you must defer documentation:**

1. Add entry to `docs/TECHNICAL_DEBT.md`
2. Create GitHub issue with label `tech-debt`
3. Link in PR description
4. Set target sprint

**Never defer:**
- XML comments on public APIs
- CHANGELOG.md entry
- Critical architecture docs

---

## Help & Resources

**Stuck on documentation?**
- Review similar existing docs for patterns
- Check CONTRIBUTING.md for guidelines
- Ask in PR for feedback

**Tools Not Working?**
```bash
# Reinstall markdownlint
npm install -g markdownlint-cli

# Reinstall link checker
npm install -g markdown-link-check
```

**Questions?**
- Check DOCS_INDEX.md for all documentation
- Review CONTRIBUTING.md for processes
- See ROADMAP.md for implementation plan

---

**Last Updated:** January 29, 2026
