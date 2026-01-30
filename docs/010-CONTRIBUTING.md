# Contributing to SqncR

How to help build SqncR.

---

## Getting Started

1. Clone the repo
2. Read [001-QUICKSTART](./001-QUICKSTART.md)
3. Run `dotnet build`
4. Pick a task from current sprint

---

## Development Flow

```bash
# Create branch
git checkout -b feature/my-feature

# Make changes
dotnet build
dotnet test

# Commit
git commit -m "feat: add my feature"

# Push and PR
git push origin feature/my-feature
```

---

## Commit Messages

Format: `type: description`

Types:
- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation
- `refactor:` - Code restructure
- `test:` - Adding tests

---

## Code Style

- C# conventions (PascalCase for public, camelCase for private)
- Nullable reference types enabled
- Async all the way down
- XML doc comments on public APIs

---

## Pull Requests

- One feature per PR
- Tests for new code
- Update docs if needed
- Reference sprint task if applicable

---

## Questions?

Open an issue or check [../CONTRIBUTING.md](../CONTRIBUTING.md) for full guidelines.
