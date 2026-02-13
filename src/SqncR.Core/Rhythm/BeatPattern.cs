using System.Collections.Immutable;

namespace SqncR.Core.Rhythm;

/// <summary>
/// A rhythmic pattern — the heartbeat of a groove.
/// Immutable. Create via factory methods or the constructor.
/// </summary>
public sealed class BeatPattern
{
    /// <summary>Number of steps in one measure (e.g., 16 for 16th-note resolution).</summary>
    public int StepsPerMeasure { get; }

    /// <summary>The step data for this pattern.</summary>
    public ImmutableArray<StepInfo> Steps { get; }

    /// <summary>Optional label for readability (e.g., "kick", "snare").</summary>
    public string Name { get; }

    public BeatPattern(int stepsPerMeasure, IEnumerable<StepInfo> steps, string name = "")
    {
        if (stepsPerMeasure <= 0)
            throw new ArgumentOutOfRangeException(nameof(stepsPerMeasure), "Must be positive.");

        var stepArray = steps.ToImmutableArray();
        if (stepArray.Length != stepsPerMeasure)
            throw new ArgumentException($"Expected {stepsPerMeasure} steps, got {stepArray.Length}.");

        StepsPerMeasure = stepsPerMeasure;
        Steps = stepArray;
        Name = name;
    }

    /// <summary>Returns indices of active steps.</summary>
    public IReadOnlyList<int> GetActiveSteps()
    {
        var active = new List<int>();
        for (int i = 0; i < Steps.Length; i++)
        {
            if (Steps[i].IsActive)
                active.Add(i);
        }
        return active;
    }

    /// <summary>Returns a new pattern with a step toggled on or off.</summary>
    public BeatPattern WithStep(int index, StepInfo step)
    {
        if (index < 0 || index >= StepsPerMeasure)
            throw new ArgumentOutOfRangeException(nameof(index));

        var builder = Steps.ToBuilder();
        builder[index] = step;
        return new BeatPattern(StepsPerMeasure, builder, Name);
    }

    // ── Factory methods: common patterns at 16-step resolution ──

    /// <summary>Kick on beats 1, 2, 3, 4 (steps 0, 4, 8, 12). The foundation of dance music.</summary>
    public static BeatPattern FourOnTheFloor(int stepsPerMeasure = 16)
    {
        var steps = CreateRests(stepsPerMeasure);
        int interval = stepsPerMeasure / 4;
        for (int i = 0; i < 4; i++)
            steps[i * interval] = StepInfo.Hit(110);
        return new BeatPattern(stepsPerMeasure, steps, "four-on-the-floor");
    }

    /// <summary>Kick on 1, snare on 3 (steps 0, 8 in 16-step). Laid-back feel.</summary>
    public static BeatPattern HalfTime(int stepsPerMeasure = 16)
    {
        var steps = CreateRests(stepsPerMeasure);
        steps[0] = StepInfo.Hit(110);  // kick on 1
        steps[stepsPerMeasure / 2] = StepInfo.Hit(100); // snare on 3
        return new BeatPattern(stepsPerMeasure, steps, "half-time");
    }

    /// <summary>Hits on even steps (1-indexed: 2, 4, 6...). Classic off-beat hi-hat.</summary>
    public static BeatPattern OffBeat(int stepsPerMeasure = 16)
    {
        var steps = CreateRests(stepsPerMeasure);
        for (int i = 1; i < stepsPerMeasure; i += 2)
            steps[i] = StepInfo.Hit(80);
        return new BeatPattern(stepsPerMeasure, steps, "off-beat");
    }

    /// <summary>Hits on every step. Straight 16ths, full driving energy.</summary>
    public static BeatPattern Straight(int stepsPerMeasure = 16)
    {
        var steps = new StepInfo[stepsPerMeasure];
        for (int i = 0; i < stepsPerMeasure; i++)
            steps[i] = StepInfo.Hit(80);
        return new BeatPattern(stepsPerMeasure, steps, "straight");
    }

    /// <summary>Backbeat snare — hits on beats 2 and 4 (steps 4, 12 in 16-step).</summary>
    public static BeatPattern Backbeat(int stepsPerMeasure = 16)
    {
        var steps = CreateRests(stepsPerMeasure);
        int interval = stepsPerMeasure / 4;
        steps[interval] = StepInfo.Hit(110);       // beat 2
        steps[3 * interval] = StepInfo.Hit(110);   // beat 4
        return new BeatPattern(stepsPerMeasure, steps, "backbeat");
    }

    private static StepInfo[] CreateRests(int count)
    {
        var steps = new StepInfo[count];
        for (int i = 0; i < count; i++)
            steps[i] = StepInfo.Rest();
        return steps;
    }
}
