### Smooth Transitions Use Linear Interpolation and Common-Tone Bridging

**By:** Simon (Music Theorist)
**Date:** 2026-02-15
**Issue:** #22

**What:** Smooth tempo changes use linear interpolation over N bars (default 4). Smooth scale changes allow notes from BOTH old and new scales during the transition window (common-tone prioritization via union of pitch classes), then snap to the target scale after the transition completes. Zero or negative bars = instant change (preserving backward compatibility). New in-progress transitions override any currently running transition from the same point.

**Why:** Live streaming requires smooth musical transitions. Abrupt key/tempo changes break the generative music flow. Linear interpolation is the simplest correct approach for tempo; common-tone bridging is the standard music theory technique for key changes — it avoids dissonant clashes by allowing notes that belong to either scale during the crossfade window.

**Alternatives considered:**
- Exponential/ease-in-out tempo curves: deferred to future (easing parameter exists but defaults to linear)
- Weighted probability during scale transition (favor common tones): adds complexity without clear benefit for v1
- Chromatic passing tones during transition: too adventurous for default behavior

**Files:**
- `src/SqncR.Core/Generation/TransitionEngine.cs` (new)
- `src/SqncR.Core/Generation/GenerationCommand.cs` (SetTempoSmooth, SetScaleSmooth)
- `src/SqncR.Core/Generation/GenerationEngine.cs` (integration)
- `src/SqncR.McpServer/Tools/GenerationTool.cs` (smooth parameter)
- `tests/SqncR.Core.Tests/Generation/TransitionEngineTests.cs` (13 tests)
