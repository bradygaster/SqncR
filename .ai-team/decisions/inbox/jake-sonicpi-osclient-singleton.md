# Decision: Sonic Pi OscClient as Singleton in MCP Server DI

**Date:** 2026-02-16
**Author:** Jake (Core Dev)
**Issue:** #15

## Context

The MCP server needs to communicate with Sonic Pi via OSC. `OscClient` wraps a `UdpClient` and sends messages to `localhost:4560`.

## Decision

Register `OscClient` as a singleton in the DI container with default constructor (port 4560, host 127.0.0.1). All Sonic Pi MCP tools receive it via parameter injection, matching the established pattern (e.g., `MidiService`, `GenerationEngine`).

## Rationale

- **Singleton** because `OscClient` holds a `UdpClient` — creating per-request would waste sockets and leak if not disposed.
- **Default port 4560** is Sonic Pi's standard OSC listen port. Configuration override can be added later if needed.
- **No factory/options pattern** — keeps it simple for now. If we need configurable ports (e.g., multiple Sonic Pi instances), we can add `IOptions<SonicPiOptions>` later.
- **FX chain defaults to mix: 0.5** — a reasonable middle ground. Individual mix control can be added as a future enhancement.

## Status

Implemented and merged into MCP server.
