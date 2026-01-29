# Contributing to SqncR

## Repository Ground Rules

### Branch Strategy
- **`main`** - production-ready code only
- Feature branches: `feature/your-feature-name`
- Bug fixes: `fix/issue-description`
- Documentation: `docs/what-youre-documenting`

### Code Standards

**Architecture First**
- All changes must align with agentic architecture (Skills → Agents → MCP)
- Device-agnostic design - no hardcoding for specific hardware
- Musical intent drives implementation, not device capabilities

**C# / .NET Standards**
- Use .NET 9+ features
- Nullable reference types enabled
- Async/await throughout
- Value types for performance-critical paths (Note, Interval)
- Record types for immutable data
- OpenTelemetry instrumentation everywhere

**Music Theory Standards**
- Use standard terminology (don't invent new names for scales/chords)
- Document musical reasoning in XML comments
- Theory implementations must be musically accurate
- Add OpenTelemetry traces for decisions

### Commit Messages
```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation only
- `style`: Code style (formatting, no logic change)
- `refactor`: Code restructuring (no feature change)
- `perf`: Performance improvement
- `test`: Adding/updating tests
- `chore`: Maintenance (dependencies, tooling)
- `skill`: New skill implementation
- `agent`: New or updated agent
- `device`: New device profile

**Examples:**
```
feat(skill): add analyze-song skill with web search

Implements skill-analyze-song that can identify songs from
descriptions and extract musical parameters (key, tempo, chords).

Closes #42

---

device(polyend): add Polyend Synth profile

Adds complete device profile for Polyend Synth including:
- 3 engine MIDI channel mapping
- 8-voice polyphony allocation
- Synthesis type capabilities

---

fix(theory): correct Phrygian mode intervals

Was using Locrian intervals by mistake. Now properly uses
1-b2-b3-4-5-b6-b7.
```

### Testing Requirements

**Before PR:**
- [ ] All tests pass (`dotnet test`)
- [ ] New features have tests
- [ ] Music theory functions have correctness tests
- [ ] MIDI output validated (if applicable)
- [ ] OpenTelemetry traces added for new operations
- [ ] Documentation updated

**Testing Philosophy:**
- Unit tests for music theory (scales, chords, progressions)
- Integration tests for skills
- Mock MIDI devices for testing (no hardware required for CI)
- Real hardware testing documented separately

### Pull Request Process

1. **Create feature branch** from `main`
2. **Implement** with frequent, atomic commits
3. **Test** thoroughly (automated + manual if MIDI-related)
4. **Document** in relevant .md files
5. **PR to `main`** with clear description
6. **Review** - at least one approval required
7. **Squash and merge** (keep history clean)

### PR Template
```markdown
## What does this PR do?
Brief description of changes

## Type of Change
- [ ] New feature
- [ ] Bug fix
- [ ] Documentation
- [ ] Refactoring
- [ ] Device profile
- [ ] Skill
- [ ] Agent

## Testing
How was this tested?

## Musical Impact
How does this affect music generation? (if applicable)

## Checklist
- [ ] Tests pass
- [ ] Documentation updated
- [ ] No breaking changes (or documented if necessary)
- [ ] Follows architecture guidelines
```

### Documentation Standards

**Keep docs up-to-date:**
- `README.md` - getting started, installation, usage (update frequently)
- `CONCEPT.md` - high-level vision (rarely changes)
- `ARCHITECTURE.md` - system architecture (update when structure changes)
- `AGENTIC_ARCHITECTURE.md` - skills/agents/MCP details (update when adding new components)
- `MUSIC_THEORY.md` - theory concepts and workflows (update with new musical capabilities)
- `DOCS_INDEX.md` - documentation index (**update with every doc change**)

**Documentation Index Requirement:**
- **CRITICAL**: Update `DOCS_INDEX.md` whenever you create, modify, or delete documentation
- Add new documents with brief description
- Update "Last Updated" timestamp
- Link to relevant external resources
- Keep project status current

**Linking Standards:**
- Link to external resources for:
  - Device manufacturer pages (Polyend, Moog, etc.)
  - Music theory concepts (scales, modes, harmony)
  - Technical specifications (MIDI implementation charts)
  - Development tools and libraries
- Use descriptive link text (not "click here")
- Verify links are current and accessible
- Link to specific sections in long documents using anchors

**Diagram Standards:**
- **Always use Mermaid diagrams** for architecture, flows, and visualizations
- **Never use ASCII art** for diagrams
- Mermaid is rendered natively in GitHub and most markdown viewers
- Supported diagram types:
  - `flowchart` - System architecture, process flows
  - `sequenceDiagram` - Interactions, message flows
  - `stateDiagram-v2` - State machines, agent states
  - `classDiagram` - Type hierarchies, relationships
  - `graph` - Simple node/edge diagrams
- Example:
  ```mermaid
  flowchart TD
      User[User] --> AI[AI Assistant]
      AI --> MCP[MCP Server]
      MCP --> MIDI[MIDI Service]
      MIDI --> Device[Hardware Synth]
  ```

**Code Documentation:**
- JSDoc for all public functions/classes
- Inline comments for complex musical algorithms
- README in each major directory

### Issue Management

**Issue Labels:**
- `enhancement` - new feature
- `bug` - something broken
- `documentation` - docs improvement
- `good first issue` - great for new contributors
- `skill` - new skill needed
- `agent` - agent implementation/improvement
- `device` - device profile needed
- `theory` - music theory related
- `performance` - latency/optimization
- `question` - discussion/clarification

**Issue Template:**
```markdown
### Description
Clear description of the issue/feature

### Type
- [ ] Bug
- [ ] Feature Request
- [ ] Device Profile Request
- [ ] Documentation

### Musical Context (if applicable)
How does this relate to music generation?

### Proposed Solution (if you have one)
Your thoughts on implementation

### Related Components
- [ ] Skills
- [ ] Agents
- [ ] MCP Server
- [ ] Device Profiles
- [ ] Music Theory
```

### Development Workflow

**Local Setup:**
```bash
# Clone repo
git clone https://github.com/bradygaster/SqncR.git
cd SqncR

# Install .NET 9 SDK
# https://dotnet.microsoft.com/download

# Restore dependencies
dotnet restore

# Configure your devices
cp appsettings.example.json appsettings.json
# Edit with your MIDI devices

# Run tests
dotnet test

# Start with Aspire (launches all services + dashboard)
cd src/SqncR.AppHost
dotnet run

# Aspire Dashboard opens at http://localhost:15888
```

**Adding a New Skill:**
1. Create skill in `src/skills/`
2. Add schema to `schemas/skills/`
3. Document in `AGENTIC_ARCHITECTURE.md`
4. Add tests in `test/skills/`
5. Update MCP server tool registry

**Adding a New Device Profile:**
1. Create profile in `src/devices/profiles/`
2. Follow `DeviceProfile` interface
3. Add to device registry
4. Document MIDI implementation notes
5. Add example configuration

**Adding a New Agent:**
1. Create agent in `src/agents/`
2. Implement state management
3. Define autonomy boundaries
4. Document in `AGENTIC_ARCHITECTURE.md`
5. Add integration tests

### Code Review Guidelines

**Reviewers look for:**
- ✅ Follows agentic architecture
- ✅ Device-agnostic (no hardcoded device names in core logic)
- ✅ Musical accuracy (theory is correct)
- ✅ Tests included and passing
- ✅ Documentation updated
- ✅ No breaking changes without discussion
- ✅ Performance considerations (especially MIDI latency)
- ✅ Error handling (graceful failures)

**What gets rejected:**
- ❌ Device-specific logic in core/agents
- ❌ Music theory errors
- ❌ Breaking changes without approval
- ❌ No tests for new features
- ❌ Hardcoded magic numbers (use constants)
- ❌ Poor error messages
- ❌ Undocumented complexity

### Release Process

**Versioning:** Semantic Versioning (semver)
- MAJOR: Breaking changes
- MINOR: New features (backwards compatible)
- PATCH: Bug fixes

**Release Checklist:**
- [ ] All tests pass
- [ ] Documentation up-to-date
- [ ] CHANGELOG.md updated
- [ ] Version bumped in package.json
- [ ] Tag created: `v1.2.3`
- [ ] Release notes written

### Community & Support

**Communication:**
- GitHub Issues for bugs/features
- Discussions for questions/ideas
- PRs for code contributions

**Be Respectful:**
- This is a creative tool for musicians
- Musical preferences vary - be inclusive
- Constructive feedback only
- Help newcomers learn the architecture

### Musical Philosophy

**Remember:**
- SqncR is for **organic, generative music**
- Focus on **musical intelligence**, not just patterns
- **User intent** drives everything
- **Device-agnostic** - works with any MIDI setup
- **AI-native** - conversation is the interface

### Questions?

Open a Discussion or Issue. Let's build something amazing together.

---

**Thank you for contributing to SqncR!** 🎹🎵
