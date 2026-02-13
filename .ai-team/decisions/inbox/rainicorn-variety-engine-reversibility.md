# Decision: Variety Engine Reversibility Pattern

**By:** Rainicorn  
**Date:** 2025-07-24  
**Issue:** #23

## Context
The VarietyEngine needs to evolve music over time without permanently altering the generation state. Drift behaviors (octave, velocity, register) must return to baseline.

## Decision
All variety behaviors use a countdown pattern: when triggered, they set a drift value and a `MeasuresRemaining` counter. On each measure boundary, the counter decrements. When it hits 0, the drift reverts. This ensures:
- No permanent state mutations
- Predictable behavior windows (4-8 measures for octave, 2-6 for velocity, 1-3 for rests)
- Octave drift and register shift are mutually exclusive to prevent excessive displacement

## Probability Model
- Conservative: 5% per behavior per measure
- Moderate: 15%
- Adventurous: 30%

These are per-behavior, so multiple behaviors can be active simultaneously (except octave drift + register shift).
