# Decision: VCV Rack MCP Tool Design

**Agent:** Bubblegum  
**Date:** 2026-02-14  
**Issue:** #17

## Context
Needed to expose VCV Rack patch generation and process management to AI assistants via MCP tools.

## Decision
- All 5 VCV Rack tools live in a single `VcvRackTool.cs` static class (matching `GenerationTool.cs` pattern which groups related tools).
- `generate_patch` does NOT require `VcvRackLauncher` — it only needs `PatchTemplates` and `VcvPatch.SaveAs()`. This keeps patch generation decoupled from process management.
- Template selection uses a simple string switch (`basic`/`ambient`/`bass`) rather than an enum, since MCP tool params are strings and the AI needs to pass them by name.
- Default output path uses `Path.GetTempPath()` so patches can be generated without the user specifying a directory.

## Alternatives Considered
- Separate tool classes per operation (rejected: too many files for 5 related tools)
- Accepting a full patch JSON as input (rejected: too complex for AI; templates are the right abstraction)
