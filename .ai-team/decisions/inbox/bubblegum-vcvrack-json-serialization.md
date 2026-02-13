# Decision: VCV Rack Patch Serialization Uses JsonNode API

**Date:** 2026-02-14
**Author:** Bubblegum (VCV Rack Specialist)
**Issue:** #16

## Context
VCV Rack patches require a specific JSON format with nested structures (modules with params arrays, position arrays, cables with port IDs). Attempted source-generated `JsonSerializerContext` but it cannot handle `Dictionary<string, object>` with polymorphic nested values (lists, arrays, dictionaries).

## Decision
Use `System.Text.Json.Nodes` (`JsonObject` / `JsonArray`) for building VCV Rack patch JSON. This avoids reflection-based serialization complexity while giving full control over the output structure.

## Consequences
- Clean, readable code in `VcvPatch.ToJson()`
- No AOT/trimming issues since JsonNode is fully supported
- Other team members working with VCV Rack patches should use the same approach
- Port names in ModuleLibrary are friendly strings mapped to integer port indices — use `PatchBuilder.Cable()` for name-based wiring
