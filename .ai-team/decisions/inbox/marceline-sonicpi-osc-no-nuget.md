# Decision: Sonic Pi OSC Protocol — No External NuGet

**By:** Marceline
**Date:** 2026-02-15
**Status:** Implemented
**Issue:** #14

## What
The Sonic Pi OSC integration uses raw UDP + manual OSC byte encoding rather than an external NuGet package. The `OscMessage` class is ~65 lines of code implementing just enough of OSC 1.0 to send string-argument messages.

## Why
- Sonic Pi only needs two OSC messages: `/run-code` (two string args: GUID + Ruby code) and `/stop-all-jobs` (no args).
- The OSC 1.0 wire format for strings is trivial: null-terminated, 4-byte-aligned ASCII.
- Adding a NuGet dependency for this would be over-engineering. Zero external dependencies keeps the project lean.
- If we later need richer OSC (e.g., for SuperCollider integration), we can revisit.

## Impact
- `SqncR.SonicPi` has zero NuGet dependencies (only references `SqncR.Core`).
- The `OscMessage` class is `internal` — implementation detail, not part of the public API.
- Anyone adding new OSC endpoints just adds a method to `OscMessage`.
