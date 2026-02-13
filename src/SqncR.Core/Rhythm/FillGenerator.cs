namespace SqncR.Core.Rhythm;

/// <summary>
/// Fill styles for automatic drum fill generation.
/// </summary>
public enum FillStyle
{
    SnareRoll,
    TomCascade,
    BuildUp,
    Breakdown
}

/// <summary>
/// Generates drum fills for section boundaries — snare rolls, tom cascades,
/// build-ups, and breakdowns. Each fill returns a single-measure BeatPattern
/// paired with the appropriate drum voice.
/// </summary>
public static class FillGenerator
{
    /// <summary>
    /// Generate a fill pattern for the given style.
    /// </summary>
    /// <param name="map">Drum map used to verify voice availability.</param>
    /// <param name="steps">Number of steps in the fill (typically 16).</param>
    /// <param name="style">The type of fill to generate.</param>
    public static LayeredPattern GenerateFill(DrumMap map, int steps, FillStyle style) => style switch
    {
        FillStyle.SnareRoll => BuildSnareRoll(map, steps),
        FillStyle.TomCascade => BuildTomCascade(map, steps),
        FillStyle.BuildUp => BuildBuildUp(map, steps),
        FillStyle.Breakdown => BuildBreakdown(map, steps),
        _ => throw new ArgumentOutOfRangeException(nameof(style))
    };

    private static LayeredPattern BuildSnareRoll(DrumMap map, int steps)
    {
        // Snare hits on every step in the last half, building velocity
        var snareSteps = new StepInfo[steps];
        int fillStart = steps / 2;
        for (int i = 0; i < steps; i++)
        {
            if (i >= fillStart)
            {
                double progress = (double)(i - fillStart) / (steps - fillStart - 1);
                int velocity = (int)(70 + 57 * progress); // 70 → 127
                snareSteps[i] = StepInfo.Hit(Math.Min(velocity, 127));
            }
            else
            {
                snareSteps[i] = StepInfo.Rest();
            }
        }

        // Crash on the very last step for emphasis
        var crashSteps = CreateSingleHit(steps, steps - 1, 127);

        return new LayeredPattern("fill-snare-roll", [
            (DrumVoice.Snare, new BeatPattern(steps, snareSteps, "fill-snare")),
            (DrumVoice.Crash, new BeatPattern(steps, crashSteps, "fill-crash"))
        ]);
    }

    private static LayeredPattern BuildTomCascade(DrumMap map, int steps)
    {
        // Descending tom pattern: high → mid → low over the last 3/4 of the measure
        var highSteps = new StepInfo[steps];
        var midSteps = new StepInfo[steps];
        var lowSteps = new StepInfo[steps];

        int fillStart = steps / 4;
        int segmentLength = (steps - fillStart) / 3;

        for (int i = 0; i < steps; i++)
        {
            highSteps[i] = StepInfo.Rest();
            midSteps[i] = StepInfo.Rest();
            lowSteps[i] = StepInfo.Rest();
        }

        // High toms first segment
        for (int i = fillStart; i < fillStart + segmentLength; i++)
            highSteps[i] = StepInfo.Hit(100);

        // Mid toms second segment
        for (int i = fillStart + segmentLength; i < fillStart + 2 * segmentLength; i++)
            midSteps[i] = StepInfo.Hit(105);

        // Low toms final segment
        for (int i = fillStart + 2 * segmentLength; i < steps; i++)
            lowSteps[i] = StepInfo.Hit(110);

        return new LayeredPattern("fill-tom-cascade", [
            (DrumVoice.HighTom, new BeatPattern(steps, highSteps, "fill-high-tom")),
            (DrumVoice.MidTom, new BeatPattern(steps, midSteps, "fill-mid-tom")),
            (DrumVoice.LowTom, new BeatPattern(steps, lowSteps, "fill-low-tom"))
        ]);
    }

    private static LayeredPattern BuildBuildUp(DrumMap map, int steps)
    {
        // Kick and snare with increasing density and velocity
        var kickSteps = new StepInfo[steps];
        var snareSteps = new StepInfo[steps];

        for (int i = 0; i < steps; i++)
        {
            kickSteps[i] = StepInfo.Rest();
            snareSteps[i] = StepInfo.Rest();
        }

        // Kick: every 4 steps in first half, every 2 in second half
        for (int i = 0; i < steps; i++)
        {
            int interval = i < steps / 2 ? 4 : 2;
            if (i % interval == 0)
            {
                double progress = (double)i / (steps - 1);
                int velocity = (int)(60 + 67 * progress); // 60 → 127
                kickSteps[i] = StepInfo.Hit(Math.Min(velocity, 127));
            }
        }

        // Snare: second half, every 2 steps, increasing velocity
        for (int i = steps / 2; i < steps; i += 2)
        {
            double progress = (double)(i - steps / 2) / (steps / 2);
            int velocity = (int)(70 + 57 * progress);
            snareSteps[i] = StepInfo.Hit(Math.Min(velocity, 127));
        }

        return new LayeredPattern("fill-build-up", [
            (DrumVoice.Kick, new BeatPattern(steps, kickSteps, "fill-kick-buildup")),
            (DrumVoice.Snare, new BeatPattern(steps, snareSteps, "fill-snare-buildup"))
        ]);
    }

    private static LayeredPattern BuildBreakdown(DrumMap map, int steps)
    {
        // Sparse pattern — only kick on 1, open hat at midpoint, and crash at end
        var kickSteps = CreateSingleHit(steps, 0, 110);
        var hatSteps = CreateSingleHit(steps, steps / 2, 80);
        var crashSteps = CreateSingleHit(steps, steps - 1, 120);

        return new LayeredPattern("fill-breakdown", [
            (DrumVoice.Kick, new BeatPattern(steps, kickSteps, "fill-kick-bd")),
            (DrumVoice.OpenHiHat, new BeatPattern(steps, hatSteps, "fill-hat-bd")),
            (DrumVoice.Crash, new BeatPattern(steps, crashSteps, "fill-crash-bd"))
        ]);
    }

    private static StepInfo[] CreateSingleHit(int steps, int hitIndex, int velocity)
    {
        var s = new StepInfo[steps];
        for (int i = 0; i < steps; i++)
            s[i] = StepInfo.Rest();
        s[hitIndex] = StepInfo.Hit(velocity);
        return s;
    }
}
