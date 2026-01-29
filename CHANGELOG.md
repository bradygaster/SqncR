# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Documentation infrastructure and planning phase complete
- Comprehensive architecture documentation
- Visual architecture guide with Mermaid diagrams
- 43+ skills catalog
- 6 sprint plans (15 weeks to v1.0)

## [0.1.0] - 2026-01-29

### Added
- Initial repository setup
- Complete documentation package:
  - README.md - Project overview
  - CONCEPT.md - Vision and philosophy
  - ARCHITECTURE.md - System design
  - DIAGRAMS.md - Visual architecture guide
  - AGENTIC_ARCHITECTURE.md - Skills and agents
  - MUSIC_THEORY.md - Music theory foundation
  - OBSERVABILITY.md - Telemetry strategy
  - SKILLS.md - 43+ skills catalog
  - ROADMAP.md - Implementation roadmap
  - CONTRIBUTING.md - Development guidelines
  - DOCS_INDEX.md - Documentation index
- Sprint plans (Sprint 00-06)
- GitHub issue templates (bug, feature, device)
- Pull request template
- Private repository configuration

### Documentation
- All documentation follows Mermaid diagram standard (no ASCII art)
- External links throughout documentation
- Cross-references between documents
- Sprint-based implementation plan

---

## How to Update This File

### For Every Merge to Main:

Add an entry under `[Unreleased]` in the appropriate section:

**Sections:**
- **Added** - New features, skills, agents, device profiles
- **Changed** - Changes to existing functionality
- **Deprecated** - Soon-to-be removed features
- **Removed** - Removed features
- **Fixed** - Bug fixes
- **Security** - Security fixes

**Example Entry:**
```markdown
### Added
- skill-polyrhythm-generator for creating polyrhythmic patterns
- Polyend MESS device profile with glitch automation support
```

### For Release:

When cutting a release, move everything from `[Unreleased]` to a new version section:

```markdown
## [0.2.0] - 2026-02-15

### Added
- [moved from Unreleased]

### Changed
- [moved from Unreleased]

## [Unreleased]
[empty until next changes]
```

---

## Version Numbers

We follow [Semantic Versioning](https://semver.org/):

- **Major** (1.0.0) - Breaking changes, incompatible API changes
- **Minor** (0.1.0) - New features, backward compatible
- **Patch** (0.0.1) - Bug fixes, backward compatible

**Pre-releases:**
- `-alpha` - Early development, incomplete features
- `-beta` - Feature complete, testing phase
- `-rc.1` - Release candidate

**Examples:**
- `0.1.0-alpha` - Initial development
- `0.5.0-beta` - Feature complete, in testing
- `1.0.0-rc.1` - Release candidate 1
- `1.0.0` - Production release

---

[unreleased]: https://github.com/bradygaster/SqncR/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/bradygaster/SqncR/releases/tag/v0.1.0
