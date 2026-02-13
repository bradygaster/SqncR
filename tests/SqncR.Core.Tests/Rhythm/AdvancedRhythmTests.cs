using SqncR.Core.Rhythm;

namespace SqncR.Core.Tests.Rhythm;

public class AdvancedRhythmTests
{
    // ── DrumMap tests ──

    [Theory]
    [InlineData(DrumVoice.Kick, 36)]
    [InlineData(DrumVoice.Snare, 38)]
    [InlineData(DrumVoice.ClosedHiHat, 42)]
    [InlineData(DrumVoice.OpenHiHat, 46)]
    [InlineData(DrumVoice.Crash, 49)]
    [InlineData(DrumVoice.Ride, 51)]
    [InlineData(DrumVoice.LowTom, 45)]
    [InlineData(DrumVoice.MidTom, 47)]
    [InlineData(DrumVoice.HighTom, 50)]
    [InlineData(DrumVoice.Clap, 39)]
    public void DrumMap_GeneralMidi_HasCorrectMappings(DrumVoice voice, int expectedNote)
    {
        Assert.Equal(expectedNote, DrumMap.GeneralMidi.GetMidiNote(voice));
    }

    [Fact]
    public void DrumMap_SimplifiedKit_HasCoreVoicesOnly()
    {
        var kit = DrumMap.SimplifiedKit;
        Assert.Equal(36, kit.GetMidiNote(DrumVoice.Kick));
        Assert.Equal(38, kit.GetMidiNote(DrumVoice.Snare));
        Assert.Equal(42, kit.GetMidiNote(DrumVoice.ClosedHiHat));
        Assert.False(kit.Contains(DrumVoice.LowTom));
        Assert.False(kit.Contains(DrumVoice.Cowbell));
    }

    // ── Polyrhythm tests ──

    [Fact]
    public void Polyrhythm_3Over4_ProducesCorrectBeatPlacement()
    {
        var poly = PolyrhythmEngine.GetPolyrhythmicPattern("3-over-4");
        Assert.Equal("3-over-4", poly.Name);
        Assert.Equal(2, poly.Layers.Length);

        // Base layer: 4 beats across 16 steps → steps 0, 4, 8, 12
        var baseLayer = poly.Layers[0].Pattern;
        var baseActive = baseLayer.GetActiveSteps();
        Assert.Equal(4, baseActive.Count);
        Assert.Contains(0, baseActive);
        Assert.Contains(4, baseActive);
        Assert.Contains(8, baseActive);
        Assert.Contains(12, baseActive);

        // Cross layer: 3 beats across 16 steps → evenly spaced
        var crossLayer = poly.Layers[1].Pattern;
        var crossActive = crossLayer.GetActiveSteps();
        Assert.Equal(3, crossActive.Count);
    }

    [Fact]
    public void Polyrhythm_5Over4_ProducesCorrectBeatPlacement()
    {
        var poly = PolyrhythmEngine.GetPolyrhythmicPattern("5-over-4");
        Assert.Equal("5-over-4", poly.Name);

        var baseLayer = poly.Layers[0].Pattern;
        Assert.Equal(4, baseLayer.GetActiveSteps().Count);

        var crossLayer = poly.Layers[1].Pattern;
        Assert.Equal(5, crossLayer.GetActiveSteps().Count);
    }

    [Fact]
    public void Polyrhythm_CreateLayer_EvenlyDistributesBeats()
    {
        var pattern = PolyrhythmEngine.CreateLayer(3, 12, 100, "test");
        var active = pattern.GetActiveSteps();
        Assert.Equal(3, active.Count);
        // 3 beats across 12 steps: 0, 4, 8
        Assert.Equal(0, active[0]);
        Assert.Equal(4, active[1]);
        Assert.Equal(8, active[2]);
    }

    [Fact]
    public void Polyrhythm_UnknownName_Throws()
    {
        Assert.Throws<ArgumentException>(() => PolyrhythmEngine.GetPolyrhythmicPattern("9-over-11"));
    }

    // ── VelocityAccent tests ──

    [Fact]
    public void VelocityAccent_SwingAccent_ReducesOffBeatVelocity()
    {
        var accent = VelocityAccent.CreateSwingAccent(1.0, 16);
        var pattern = BeatPattern.Straight(16); // all steps active at vel 80

        var accented = accent.ApplyTo(pattern);

        // On-beat steps should stay at original velocity
        Assert.Equal(80, accented.Steps[0].Velocity);
        Assert.Equal(80, accented.Steps[2].Velocity);

        // Off-beat steps should be reduced (80 * 0.6 = 48)
        Assert.Equal(48, accented.Steps[1].Velocity);
        Assert.Equal(48, accented.Steps[3].Velocity);
    }

    [Fact]
    public void VelocityAccent_StrongWeak_AccentsBeats1And3()
    {
        var accent = VelocityAccent.CreateStrongWeakAccent(16);
        var pattern = BeatPattern.Straight(16);

        var accented = accent.ApplyTo(pattern);

        // Beat 1 (step 0) and beat 3 (step 8) should have full velocity
        Assert.Equal(80, accented.Steps[0].Velocity);  // 80 * 1.0
        Assert.Equal(80, accented.Steps[8].Velocity);   // 80 * 1.0

        // Beat 2 (step 4) and beat 4 (step 12) should be weaker
        Assert.Equal(60, accented.Steps[4].Velocity);   // 80 * 0.75
        Assert.Equal(60, accented.Steps[12].Velocity);  // 80 * 0.75
    }

    [Fact]
    public void VelocityAccent_BuildUp_IncreasesGradually()
    {
        var accent = VelocityAccent.CreateBuildUp(16);

        // First multiplier should be low, last should be high
        Assert.True(accent.Multipliers[0] < accent.Multipliers[15]);
        Assert.InRange(accent.Multipliers[0], 0.25, 0.35);
        Assert.InRange(accent.Multipliers[15], 0.95, 1.05);
    }

    [Fact]
    public void VelocityAccent_ApplyPreservesRests()
    {
        var accent = VelocityAccent.CreateSwingAccent(1.0, 16);
        var pattern = BeatPattern.HalfTime(16); // only steps 0 and 8 active

        var accented = accent.ApplyTo(pattern);

        // Rests should remain rests
        Assert.False(accented.Steps[1].IsActive);
        Assert.False(accented.Steps[5].IsActive);
    }

    // ── FillGenerator tests ──

    [Fact]
    public void FillGenerator_SnareRoll_ProducesValidPattern()
    {
        var fill = FillGenerator.GenerateFill(DrumMap.GeneralMidi, 16, FillStyle.SnareRoll);
        Assert.Equal("fill-snare-roll", fill.Name);
        Assert.True(fill.Layers.Length >= 1);

        // Snare layer should have hits in the second half
        var snareLayer = fill.Layers.First(l => l.Voice == DrumVoice.Snare).Pattern;
        var active = snareLayer.GetActiveSteps();
        Assert.True(active.Count > 0);
        Assert.True(active.All(s => s >= 8)); // only in second half
    }

    [Fact]
    public void FillGenerator_TomCascade_UsesAllThreeToms()
    {
        var fill = FillGenerator.GenerateFill(DrumMap.GeneralMidi, 16, FillStyle.TomCascade);
        Assert.Equal("fill-tom-cascade", fill.Name);

        var voices = fill.Layers.Select(l => l.Voice).ToHashSet();
        Assert.Contains(DrumVoice.HighTom, voices);
        Assert.Contains(DrumVoice.MidTom, voices);
        Assert.Contains(DrumVoice.LowTom, voices);
    }

    [Fact]
    public void FillGenerator_BuildUp_HasIncreasingDensity()
    {
        var fill = FillGenerator.GenerateFill(DrumMap.GeneralMidi, 16, FillStyle.BuildUp);
        Assert.Equal("fill-build-up", fill.Name);

        var kickLayer = fill.Layers.First(l => l.Voice == DrumVoice.Kick).Pattern;
        var activeSteps = kickLayer.GetActiveSteps();
        Assert.True(activeSteps.Count >= 4);
    }

    [Fact]
    public void FillGenerator_Breakdown_IsSparse()
    {
        var fill = FillGenerator.GenerateFill(DrumMap.GeneralMidi, 16, FillStyle.Breakdown);
        Assert.Equal("fill-breakdown", fill.Name);

        // Each layer in a breakdown should have exactly 1 hit
        foreach (var (_, pattern) in fill.Layers)
        {
            Assert.Single(pattern.GetActiveSteps());
        }
    }

    // ── PatternLibrary new patterns ──

    [Theory]
    [InlineData("breakbeat")]
    [InlineData("half-time")]
    [InlineData("shuffle")]
    [InlineData("latin-clave")]
    [InlineData("bossa-nova")]
    public void PatternLibrary_NewPatterns_AreValid(string name)
    {
        var pattern = PatternLibrary.Get(name);
        Assert.NotNull(pattern);
        Assert.Equal(name, pattern.Name);
        Assert.True(pattern.Layers.Length >= 2, $"Pattern '{name}' should have at least 2 layers");

        // Every layer should have valid steps
        foreach (var (_, bp) in pattern.Layers)
        {
            Assert.Equal(16, bp.StepsPerMeasure);
            Assert.True(bp.GetActiveSteps().Count > 0, $"Layer '{bp.Name}' should have active steps");
        }
    }

    [Fact]
    public void PatternLibrary_ContainsAllExpectedPatterns()
    {
        var names = PatternLibrary.Names.ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("rock", names);
        Assert.Contains("house", names);
        Assert.Contains("breakbeat", names);
        Assert.Contains("half-time", names);
        Assert.Contains("shuffle", names);
        Assert.Contains("latin-clave", names);
        Assert.Contains("bossa-nova", names);
    }
}
