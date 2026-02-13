# Decision: Stability Architecture for Long-Running Sessions

**Author:** BMO  
**Date:** 2026-02-15  
**Status:** Implemented  
**Issue:** #24

## Context
SqncR needs to run for hours during streaming without leaks, stuck notes, or crashes. The generation engine previously had no note tracking, no health monitoring, and no error recovery.

## Decision
1. **NoteTracker** — tracks every active note with timestamps. Enforces max polyphony (32) by force-releasing oldest notes. Provides `AllNotesOff()` for panic/cleanup.
2. **HealthMonitor** — rolling-window latency tracking (1000 ticks), missed tick detection (>2x expected duration), memory/uptime reporting. Zero-allocation steady-state via fixed circular buffer.
3. **Error recovery** — all MIDI sends wrapped in try/catch. Failures log and continue. Engine never crashes from a single bad send.
4. **Tick skip-ahead** — when a tick arrives >2x late, skip forward instead of queuing. Prevents cascading latency spirals.
5. **MCP tools** — `get_health` returns JSON snapshot, `all_notes_off` is a panic button.

## Alternatives Considered
- **No polyphony limit**: Risk of note pile-up with buggy generators. Rejected — silent oldest-note-off is safer.
- **External health check service**: Overkill. HealthMonitor lives inside the engine and exposes via MCP.
- **Crash on MIDI failure**: Unacceptable for streaming. Log-and-continue is the right tradeoff.

## Impact
- `GenerationEngine.cs` gains NoteTracker + HealthMonitor fields, try/catch around MIDI sends, tick skip-ahead logic
- `GenerationCommand.cs` gains `AllNotesOff` variant
- New `HealthTool.cs` MCP tool (auto-discovered)
- 14 new tests in `StabilityTests.cs`
