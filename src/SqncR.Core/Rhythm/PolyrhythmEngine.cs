namespace SqncR.Core.Rhythm;

/// <summary>
/// Generates polyrhythmic patterns — two or more rhythmic layers
/// with different beat divisions running simultaneously.
/// </summary>
public static class PolyrhythmEngine
{
    /// <summary>
    /// Create a polyrhythmic pattern by distributing <paramref name="beats"/>
    /// evenly across <paramref name="totalSteps"/> steps using the Bjorklund algorithm approach.
    /// </summary>
    /// <param name="beats">Number of hits to distribute.</param>
    /// <param name="totalSteps">Total number of steps in the pattern.</param>
    /// <param name="velocity">MIDI velocity for hits.</param>
    /// <param name="name">Optional pattern name.</param>
    public static BeatPattern CreateLayer(int beats, int totalSteps, int velocity = 100, string name = "")
    {
        if (beats <= 0)
            throw new ArgumentOutOfRangeException(nameof(beats), "Must be positive.");
        if (totalSteps <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalSteps), "Must be positive.");
        if (beats > totalSteps)
            throw new ArgumentException("Beats cannot exceed total steps.");

        var steps = new StepInfo[totalSteps];
        for (int i = 0; i < totalSteps; i++)
            steps[i] = StepInfo.Rest();

        // Evenly distribute beats using integer scaling
        for (int i = 0; i < beats; i++)
        {
            int stepIndex = (int)Math.Round((double)i * totalSteps / beats) % totalSteps;
            steps[stepIndex] = StepInfo.Hit(velocity);
        }

        return new BeatPattern(totalSteps, steps, name);
    }

    /// <summary>
    /// Create a polyrhythmic composite: <paramref name="crossBeats"/> over <paramref name="baseBeats"/>.
    /// Returns a LayeredPattern with two voices.
    /// </summary>
    /// <param name="baseBeats">Number of beats in the base rhythm (e.g., 4 in "3-over-4").</param>
    /// <param name="crossBeats">Number of beats in the cross rhythm (e.g., 3 in "3-over-4").</param>
    /// <param name="stepsPerBeat">Step resolution per base beat. Total steps = baseBeats × stepsPerBeat.</param>
    /// <param name="baseVoice">Drum voice for the base layer.</param>
    /// <param name="crossVoice">Drum voice for the cross layer.</param>
    public static LayeredPattern CreatePolyrhythm(
        int baseBeats,
        int crossBeats,
        int stepsPerBeat = 4,
        DrumVoice baseVoice = DrumVoice.Kick,
        DrumVoice crossVoice = DrumVoice.ClosedHiHat)
    {
        if (baseBeats <= 0)
            throw new ArgumentOutOfRangeException(nameof(baseBeats));
        if (crossBeats <= 0)
            throw new ArgumentOutOfRangeException(nameof(crossBeats));
        if (stepsPerBeat <= 0)
            throw new ArgumentOutOfRangeException(nameof(stepsPerBeat));

        int totalSteps = baseBeats * stepsPerBeat;
        string name = $"{crossBeats}-over-{baseBeats}";

        var baseLayer = CreateLayer(baseBeats, totalSteps, 110, $"{name}-base");
        var crossLayer = CreateLayer(crossBeats, totalSteps, 90, $"{name}-cross");

        return new LayeredPattern(name, [
            (baseVoice, baseLayer),
            (crossVoice, crossLayer)
        ]);
    }

    /// <summary>
    /// Get a named polyrhythmic pattern. Supports: "3-over-4", "5-over-4", "7-over-8".
    /// </summary>
    public static LayeredPattern GetPolyrhythmicPattern(string name) => name.ToLowerInvariant() switch
    {
        "3-over-4" => CreatePolyrhythm(4, 3),
        "5-over-4" => CreatePolyrhythm(4, 5),
        "7-over-8" => CreatePolyrhythm(8, 7),
        _ => throw new ArgumentException(
            $"Unknown polyrhythm '{name}'. Available: 3-over-4, 5-over-4, 7-over-8")
    };
}
