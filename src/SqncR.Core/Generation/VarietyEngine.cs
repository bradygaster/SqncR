using SqncR.Core.Rhythm;

namespace SqncR.Core.Generation;

/// <summary>
/// Controls how aggressively the variety engine mutates the musical output.
/// </summary>
public enum VarietyLevel
{
    Conservative,
    Moderate,
    Adventurous
}

/// <summary>
/// Automatic musical evolution engine. Drifts parameters away from baseline
/// and back again, keeping generative output from becoming repetitive.
/// All mutations are reversible — the engine always returns to baseline.
/// </summary>
public sealed class VarietyEngine
{
    private readonly Random _random;

    /// <summary>Current variety level controlling mutation probability.</summary>
    public VarietyLevel Level { get; set; }

    // Drift state tracking — allows reversibility
    private int _octaveDrift;
    private int _octaveDriftMeasuresRemaining;
    private int _velocityDrift;
    private int _velocityDriftMeasuresRemaining;
    private bool _restInsertionActive;
    private int _restInsertionMeasuresRemaining;
    private int _registerShift;
    private int _registerShiftMeasuresRemaining;

    /// <summary>Current octave drift offset from baseline.</summary>
    public int OctaveDrift => _octaveDrift;

    /// <summary>Current velocity drift offset from baseline.</summary>
    public int VelocityDrift => _velocityDrift;

    /// <summary>Whether rest insertion is currently active.</summary>
    public bool RestInsertionActive => _restInsertionActive;

    /// <summary>Current register shift offset.</summary>
    public int RegisterShift => _registerShift;

    public VarietyEngine(VarietyLevel level = VarietyLevel.Moderate, Random? random = null)
    {
        Level = level;
        _random = random ?? Random.Shared;
    }

    /// <summary>
    /// Called on every measure boundary. May modify state to introduce musical variety.
    /// All changes are reversible — drifts return to baseline after a period.
    /// </summary>
    public void OnMeasureBoundary(GenerationState state, int measureNumber)
    {
        double probability = GetProbability();

        HandleOctaveDrift(state, probability);
        HandleVelocityVariation(state, probability);
        HandleRhythmicFills(state, measureNumber, probability);
        HandleRestInsertion(probability);
        HandlePatternDensity(state, measureNumber, probability);
        HandleRegisterShift(state, probability);
    }

    private double GetProbability() => Level switch
    {
        VarietyLevel.Conservative => 0.05,
        VarietyLevel.Moderate => 0.15,
        VarietyLevel.Adventurous => 0.30,
        _ => 0.15
    };

    private void HandleOctaveDrift(GenerationState state, double probability)
    {
        // Count down active drift
        if (_octaveDriftMeasuresRemaining > 0)
        {
            _octaveDriftMeasuresRemaining--;
            if (_octaveDriftMeasuresRemaining == 0)
            {
                // Revert drift
                state.Octave -= _octaveDrift;
                _octaveDrift = 0;
            }
            return;
        }

        if (_octaveDrift != 0) return;
        if (_random.NextDouble() >= probability) return;

        // Drift up or down by 1 octave
        int direction = _random.Next(2) == 0 ? 1 : -1;
        int newOctave = state.Octave + direction;

        // Keep within reasonable MIDI range (octaves 1-8)
        if (newOctave < 1 || newOctave > 8) return;

        _octaveDrift = direction;
        _octaveDriftMeasuresRemaining = 4 + _random.Next(5); // 4-8 measures
        state.Octave = newOctave;
    }

    private void HandleVelocityVariation(GenerationState state, double probability)
    {
        // Count down active velocity drift
        if (_velocityDriftMeasuresRemaining > 0)
        {
            _velocityDriftMeasuresRemaining--;
            if (_velocityDriftMeasuresRemaining == 0)
            {
                _velocityDrift = 0;
            }
            return;
        }

        if (_velocityDrift != 0) return;
        if (_random.NextDouble() >= probability) return;

        // ±10-20 velocity shift
        int magnitude = 10 + _random.Next(11); // 10-20
        int direction = _random.Next(2) == 0 ? 1 : -1;
        _velocityDrift = direction * magnitude;
        _velocityDriftMeasuresRemaining = 2 + _random.Next(5); // 2-6 measures
    }

    private void HandleRhythmicFills(GenerationState state, int measureNumber, double probability)
    {
        // Only on every 4th or 8th measure
        if (measureNumber == 0) return;
        if (measureNumber % 4 != 0) return;
        if (_random.NextDouble() >= probability) return;
        if (state.DrumPattern == null) return;

        // Add ghost notes to an existing pattern layer
        var layers = state.DrumPattern.Layers;
        if (layers.Length == 0) return;

        // Pick a random layer and add a ghost note on an empty step
        int layerIndex = _random.Next(layers.Length);
        var (voice, pattern) = layers[layerIndex];

        var emptySteps = new List<int>();
        for (int i = 0; i < pattern.StepsPerMeasure; i++)
        {
            if (!pattern.Steps[i].IsActive)
                emptySteps.Add(i);
        }

        if (emptySteps.Count == 0) return;

        int stepToFill = emptySteps[_random.Next(emptySteps.Count)];
        var ghostNote = StepInfo.Hit(velocity: 40 + _random.Next(20), probability: 0.6);
        var newPattern = pattern.WithStep(stepToFill, ghostNote);

        // Rebuild layered pattern with modified layer
        var newLayers = new List<(DrumVoice, BeatPattern)>();
        for (int i = 0; i < layers.Length; i++)
        {
            if (i == layerIndex)
                newLayers.Add((voice, newPattern));
            else
                newLayers.Add(layers[i]);
        }

        state.DrumPattern = new LayeredPattern(state.DrumPattern.Name, newLayers);
    }

    private void HandleRestInsertion(double probability)
    {
        // Count down active rest insertion
        if (_restInsertionMeasuresRemaining > 0)
        {
            _restInsertionMeasuresRemaining--;
            if (_restInsertionMeasuresRemaining == 0)
            {
                _restInsertionActive = false;
            }
            return;
        }

        if (_restInsertionActive) return;
        if (_random.NextDouble() >= probability) return;

        _restInsertionActive = true;
        _restInsertionMeasuresRemaining = 1 + _random.Next(3); // 1-3 measures
    }

    private void HandlePatternDensity(GenerationState state, int measureNumber, double probability)
    {
        if (measureNumber == 0) return;
        if (measureNumber % 4 != 0) return;
        if (_random.NextDouble() >= probability) return;
        if (state.DrumPattern == null) return;

        var layers = state.DrumPattern.Layers;
        if (layers.Length == 0) return;

        // Pick a layer and thin out or fill in
        int layerIndex = _random.Next(layers.Length);
        var (voice, pattern) = layers[layerIndex];

        bool thinOut = _random.Next(2) == 0;

        if (thinOut)
        {
            // Remove a random active step (but not on beat 1)
            var activeSteps = pattern.GetActiveSteps()
                .Where(s => s > 0)
                .ToList();

            if (activeSteps.Count <= 1) return;

            int stepToRemove = activeSteps[_random.Next(activeSteps.Count)];
            var newPattern = pattern.WithStep(stepToRemove, StepInfo.Rest());

            ReplaceLayer(state, layers, layerIndex, voice, newPattern);
        }
        else
        {
            // Add a hit on an empty step
            var emptySteps = new List<int>();
            for (int i = 0; i < pattern.StepsPerMeasure; i++)
            {
                if (!pattern.Steps[i].IsActive)
                    emptySteps.Add(i);
            }

            if (emptySteps.Count == 0) return;

            int stepToAdd = emptySteps[_random.Next(emptySteps.Count)];
            var newPattern = pattern.WithStep(stepToAdd, StepInfo.Hit(80));

            ReplaceLayer(state, layers, layerIndex, voice, newPattern);
        }
    }

    private void HandleRegisterShift(GenerationState state, double probability)
    {
        // Count down active register shift
        if (_registerShiftMeasuresRemaining > 0)
        {
            _registerShiftMeasuresRemaining--;
            if (_registerShiftMeasuresRemaining == 0)
            {
                state.Octave -= _registerShift;
                _registerShift = 0;
            }
            return;
        }

        // Don't overlap with octave drift
        if (_registerShift != 0 || _octaveDrift != 0) return;
        if (_random.NextDouble() >= probability) return;

        int direction = _random.Next(2) == 0 ? 1 : -1;
        int newOctave = state.Octave + direction;

        if (newOctave < 1 || newOctave > 8) return;

        _registerShift = direction;
        _registerShiftMeasuresRemaining = 2 + _random.Next(3); // 2-4 measures
        state.Octave = newOctave;
    }

    /// <summary>
    /// Applies velocity drift to a base velocity, clamping to valid MIDI range.
    /// </summary>
    public int ApplyVelocityDrift(int baseVelocity)
    {
        return Math.Clamp(baseVelocity + _velocityDrift, 0, 127);
    }

    /// <summary>
    /// Returns true if a rest should be inserted this beat (for breathing room).
    /// </summary>
    public bool ShouldInsertRest()
    {
        if (!_restInsertionActive) return false;
        // 50% chance per beat when active — not every beat becomes a rest
        return _random.NextDouble() < 0.5;
    }

    private static void ReplaceLayer(
        GenerationState state,
        System.Collections.Immutable.ImmutableArray<(DrumVoice Voice, BeatPattern Pattern)> layers,
        int layerIndex,
        DrumVoice voice,
        BeatPattern newPattern)
    {
        var newLayers = new List<(DrumVoice, BeatPattern)>();
        for (int i = 0; i < layers.Length; i++)
        {
            if (i == layerIndex)
                newLayers.Add((voice, newPattern));
            else
                newLayers.Add(layers[i]);
        }

        state.DrumPattern = new LayeredPattern(state.DrumPattern!.Name, newLayers);
    }
}
