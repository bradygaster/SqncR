# Decision: Spectral Analysis Uses MathNet.Numerics with Parabolic Interpolation

**Date:** 2026-02-14
**Author:** Peppermint Butler (Audio/MIDI Test Engineer)
**Issue:** #19

## Context

Needed FFT-based frequency detection for audio test assertions. Chose `MathNet.Numerics` for FFT (well-maintained, no native dependencies, CI-friendly). Applied Hanning window before FFT to reduce spectral leakage. Used parabolic interpolation on magnitude peaks for sub-bin frequency resolution.

## Decision

- **Library:** `MathNet.Numerics 5.0.0` — pure .NET, no native deps, runs everywhere without hardware
- **Window function:** Hanning — good general-purpose choice for music signals
- **Peak detection:** Local maxima with parabolic interpolation for better frequency accuracy
- **Default tolerance:** ±5% — works well for single tones and chords at 44100 Hz / 1s duration
- **Location:** `src/SqncR.Testing/` — shared library, NOT a test project. Test projects reference it.
- **Assertions:** Throw `Xunit.Sdk.XunitException` with descriptive messages including detected peaks

## Impact

All future audio integration tests (M2, M3, M4) should reference `SqncR.Testing` for spectral analysis. The `AudioAssertions` class provides the primary API for test authors. `ToneGenerator` is useful for self-testing the analysis pipeline without real audio hardware.
