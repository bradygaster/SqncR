namespace SqncR.Core.Rhythm;

/// <summary>
/// A single step in a beat pattern. Immutable.
/// </summary>
/// <param name="IsActive">Whether this step triggers a hit.</param>
/// <param name="Velocity">MIDI velocity 0-127.</param>
/// <param name="Probability">Probability of firing, 0.0-1.0. Used for generative variation.</param>
public readonly record struct StepInfo(bool IsActive, int Velocity = 100, double Probability = 1.0)
{
    public static StepInfo Hit(int velocity = 100, double probability = 1.0) =>
        new(true, Math.Clamp(velocity, 0, 127), Math.Clamp(probability, 0.0, 1.0));

    public static StepInfo Rest() => new(false, 0, 0.0);
}
