# Decision: Integration Tests Use CI-Safe Patterns Only

**Author:** Peppermint Butler (Audio/MIDI Test Engineer)
**Date:** 2026-02-14
**Issue:** #20

## Context
Integration tests for Sonic Pi and VCV Rack pipelines cannot depend on those applications being installed or running. Tests must be CI-friendly and run without physical hardware or software synths.

## Decision
All 21 integration tests use mock/simulation patterns:
- **Sonic Pi**: Validates generated Ruby code structure (string assertions), not live OSC execution. OscClient tests use ephemeral UDP ports (fire-and-forget).
- **VCV Rack**: Validates patch object graph (modules, cables, signal paths) and JSON serialization, not live Rack processes. VcvRackLauncher tests verify graceful failure when Rack isn't installed.
- **End-to-end**: Uses `MockMidiOutput` to capture MIDI events from `GenerationEngine`, then validates musical correctness (scale membership, frequency accuracy) using `MidiFrequency` and `SpectralAnalyzer` with synthetic tones.

## Consequence
Tests run in <3 seconds total, require no external dependencies, and will not flake in CI. When real Sonic Pi / VCV Rack integration is needed (M3/M4), those tests should be tagged `[Trait("Category", "Hardware")]` and skipped in CI.
