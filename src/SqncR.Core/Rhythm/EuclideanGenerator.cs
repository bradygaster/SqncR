namespace SqncR.Core.Rhythm;

/// <summary>
/// Generates Euclidean rhythms using Bjorklund's algorithm.
/// Distributes K hits as evenly as possible across N steps.
/// </summary>
public static class EuclideanGenerator
{
    /// <summary>
    /// Bjorklund's algorithm: distribute <paramref name="hits"/> pulses
    /// evenly across <paramref name="steps"/> positions.
    /// </summary>
    /// <param name="steps">Total number of steps (N).</param>
    /// <param name="hits">Number of active hits (K).</param>
    /// <param name="rotation">Right-shift rotation (wrapping).</param>
    public static bool[] Generate(int steps, int hits, int rotation = 0)
    {
        if (steps <= 0)
            throw new ArgumentOutOfRangeException(nameof(steps), "Must be positive.");
        if (hits < 0 || hits > steps)
            throw new ArgumentOutOfRangeException(nameof(hits), $"Must be between 0 and {steps}.");

        if (hits == 0) return new bool[steps];
        if (hits == steps)
        {
            var all = new bool[steps];
            Array.Fill(all, true);
            return all;
        }

        var result = Bjorklund(steps, hits);

        if (rotation != 0)
        {
            rotation = ((rotation % steps) + steps) % steps;
            var rotated = new bool[steps];
            for (int i = 0; i < steps; i++)
                rotated[(i + rotation) % steps] = result[i];
            return rotated;
        }

        return result;
    }

    /// <summary>
    /// Convert a Euclidean rhythm to a <see cref="BeatPattern"/>.
    /// </summary>
    public static BeatPattern ToBeatPattern(int steps, int hits,
        int rotation = 0, int velocity = 100, double probability = 1.0, string name = "")
    {
        var rhythm = Generate(steps, hits, rotation);
        var stepInfos = rhythm.Select(hit => hit ? StepInfo.Hit(velocity, probability) : StepInfo.Rest());
        return new BeatPattern(steps, stepInfos, name);
    }

    // ── Named presets from Toussaint's musicological research ──

    /// <summary>E(3,8) — Cuban tresillo, the 3-3-2 pattern.</summary>
    public static BeatPattern Tresillo() => ToBeatPattern(8, 3, name: "euclidean-tresillo");

    /// <summary>E(5,8) — Cuban cinquillo.</summary>
    public static BeatPattern Cinquillo() => ToBeatPattern(8, 5, name: "euclidean-cinquillo");

    /// <summary>E(5,16) — Bossa nova rhythm.</summary>
    public static BeatPattern BossaNova() => ToBeatPattern(16, 5, name: "euclidean-bossa");

    /// <summary>E(7,16) — West African bell pattern.</summary>
    public static BeatPattern WestAfrican() => ToBeatPattern(16, 7, name: "euclidean-west-african");

    /// <summary>E(3,16) — Sparse, ambient-friendly pattern.</summary>
    public static BeatPattern Sparse() => ToBeatPattern(16, 3, name: "euclidean-sparse");

    /// <summary>E(11,16) — Dense, driving pattern.</summary>
    public static BeatPattern Dense() => ToBeatPattern(16, 11, name: "euclidean-dense");

    // ── Bjorklund's algorithm (list-of-lists formulation) ──

    private static bool[] Bjorklund(int steps, int hits)
    {
        var pattern = new List<List<bool>>(hits);
        var remainder = new List<List<bool>>(steps - hits);

        for (int i = 0; i < hits; i++)
            pattern.Add([true]);
        for (int i = 0; i < steps - hits; i++)
            remainder.Add([false]);

        while (remainder.Count > 1)
        {
            var newPattern = new List<List<bool>>();
            var newRemainder = new List<List<bool>>();
            int minCount = Math.Min(pattern.Count, remainder.Count);

            for (int i = 0; i < minCount; i++)
            {
                var combined = new List<bool>(pattern[i]);
                combined.AddRange(remainder[i]);
                newPattern.Add(combined);
            }

            var larger = pattern.Count > remainder.Count ? pattern : remainder;
            for (int i = minCount; i < larger.Count; i++)
                newRemainder.Add(larger[i]);

            pattern = newPattern;
            remainder = newRemainder;
        }

        var result = new bool[steps];
        int idx = 0;
        foreach (var group in pattern)
            foreach (var bit in group)
                result[idx++] = bit;
        foreach (var group in remainder)
            foreach (var bit in group)
                result[idx++] = bit;

        return result;
    }
}
