using SqncR.Core.Rhythm;

namespace SqncR.Core.Tests.Rhythm;

public class EuclideanGeneratorTests
{
    // ── Musicological patterns from Toussaint's paper ──

    [Fact]
    public void E2_5_KhafifERamal()
    {
        var result = EuclideanGenerator.Generate(5, 2);
        Assert.Equal([true, false, true, false, false], result);
    }

    [Fact]
    public void E3_8_Tresillo()
    {
        var result = EuclideanGenerator.Generate(8, 3);
        Assert.Equal([true, false, false, true, false, false, true, false], result);
    }

    [Fact]
    public void E5_8_Cinquillo()
    {
        var result = EuclideanGenerator.Generate(8, 5);
        Assert.Equal([true, false, true, true, false, true, true, false], result);
    }

    [Fact]
    public void E3_4()
    {
        var result = EuclideanGenerator.Generate(4, 3);
        // Bjorklund produces [1,1,1,0] — valid rotation of E(3,4)
        Assert.Equal(3, result.Count(x => x));
        Assert.Equal(4, result.Length);
    }

    [Fact]
    public void E4_12()
    {
        var result = EuclideanGenerator.Generate(12, 4);
        Assert.Equal(
            [true, false, false, true, false, false, true, false, false, true, false, false],
            result);
    }

    [Fact]
    public void E5_16_BossaNova()
    {
        var result = EuclideanGenerator.Generate(16, 5);
        Assert.Equal(
            [true, false, false, true, false, false, true, false, false, true, false, false, true, false, false, false],
            result);
    }

    [Fact]
    public void E7_16_WestAfrican()
    {
        var result = EuclideanGenerator.Generate(16, 7);
        Assert.Equal(
            [true, false, false, true, false, true, false, true, false, false, true, false, true, false, true, false],
            result);
    }

    // ── Edge cases ──

    [Fact]
    public void E0_8_AllRests()
    {
        var result = EuclideanGenerator.Generate(8, 0);
        Assert.All(result, b => Assert.False(b));
    }

    [Fact]
    public void E8_8_AllHits()
    {
        var result = EuclideanGenerator.Generate(8, 8);
        Assert.All(result, b => Assert.True(b));
    }

    [Fact]
    public void E1_1_SingleHit()
    {
        var result = EuclideanGenerator.Generate(1, 1);
        Assert.Equal([true], result);
    }

    [Fact]
    public void Generate_InvalidSteps_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EuclideanGenerator.Generate(0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => EuclideanGenerator.Generate(-1, 0));
    }

    [Fact]
    public void Generate_HitsOutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EuclideanGenerator.Generate(8, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => EuclideanGenerator.Generate(8, 9));
    }

    // ── Rotation tests ──

    [Fact]
    public void Rotation_ShiftsRight()
    {
        // E(3,8) = [1,0,0,1,0,0,1,0], rotation=1 → [0,1,0,0,1,0,0,1]
        var result = EuclideanGenerator.Generate(8, 3, rotation: 1);
        Assert.Equal([false, true, false, false, true, false, false, true], result);
    }

    [Fact]
    public void Rotation_FullCycle_ReturnsOriginal()
    {
        var original = EuclideanGenerator.Generate(8, 3);
        var rotated = EuclideanGenerator.Generate(8, 3, rotation: 8);
        Assert.Equal(original, rotated);
    }

    [Fact]
    public void Rotation_Negative_WrapsCorrectly()
    {
        // Negative rotation = left shift
        var rightBy7 = EuclideanGenerator.Generate(8, 3, rotation: 7);
        var leftBy1 = EuclideanGenerator.Generate(8, 3, rotation: -1);
        Assert.Equal(rightBy7, leftBy1);
    }

    // ── ToBeatPattern tests ──

    [Fact]
    public void ToBeatPattern_CorrectStepsAndName()
    {
        var pattern = EuclideanGenerator.ToBeatPattern(8, 3, name: "test-tresillo");

        Assert.Equal(8, pattern.StepsPerMeasure);
        Assert.Equal("test-tresillo", pattern.Name);
        Assert.Equal([0, 3, 6], pattern.GetActiveSteps());
    }

    [Fact]
    public void ToBeatPattern_CustomVelocityAndProbability()
    {
        var pattern = EuclideanGenerator.ToBeatPattern(8, 3, velocity: 80, probability: 0.7);
        var hit = pattern.Steps[0]; // first step is a hit

        Assert.True(hit.IsActive);
        Assert.Equal(80, hit.Velocity);
        Assert.Equal(0.7, hit.Probability, precision: 5);
    }

    // ── Named preset tests ──

    [Fact]
    public void Tresillo_IsE3_8()
    {
        var preset = EuclideanGenerator.Tresillo();
        Assert.Equal("euclidean-tresillo", preset.Name);
        Assert.Equal(8, preset.StepsPerMeasure);
        Assert.Equal([0, 3, 6], preset.GetActiveSteps());
    }

    [Fact]
    public void Cinquillo_IsE5_8()
    {
        var preset = EuclideanGenerator.Cinquillo();
        Assert.Equal("euclidean-cinquillo", preset.Name);
        Assert.Equal([0, 2, 3, 5, 6], preset.GetActiveSteps());
    }

    [Fact]
    public void BossaNova_IsE5_16()
    {
        var preset = EuclideanGenerator.BossaNova();
        Assert.Equal("euclidean-bossa", preset.Name);
        Assert.Equal(16, preset.StepsPerMeasure);
        Assert.Equal(5, preset.GetActiveSteps().Count);
    }

    [Fact]
    public void WestAfrican_IsE7_16()
    {
        var preset = EuclideanGenerator.WestAfrican();
        Assert.Equal("euclidean-west-african", preset.Name);
        Assert.Equal(7, preset.GetActiveSteps().Count);
    }

    [Fact]
    public void Dense_IsE11_16()
    {
        var preset = EuclideanGenerator.Dense();
        Assert.Equal("euclidean-dense", preset.Name);
        Assert.Equal(11, preset.GetActiveSteps().Count);
    }

    [Fact]
    public void Sparse_IsE3_16()
    {
        var preset = EuclideanGenerator.Sparse();
        Assert.Equal("euclidean-sparse", preset.Name);
        Assert.Equal(3, preset.GetActiveSteps().Count);
    }

    // ── PatternLibrary integration ──

    [Theory]
    [InlineData("euclidean")]
    [InlineData("euclidean-tresillo")]
    [InlineData("euclidean-cinquillo")]
    [InlineData("euclidean-bossa")]
    [InlineData("euclidean-west-african")]
    [InlineData("euclidean-sparse")]
    [InlineData("euclidean-dense")]
    public void PatternLibrary_ResolvesEuclideanPatterns(string name)
    {
        var pattern = PatternLibrary.Get(name);

        Assert.NotNull(pattern);
        Assert.Equal(name, pattern.Name);
        Assert.NotEmpty(pattern.Layers);
    }

    [Fact]
    public void PatternLibrary_EuclideanDefault_HasKickLayer()
    {
        var pattern = PatternLibrary.Get("euclidean");
        Assert.Contains(pattern.Layers, l => l.Voice == DrumVoice.Kick);
        Assert.Contains(pattern.Layers, l => l.Voice == DrumVoice.RimShot);
    }
}
