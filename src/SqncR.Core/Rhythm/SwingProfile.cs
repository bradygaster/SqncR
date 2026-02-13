namespace SqncR.Core.Rhythm;

/// <summary>
/// Swing/shuffle timing. Pushes even-numbered steps late to create groove.
/// SwingAmount 0.0 = dead straight. 1.0 = full triplet swing (66/33 ratio).
/// </summary>
public sealed class SwingProfile
{
    /// <summary>Swing amount: 0.0 (straight) to 1.0 (full triplet swing).</summary>
    public double SwingAmount { get; }

    public SwingProfile(double swingAmount)
    {
        SwingAmount = Math.Clamp(swingAmount, 0.0, 1.0);
    }

    /// <summary>No swing — perfectly quantized.</summary>
    public static SwingProfile Straight { get; } = new(0.0);

    /// <summary>Light swing — subtle shuffle.</summary>
    public static SwingProfile Light { get; } = new(0.3);

    /// <summary>Medium swing — classic groovy feel.</summary>
    public static SwingProfile Medium { get; } = new(0.5);

    /// <summary>Full triplet swing — hard shuffle.</summary>
    public static SwingProfile Full { get; } = new(1.0);

    /// <summary>
    /// Apply swing to a step's tick time. Even-numbered steps (0-indexed) get pushed later.
    /// </summary>
    /// <param name="stepIndex">0-based step index in the pattern.</param>
    /// <param name="tickTime">Original tick time for this step.</param>
    /// <param name="ticksPerStep">Duration in ticks of one step at straight timing.</param>
    /// <returns>Adjusted tick time with swing applied.</returns>
    public long ApplySwing(int stepIndex, long tickTime, int ticksPerStep)
    {
        // Only even-indexed steps in pairs get swung.
        // In a pair (0,1), (2,3), (4,5)..., the second of each pair (odd index) gets delayed.
        if (stepIndex % 2 == 0)
            return tickTime; // downbeat stays on the grid

        // Max offset is 1/3 of a step (triplet feel: 2/3 + 1/3 instead of 1/2 + 1/2)
        double maxOffset = ticksPerStep / 3.0;
        long offset = (long)(maxOffset * SwingAmount);
        return tickTime + offset;
    }
}
