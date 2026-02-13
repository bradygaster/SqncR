# Decision: IMidiOutput interface extraction and MockMidiOutput test double

**By:** Lemongrab (Tester)
**Date:** 2026-02-15
**Issue:** #11 — [M1] MIDI output capture test framework

## What

Extracted `IMidiOutput` interface from `MidiService` in `src/SqncR.Midi/Testing/`. The interface covers `SendNoteOn`, `SendNoteOff`, `AllNotesOff`, and `CurrentDeviceName`. `MidiService` now implements `IMidiOutput` with zero behavioral change. `SequencePlayer` constructor changed from `MidiService` to `IMidiOutput`.

`MockMidiOutput` is a thread-safe test double that captures every MIDI event with relative timing (Stopwatch from first event, ConcurrentQueue for thread safety). It lives alongside the interface in `src/SqncR.Midi/Testing/`.

## Why

- **Testability:** MidiService wraps DryWetMidi hardware access — untestable without real devices. The interface lets tests inject MockMidiOutput instead.
- **CI/CD:** All 20 new tests run without MIDI hardware. No special setup. Pure software.
- **Thread safety:** Generation loop runs async; ConcurrentQueue ensures no data races in event capture.
- **Future work:** Issue #12 (scale-aware validation) and all M1 MIDI tests build on this framework.

## Impact

- `SequencePlayer` constructor signature changed: `MidiService` → `IMidiOutput`. Any code creating a `SequencePlayer` must pass an `IMidiOutput` (MidiService satisfies this).
- CLI `Program.cs` unchanged — `MidiService` implements `IMidiOutput` so `new SequencePlayer(midi)` still works.
- New test project `tests/SqncR.Midi.Tests/` added to solution.

## Files Changed

- `src/SqncR.Midi/Testing/IMidiOutput.cs` — new interface
- `src/SqncR.Midi/Testing/CapturedMidiEvent.cs` — event record + enum
- `src/SqncR.Midi/Testing/MockMidiOutput.cs` — test double
- `src/SqncR.Midi/MidiService.cs` — added `IMidiOutput` implementation
- `src/SqncR.Core/SequencePlayer.cs` — constructor accepts `IMidiOutput`
- `tests/SqncR.Midi.Tests/` — new xUnit test project (20 tests)
- `SqncR.slnx` — added test project
