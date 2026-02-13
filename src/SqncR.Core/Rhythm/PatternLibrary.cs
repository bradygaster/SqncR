using System.Collections.Frozen;

namespace SqncR.Core.Rhythm;

/// <summary>
/// Reusable beat pattern templates. The groove cookbook.
/// Each entry is a complete layered drum pattern (kick + snare + hat).
/// </summary>
public static class PatternLibrary
{
    private static readonly FrozenDictionary<string, Func<LayeredPattern>> Patterns;

    static PatternLibrary()
    {
        Patterns = new Dictionary<string, Func<LayeredPattern>>(StringComparer.OrdinalIgnoreCase)
        {
            ["rock"] = BuildRock,
            ["house"] = BuildHouse,
            ["hip-hop"] = BuildHipHop,
            ["jazz"] = BuildJazz,
            ["ambient"] = BuildAmbient,
            ["breakbeat"] = BuildBreakbeat,
            ["half-time"] = BuildHalfTime,
            ["shuffle"] = BuildShuffle,
            ["latin-clave"] = BuildLatinClave,
            ["bossa-nova"] = BuildBossaNova,
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Get a pattern by name. Case-insensitive.</summary>
    public static LayeredPattern Get(string name)
    {
        if (Patterns.TryGetValue(name, out var factory))
            return factory();
        throw new ArgumentException($"Unknown pattern '{name}'. Available: {string.Join(", ", Names)}");
    }

    /// <summary>All available pattern names.</summary>
    public static IEnumerable<string> Names => Patterns.Keys;

    // ── Pattern builders ──

    private static LayeredPattern BuildRock()
    {
        // Classic rock: kick on 1 & 3, snare on 2 & 4, closed hat on 8ths
        var kick = FromHits(16, [0, 8], 110, "rock-kick");
        var snare = FromHits(16, [4, 12], 110, "rock-snare");
        var hat = FromHits(16, [0, 2, 4, 6, 8, 10, 12, 14], 80, "rock-hat");

        return new LayeredPattern("rock", [
            (DrumVoice.Kick, kick),
            (DrumVoice.Snare, snare),
            (DrumVoice.ClosedHiHat, hat)
        ]);
    }

    private static LayeredPattern BuildHouse()
    {
        // House: four-on-the-floor kick, off-beat open hat, snare on 2 & 4
        var kick = BeatPattern.FourOnTheFloor();
        var snare = FromHits(16, [4, 12], 100, "house-snare");
        var hat = BeatPattern.OffBeat();

        return new LayeredPattern("house", [
            (DrumVoice.Kick, kick),
            (DrumVoice.Snare, snare),
            (DrumVoice.OpenHiHat, hat)
        ]);
    }

    private static LayeredPattern BuildHipHop()
    {
        // Hip-hop: kick on 1 and the "and" of 3, snare on 2 & 4, hat 16ths with accents
        var kick = FromHits(16, [0, 10], 110, "hiphop-kick");
        var snare = FromHits(16, [4, 12], 110, "hiphop-snare");
        var hat = BuildAccentedHat(16, "hiphop-hat");

        return new LayeredPattern("hip-hop", [
            (DrumVoice.Kick, kick),
            (DrumVoice.Snare, snare),
            (DrumVoice.ClosedHiHat, hat)
        ]);
    }

    private static LayeredPattern BuildJazz()
    {
        // Jazz ride: steps 0, 3, 6, 10 (swing-friendly spacing), kick ghost notes
        var ride = FromHits(16, [0, 6, 8, 14], 90, "jazz-ride");
        var kick = FromHitsWithVelocity(16, [(0, 70), (10, 50)], "jazz-kick");
        var hat = FromHits(16, [4, 12], 60, "jazz-hat");

        return new LayeredPattern("jazz", [
            (DrumVoice.Ride, ride),
            (DrumVoice.Kick, kick),
            (DrumVoice.PedalHiHat, hat)
        ]);
    }

    private static LayeredPattern BuildAmbient()
    {
        // Ambient: sparse, probabilistic hits
        var kick = FromHitsWithProbability(16, [(0, 100, 0.8), (8, 80, 0.5)], "ambient-kick");
        var rim = FromHitsWithProbability(16, [(4, 60, 0.4), (12, 50, 0.3)], "ambient-rim");

        return new LayeredPattern("ambient", [
            (DrumVoice.Kick, kick),
            (DrumVoice.RimShot, rim)
        ]);
    }

    private static LayeredPattern BuildBreakbeat()
    {
        // Breakbeat: syncopated kick with snare on 2 & 4, busy hat work
        var kick = FromHits(16, [0, 3, 6, 10], 110, "breakbeat-kick");
        var snare = FromHits(16, [4, 12], 110, "breakbeat-snare");
        var hat = BuildAccentedHat(16, "breakbeat-hat");

        return new LayeredPattern("breakbeat", [
            (DrumVoice.Kick, kick),
            (DrumVoice.Snare, snare),
            (DrumVoice.ClosedHiHat, hat)
        ]);
    }

    private static LayeredPattern BuildHalfTime()
    {
        // Half-time: kick on 1, snare on 3, open hat on 8ths
        var kick = FromHits(16, [0], 110, "halftime-kick");
        var snare = FromHits(16, [8], 110, "halftime-snare");
        var hat = FromHits(16, [0, 4, 8, 12], 80, "halftime-hat");

        return new LayeredPattern("half-time", [
            (DrumVoice.Kick, kick),
            (DrumVoice.Snare, snare),
            (DrumVoice.ClosedHiHat, hat)
        ]);
    }

    private static LayeredPattern BuildShuffle()
    {
        // Shuffle: kick on 1 & 3, snare on 2 & 4, triplet-feel hat (accented on beats)
        var kick = FromHits(16, [0, 8], 110, "shuffle-kick");
        var snare = FromHits(16, [4, 12], 110, "shuffle-snare");
        // Simulate triplet shuffle with alternating velocities
        var hat = FromHitsWithVelocity(16,
            [(0, 100), (1, 50), (2, 80), (3, 50), (4, 100), (5, 50),
             (6, 80), (7, 50), (8, 100), (9, 50), (10, 80), (11, 50),
             (12, 100), (13, 50), (14, 80), (15, 50)], "shuffle-hat");

        return new LayeredPattern("shuffle", [
            (DrumVoice.Kick, kick),
            (DrumVoice.Snare, snare),
            (DrumVoice.ClosedHiHat, hat)
        ]);
    }

    private static LayeredPattern BuildLatinClave()
    {
        // Son clave 3-2 pattern: hits on steps 0, 3, 6, 10, 12
        var clave = FromHits(16, [0, 3, 6, 10, 12], 100, "clave-3-2");
        var kick = FromHits(16, [0, 8], 90, "clave-kick");

        return new LayeredPattern("latin-clave", [
            (DrumVoice.RimShot, clave),
            (DrumVoice.Kick, kick)
        ]);
    }

    private static LayeredPattern BuildBossaNova()
    {
        // Bossa nova: cross-stick pattern with bass drum, gentle hat
        var crossStick = FromHits(16, [2, 6, 10, 14], 80, "bossa-cross");
        var kick = FromHits(16, [0, 4, 10], 90, "bossa-kick");
        var hat = FromHits(16, [0, 2, 4, 6, 8, 10, 12, 14], 60, "bossa-hat");

        return new LayeredPattern("bossa-nova", [
            (DrumVoice.SideStick, crossStick),
            (DrumVoice.Kick, kick),
            (DrumVoice.ClosedHiHat, hat)
        ]);
    }

    // ── Helpers ──

    private static BeatPattern FromHits(int steps, int[] activeSteps, int velocity, string name)
    {
        var s = new StepInfo[steps];
        for (int i = 0; i < steps; i++)
            s[i] = StepInfo.Rest();
        foreach (var idx in activeSteps)
            s[idx] = StepInfo.Hit(velocity);
        return new BeatPattern(steps, s, name);
    }

    private static BeatPattern FromHitsWithVelocity(int steps, (int Index, int Velocity)[] hits, string name)
    {
        var s = new StepInfo[steps];
        for (int i = 0; i < steps; i++)
            s[i] = StepInfo.Rest();
        foreach (var (idx, vel) in hits)
            s[idx] = StepInfo.Hit(vel);
        return new BeatPattern(steps, s, name);
    }

    private static BeatPattern FromHitsWithProbability(int steps, (int Index, int Velocity, double Probability)[] hits, string name)
    {
        var s = new StepInfo[steps];
        for (int i = 0; i < steps; i++)
            s[i] = StepInfo.Rest();
        foreach (var (idx, vel, prob) in hits)
            s[idx] = StepInfo.Hit(vel, prob);
        return new BeatPattern(steps, s, name);
    }

    private static BeatPattern BuildAccentedHat(int steps, string name)
    {
        var s = new StepInfo[steps];
        for (int i = 0; i < steps; i++)
        {
            // Accent on quarter notes, ghost notes on the rest
            int vel = (i % 4 == 0) ? 100 : 60;
            s[i] = StepInfo.Hit(vel);
        }
        return new BeatPattern(steps, s, name);
    }
}
