## What does this PR do?
<!-- Brief description of the changes -->

## Type of Change
- [ ] New feature
- [ ] Bug fix
- [ ] Documentation
- [ ] Refactoring
- [ ] Device profile
- [ ] Skill implementation
- [ ] Agent implementation
- [ ] Music theory enhancement
- [ ] Performance improvement

## Musical Impact
<!-- How does this affect music generation? What's different musically? -->

## Testing
<!-- How was this tested? Include steps if manual testing required -->

- [ ] Unit tests pass (`dotnet test`)
- [ ] Integration tests pass (if applicable)
- [ ] Manual testing completed (describe below)
- [ ] Tested with real MIDI hardware (if applicable)
- [ ] Aspire Dashboard traces verified

### Manual Testing Details
<!-- What did you test manually? What devices? What scenarios? -->

## Breaking Changes
- [ ] No breaking changes
- [ ] Contains breaking changes (describe below)

<!-- If breaking changes, explain what breaks and migration path -->

## Documentation Checklist

### Code Documentation
- [ ] XML comments added to public APIs
- [ ] Complex logic documented
- [ ] Examples provided where helpful

### Architecture Documentation
- [ ] ARCHITECTURE.md updated (if applicable)
- [ ] DIAGRAMS.md updated (if applicable)
- [ ] AGENTIC_ARCHITECTURE.md updated (if applicable)

### User Documentation
- [ ] README.md updated (if applicable)
- [ ] SKILLS.md updated (if applicable)
- [ ] Quick Start Guide updated (if applicable)

### Reference Documentation
- [ ] API Reference updated (if applicable)
- [ ] CLI Reference updated (if applicable)
- [ ] MCP Tools Reference updated (if applicable)

### Always Required
- [ ] DOCS_INDEX.md updated (if new docs added)
- [ ] CHANGELOG.md updated
- [ ] Sprint plan updated (if in active sprint)

### Examples
- [ ] Code examples added/updated (if applicable)
- [ ] Configuration examples added/updated (if applicable)

## Code Quality Checklist
- [ ] Code follows .NET conventions
- [ ] OpenTelemetry instrumentation added
- [ ] Nullable reference types handled correctly
- [ ] Async/await used properly
- [ ] No TODO/HACK comments left in production code
- [ ] Music theory is accurate (if applicable)
- [ ] Device-agnostic (no hardcoded device names in core)
- [ ] Ready for review

## Observability
<!-- What traces/metrics/logs will appear in Aspire Dashboard? -->
- [ ] Activity spans added with meaningful names
- [ ] Relevant tags added to spans
- [ ] Errors properly traced
- [ ] Latency metrics captured (if applicable)

## Related Issues
<!-- Link related issues: Closes #123, Relates to #456 -->

## Additional Context
<!-- Anything else reviewers should know? -->

---

**Review Checklist for Reviewers:**
- [ ] Documentation complete and accurate
- [ ] Tests cover new functionality
- [ ] No obvious bugs or code smells
- [ ] Follows architecture patterns
- [ ] OpenTelemetry properly instrumented
- [ ] CHANGELOG.md entry makes sense

