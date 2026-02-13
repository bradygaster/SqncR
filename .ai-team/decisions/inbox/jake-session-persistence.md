# Decision: Session Persistence Architecture

**Date:** 2026-02-17
**Author:** Jake (Core Dev)
**Issue:** #21

## Context
Need save/load for generation sessions so users can resume where they left off.

## Decision
- `SessionState` captures a flat, serializable snapshot of `GenerationState` (tempo, scale name, root note, pattern name, octave, channels, generator name, play state).
- `SessionStore` handles file I/O at `~/.sqncr/sessions/{name}.json` using `System.Text.Json`. Constructor accepts custom directory for testability.
- `SessionState.ApplyTo(GenerationEngine)` restores state by enqueuing commands through the engine's command channel (same pattern as MCP tools). Missing patterns are gracefully skipped.
- `SessionStore` registered as singleton in MCP server DI — stateless, thread-safe, no per-request state.
- Three MCP tools: `save_session`, `load_session`, `list_sessions`.

## Rationale
- Flat JSON with string names (not object references) ensures sessions remain loadable even if scale/pattern implementations change.
- Using engine command channel for restore ensures thread-safe state mutation, consistent with all other MCP tools.
- Constructor injection of directory path enables unit testing with temp directories.

## Alternatives Considered
- SQLite storage: overkill for simple key-value session files.
- Storing full MIDI state: too complex, not needed for resume.
