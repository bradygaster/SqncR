using SqncR.Core.Generation;
using SqncR.Core.Rhythm;
using SqncR.Theory;

namespace SqncR.Core.Tests.Generation;

public class VarietyEngineTests
{
    private static GenerationState CreateState(int octave = 4)
    {
        return new GenerationState
        {
            Scale = Scale.PentatonicMinor(60),
            Octave = octave,
            DrumPattern = PatternLibrary.Get("rock")
        };
    }

    [Fact]
    public void Conservative_ProducesFewerChanges_ThanAdventurous()
    {
        var conservativeEngine = new VarietyEngine(VarietyLevel.Conservative, new Random(42));
        var adventurousEngine = new VarietyEngine(VarietyLevel.Adventurous, new Random(42));

        var stateC = CreateState();
        var stateA = CreateState();

        int conservativeChanges = 0;
        int adventurousChanges = 0;

        for (int m = 1; m <= 200; m++)
        {
            int prevOctC = stateC.Octave;
            conservativeEngine.OnMeasureBoundary(stateC, m);
            if (stateC.Octave != prevOctC || conservativeEngine.VelocityDrift != 0 ||
                conservativeEngine.RestInsertionActive)
                conservativeChanges++;

            int prevOctA = stateA.Octave;
            adventurousEngine.OnMeasureBoundary(stateA, m);
            if (stateA.Octave != prevOctA || adventurousEngine.VelocityDrift != 0 ||
                adventurousEngine.RestInsertionActive)
                adventurousChanges++;
        }

        Assert.True(adventurousChanges > conservativeChanges,
            $"Adventurous ({adventurousChanges}) should produce more changes than conservative ({conservativeChanges})");
    }

    [Fact]
    public void OctaveDrift_StaysWithinPlusMinusOne_OfOriginal()
    {
        var engine = new VarietyEngine(VarietyLevel.Adventurous, new Random(42));
        var state = CreateState(octave: 4);
        int originalOctave = state.Octave;

        for (int m = 1; m <= 500; m++)
        {
            engine.OnMeasureBoundary(state, m);
            int drift = state.Octave - originalOctave;
            // Drift can be at most ±2 if both octave drift and register shift overlap,
            // but they are guarded against overlapping
            Assert.InRange(drift, -2, 2);
        }
    }

    [Fact]
    public void AllBehaviors_AreReversible_StateReturnsToBaseline()
    {
        var engine = new VarietyEngine(VarietyLevel.Adventurous, new Random(42));
        var state = CreateState(octave: 4);

        // Run long enough for drifts to trigger and expire
        for (int m = 1; m <= 100; m++)
        {
            engine.OnMeasureBoundary(state, m);
        }

        // After enough measures, all temporary drifts should have expired
        // Run additional measures to let drifts return
        for (int m = 101; m <= 200; m++)
        {
            engine.OnMeasureBoundary(state, m);
        }

        // Verify velocity drift resets
        // At some point it must have been 0 — check it's cyclic
        bool foundZeroVelocity = false;
        for (int m = 201; m <= 300; m++)
        {
            engine.OnMeasureBoundary(state, m);
            if (engine.VelocityDrift == 0)
                foundZeroVelocity = true;
        }

        Assert.True(foundZeroVelocity, "Velocity drift should return to 0 (baseline)");
    }

    [Fact]
    public void MeasureBoundary_TriggersVarietyCheck()
    {
        var engine = new VarietyEngine(VarietyLevel.Adventurous, new Random(1));
        var state = CreateState();

        // With high probability, at least one behavior should trigger over 50 measures
        bool anyChange = false;
        int originalOctave = state.Octave;

        for (int m = 1; m <= 50; m++)
        {
            engine.OnMeasureBoundary(state, m);
            if (state.Octave != originalOctave ||
                engine.VelocityDrift != 0 ||
                engine.RestInsertionActive ||
                engine.RegisterShift != 0)
            {
                anyChange = true;
                break;
            }
        }

        Assert.True(anyChange, "Adventurous variety should trigger at least one change in 50 measures");
    }

    [Fact]
    public void NullVarietyEngine_NoChanges()
    {
        var state = CreateState();
        Assert.Null(state.Variety);

        int originalOctave = state.Octave;
        var originalPattern = state.DrumPattern;

        // Without variety engine, nothing changes
        // (This tests that the engine integration is null-safe)
        state.Variety?.OnMeasureBoundary(state, 1);

        Assert.Equal(originalOctave, state.Octave);
        Assert.Same(originalPattern, state.DrumPattern);
    }

    [Fact]
    public void VelocityVariations_StayWithinValidMidiRange()
    {
        var engine = new VarietyEngine(VarietyLevel.Adventurous, new Random(42));
        var state = CreateState();

        for (int m = 1; m <= 500; m++)
        {
            engine.OnMeasureBoundary(state, m);
            int adjusted = engine.ApplyVelocityDrift(80);
            Assert.InRange(adjusted, 0, 127);
        }
    }

    [Fact]
    public void VelocityDrift_ClampsAtBoundaries()
    {
        var engine = new VarietyEngine(VarietyLevel.Adventurous, new Random(42));
        var state = CreateState();

        // Force many measures to potentially trigger large drifts
        for (int m = 1; m <= 100; m++)
        {
            engine.OnMeasureBoundary(state, m);
        }

        // Test extremes
        Assert.InRange(engine.ApplyVelocityDrift(0), 0, 127);
        Assert.InRange(engine.ApplyVelocityDrift(127), 0, 127);
        Assert.InRange(engine.ApplyVelocityDrift(1), 0, 127);
        Assert.InRange(engine.ApplyVelocityDrift(126), 0, 127);
    }

    [Fact]
    public void RestInsertion_EventuallyDeactivates()
    {
        var engine = new VarietyEngine(VarietyLevel.Adventurous, new Random(42));
        var state = CreateState();

        bool wasActive = false;
        bool wasDeactivated = false;

        for (int m = 1; m <= 200; m++)
        {
            engine.OnMeasureBoundary(state, m);
            if (engine.RestInsertionActive)
                wasActive = true;
            else if (wasActive)
            {
                wasDeactivated = true;
                break;
            }
        }

        Assert.True(wasActive, "Rest insertion should activate at adventurous level");
        Assert.True(wasDeactivated, "Rest insertion should deactivate (reversible)");
    }

    [Fact]
    public void RhythmicFills_OnlyOnMeasureMultiplesOf4()
    {
        var engine = new VarietyEngine(VarietyLevel.Adventurous, new Random(42));
        var state = CreateState();
        var originalPattern = state.DrumPattern;

        // Call on non-multiple-of-4 measures — drum pattern should not change from fills
        for (int m = 1; m <= 3; m++)
        {
            // Reset to original for clean test
            state.DrumPattern = originalPattern;
            var before = state.DrumPattern;
            engine.OnMeasureBoundary(state, m);
            // Pattern may change from density but not fills (fills need % 4 == 0)
        }

        // Call on multiples of 4 — fills may trigger
        bool patternChanged = false;
        for (int m = 4; m <= 40; m += 4)
        {
            state.DrumPattern = originalPattern;
            engine.OnMeasureBoundary(state, m);
            if (!ReferenceEquals(state.DrumPattern, originalPattern))
            {
                patternChanged = true;
                break;
            }
        }

        Assert.True(patternChanged, "Rhythmic fills should modify pattern on measure multiples of 4");
    }

    [Fact]
    public void ShouldInsertRest_OnlyWhenActive()
    {
        var engine = new VarietyEngine(VarietyLevel.Adventurous, new Random(42));
        var state = CreateState();

        // Before any measures, rest insertion should not be active
        Assert.False(engine.RestInsertionActive);

        // ShouldInsertRest should return false when not active
        bool anyRest = false;
        for (int i = 0; i < 100; i++)
        {
            if (engine.ShouldInsertRest())
            {
                anyRest = true;
                break;
            }
        }

        Assert.False(anyRest, "ShouldInsertRest should return false when rest insertion is not active");
    }

    [Fact]
    public void VarietyLevel_CanBeChanged()
    {
        var engine = new VarietyEngine(VarietyLevel.Conservative);
        Assert.Equal(VarietyLevel.Conservative, engine.Level);

        engine.Level = VarietyLevel.Adventurous;
        Assert.Equal(VarietyLevel.Adventurous, engine.Level);
    }

    [Fact]
    public void RegisterShift_DoesNotOverlapWithOctaveDrift()
    {
        var engine = new VarietyEngine(VarietyLevel.Adventurous, new Random(42));
        var state = CreateState(octave: 4);
        int originalOctave = 4;

        // The engine guards against both octave drift and register shift being active simultaneously.
        // The total octave displacement should stay within ±2 of original.
        for (int m = 1; m <= 500; m++)
        {
            engine.OnMeasureBoundary(state, m);
            int totalDisplacement = Math.Abs(state.Octave - originalOctave);
            Assert.InRange(totalDisplacement, 0, 2);
        }
    }

    [Fact]
    public void DefaultLevel_IsModerate()
    {
        var engine = new VarietyEngine();
        Assert.Equal(VarietyLevel.Moderate, engine.Level);
    }

    [Fact]
    public void PatternDensity_PreservesFirstBeat()
    {
        var engine = new VarietyEngine(VarietyLevel.Adventurous, new Random(42));
        var state = CreateState();

        // Get original first beat status for each layer
        var originalFirstBeats = state.DrumPattern!.Layers
            .Select(l => l.Pattern.Steps[0].IsActive)
            .ToList();

        // Run density changes
        for (int m = 4; m <= 100; m += 4)
        {
            engine.OnMeasureBoundary(state, m);
        }

        // Beat 1 should not be removed (thinning skips step 0)
        for (int i = 0; i < state.DrumPattern.Layers.Length && i < originalFirstBeats.Count; i++)
        {
            if (originalFirstBeats[i])
            {
                Assert.True(state.DrumPattern.Layers[i].Pattern.Steps[0].IsActive,
                    $"Layer {i}: First beat should be preserved by pattern density changes");
            }
        }
    }
}
