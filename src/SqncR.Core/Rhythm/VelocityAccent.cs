using System.Collections.Immutable;

namespace SqncR.Core.Rhythm;

/// <summary>
/// Velocity accent pattern — a sequence of multipliers applied to step velocities
/// to create dynamic emphasis (swing feel, strong/weak, build-ups, etc.).
/// </summary>
public sealed class VelocityAccent
{
    /// <summary>Multiplier per step. Length matches the target pattern's step count.</summary>
    public ImmutableArray<double> Multipliers { get; }

    /// <summary>Optional label.</summary>
    public string Name { get; }

    public VelocityAccent(IEnumerable<double> multipliers, string name = "")
    {
        Multipliers = multipliers.ToImmutableArray();
        if (Multipliers.Length == 0)
            throw new ArgumentException("Multipliers must not be empty.");
        Name = name;
    }

    /// <summary>
    /// Swing accent — off-beat steps get lower velocity to emphasize the groove.
    /// <paramref name="swingAmount"/> 0.0 = no accent, 1.0 = maximum contrast.
    /// </summary>
    public static VelocityAccent CreateSwingAccent(double swingAmount, int steps = 16)
    {
        swingAmount = Math.Clamp(swingAmount, 0.0, 1.0);
        var multipliers = new double[steps];
        for (int i = 0; i < steps; i++)
        {
            // On-beat steps (even) get full velocity; off-beat (odd) get reduced
            multipliers[i] = i % 2 == 0
                ? 1.0
                : 1.0 - (swingAmount * 0.4); // at max swing, off-beats are 60% velocity
        }
        return new VelocityAccent(multipliers, $"swing-{swingAmount:F1}");
    }

    /// <summary>
    /// Strong/weak accent — strong on beats 1 &amp; 3, weak on beats 2 &amp; 4.
    /// Assumes 16-step (16th note) resolution.
    /// </summary>
    public static VelocityAccent CreateStrongWeakAccent(int steps = 16)
    {
        int stepsPerBeat = steps / 4;
        var multipliers = new double[steps];
        for (int i = 0; i < steps; i++)
        {
            int beat = i / stepsPerBeat; // 0-3
            bool isStrong = beat == 0 || beat == 2;
            bool isDownbeat = i % stepsPerBeat == 0;

            if (isDownbeat && isStrong)
                multipliers[i] = 1.0;
            else if (isDownbeat)
                multipliers[i] = 0.75;
            else
                multipliers[i] = 0.6;
        }
        return new VelocityAccent(multipliers, "strong-weak");
    }

    /// <summary>
    /// Build-up accent — velocity gradually increases from soft to full over the given steps.
    /// </summary>
    public static VelocityAccent CreateBuildUp(int steps)
    {
        if (steps <= 0)
            throw new ArgumentOutOfRangeException(nameof(steps));

        var multipliers = new double[steps];
        for (int i = 0; i < steps; i++)
        {
            multipliers[i] = 0.3 + (0.7 * i / (steps - 1 == 0 ? 1 : steps - 1));
        }
        return new VelocityAccent(multipliers, "build-up");
    }

    /// <summary>
    /// Apply this accent to a BeatPattern, scaling velocities by the multipliers.
    /// Multipliers are cycled if the pattern is longer than the accent.
    /// </summary>
    public BeatPattern ApplyTo(BeatPattern pattern)
    {
        var newSteps = new StepInfo[pattern.StepsPerMeasure];
        for (int i = 0; i < pattern.StepsPerMeasure; i++)
        {
            var step = pattern.Steps[i];
            if (!step.IsActive)
            {
                newSteps[i] = step;
                continue;
            }

            double multiplier = Multipliers[i % Multipliers.Length];
            int newVelocity = Math.Clamp((int)(step.Velocity * multiplier), 0, 127);
            newSteps[i] = StepInfo.Hit(newVelocity, step.Probability);
        }
        return new BeatPattern(pattern.StepsPerMeasure, newSteps, pattern.Name);
    }
}
