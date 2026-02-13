namespace SqncR.SonicPi.Tests;

public class RubyCodeGeneratorTests
{
    [Fact]
    public void GenerateSynthSetup_BasicSynth_ReturnsUseSynthStatement()
    {
        var result = RubyCodeGenerator.GenerateSynthSetup("prophet");

        Assert.Equal("use_synth :prophet", result);
    }

    [Fact]
    public void GenerateSynthSetup_WithParameters_IncludesSetStatements()
    {
        var parameters = new Dictionary<string, object>
        {
            { "cutoff", 80 },
            { "release", 0.5 }
        };

        var result = RubyCodeGenerator.GenerateSynthSetup("tb303", parameters);

        Assert.Contains("use_synth :tb303", result);
        Assert.Contains("set :cutoff, 80", result);
        Assert.Contains("set :release, 0.5", result);
    }

    [Fact]
    public void GenerateSynthSetup_NullParameters_ReturnsOnlyUseSynth()
    {
        var result = RubyCodeGenerator.GenerateSynthSetup("beep", null);

        Assert.Equal("use_synth :beep", result);
    }

    [Fact]
    public void GenerateSynthSetup_EmptyParameters_ReturnsOnlyUseSynth()
    {
        var result = RubyCodeGenerator.GenerateSynthSetup("beep", new Dictionary<string, object>());

        Assert.Equal("use_synth :beep", result);
    }

    [Fact]
    public void GenerateSynthSetup_NullOrWhitespaceName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => RubyCodeGenerator.GenerateSynthSetup(""));
        Assert.Throws<ArgumentException>(() => RubyCodeGenerator.GenerateSynthSetup("  "));
        Assert.Throws<ArgumentNullException>(() => RubyCodeGenerator.GenerateSynthSetup(null!));
    }

    [Fact]
    public void GenerateLiveLoop_ValidInput_ContainsLiveLoopBlock()
    {
        var notes = new[] { 60, 64, 67 };
        var result = RubyCodeGenerator.GenerateLiveLoop("ambient", "prophet", notes, 120.0);

        Assert.Contains("live_loop :ambient do", result);
        Assert.Contains("use_synth :prophet", result);
        Assert.Contains("notes = [60, 64, 67]", result);
        Assert.Contains("use_bpm 120", result);
        Assert.Contains("play n", result);
        Assert.Contains("sleep 0.5", result);
        Assert.EndsWith("end", result);
    }

    [Fact]
    public void GenerateLiveLoop_WithFx_WrapsFxBlocks()
    {
        var notes = new[] { 60 };
        var fx = new Dictionary<string, double> { { "reverb", 0.8 } };

        var result = RubyCodeGenerator.GenerateLiveLoop("pad", "blade", notes, 90.0, fx);

        Assert.Contains("with_fx :reverb, mix: 0.8 do", result);
    }

    [Fact]
    public void GeneratePlayNote_ConvertsVelocityToAmp()
    {
        var result = RubyCodeGenerator.GeneratePlayNote(60, 1.0, 127);

        Assert.Equal("play 60, sustain: 1, amp: 1", result);
    }

    [Fact]
    public void GeneratePlayNote_HalfVelocity_ProducesHalfAmp()
    {
        var result = RubyCodeGenerator.GeneratePlayNote(48, 0.5, 64);

        Assert.Contains("play 48", result);
        Assert.Contains("sustain: 0.5", result);
        // 64/127 ≈ 0.5039...
        Assert.Contains("amp: 0.50", result);
    }

    [Fact]
    public void GeneratePlayNote_ZeroVelocity_ProducesZeroAmp()
    {
        var result = RubyCodeGenerator.GeneratePlayNote(60, 1.0, 0);

        Assert.Contains("amp: 0", result);
    }

    [Fact]
    public void GenerateFxChain_MultipleEffects_NestsCorrectly()
    {
        var effects = new Dictionary<string, double>
        {
            { "reverb", 0.7 },
            { "echo", 0.4 }
        };

        var result = RubyCodeGenerator.GenerateFxChain(effects);

        Assert.Contains("with_fx :reverb, mix: 0.7 do", result);
        Assert.Contains("with_fx :echo, mix: 0.4 do", result);
        // Should have nested structure
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.Length >= 4); // two with_fx, placeholder, two ends
    }

    [Fact]
    public void GenerateFxChain_EmptyEffects_ReturnsComment()
    {
        var result = RubyCodeGenerator.GenerateFxChain(new Dictionary<string, double>());

        Assert.Equal("# no effects", result);
    }

    [Fact]
    public void GenerateDrone_BasicDrone_GeneratesLiveLoop()
    {
        var result = RubyCodeGenerator.GenerateDrone("dark_ambience", 40);

        Assert.Contains("live_loop :drone do", result);
        Assert.Contains("use_synth :dark_ambience", result);
        Assert.Contains("play 40, sustain: 8", result);
        Assert.Contains("sleep 8", result);
    }

    [Fact]
    public void GenerateDrone_WithParameters_IncludesParams()
    {
        var parameters = new Dictionary<string, double>
        {
            { "amp", 0.3 },
            { "cutoff", 70 }
        };

        var result = RubyCodeGenerator.GenerateDrone("hollow", 36, parameters);

        Assert.Contains("amp: 0.3", result);
        Assert.Contains("cutoff: 70", result);
    }

    [Fact]
    public void GenerateDrone_NullParameters_GeneratesWithoutParams()
    {
        var result = RubyCodeGenerator.GenerateDrone("blade", 48, null);

        Assert.Contains("play 48, sustain: 8", result);
        Assert.DoesNotContain("amp:", result);
    }

    [Fact]
    public void BuiltInSynths_ContainsExpectedSynths()
    {
        Assert.Contains("prophet", RubyCodeGenerator.BuiltInSynths);
        Assert.Contains("tb303", RubyCodeGenerator.BuiltInSynths);
        Assert.Contains("dark_ambience", RubyCodeGenerator.BuiltInSynths);
        Assert.Contains("piano", RubyCodeGenerator.BuiltInSynths);
        Assert.Equal(8, RubyCodeGenerator.BuiltInSynths.Count);
    }

    [Fact]
    public void BuiltInFx_ContainsExpectedEffects()
    {
        Assert.Contains("reverb", RubyCodeGenerator.BuiltInFx);
        Assert.Contains("echo", RubyCodeGenerator.BuiltInFx);
        Assert.Contains("distortion", RubyCodeGenerator.BuiltInFx);
        Assert.Equal(7, RubyCodeGenerator.BuiltInFx.Count);
    }
}
