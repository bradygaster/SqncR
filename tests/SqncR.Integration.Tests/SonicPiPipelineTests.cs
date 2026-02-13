using SqncR.SonicPi;

namespace SqncR.Integration.Tests;

/// <summary>
/// Integration tests validating the Sonic Pi pipeline:
/// Ruby code generation, OSC client behavior, and instrument activation.
/// </summary>
public class SonicPiPipelineTests
{
    [Fact]
    public void GenerateSynthSetup_ProducesValidRubySyntax()
    {
        var code = RubyCodeGenerator.GenerateSynthSetup("prophet");

        Assert.Contains("use_synth", code);
        Assert.Contains(":prophet", code);
        // Must not contain C# artifacts
        Assert.DoesNotContain("System.", code);
        Assert.DoesNotContain("null", code);
    }

    [Fact]
    public void GenerateSynthSetup_WithParameters_IncludesSetStatements()
    {
        var parameters = new Dictionary<string, object>
        {
            ["cutoff"] = 80.0,
            ["amp"] = 0.7
        };

        var code = RubyCodeGenerator.GenerateSynthSetup("tb303", parameters);

        Assert.Contains("use_synth :tb303", code);
        Assert.Contains("set :cutoff", code);
        Assert.Contains("set :amp", code);
    }

    [Fact]
    public void GenerateLiveLoop_HasProperBlockStructure()
    {
        var notes = new[] { 60, 64, 67 };
        var code = RubyCodeGenerator.GenerateLiveLoop("melody", "prophet", notes, 120);

        Assert.Contains("live_loop :melody do", code);
        Assert.Contains("use_synth :prophet", code);
        Assert.Contains("use_bpm 120", code);
        Assert.Contains("notes.each do |n|", code);
        Assert.Contains("play n", code);
        Assert.Contains("sleep 0.5", code);
        // Block must close properly
        Assert.EndsWith("end", code.TrimEnd());
    }

    [Fact]
    public void GenerateLiveLoop_WithFx_WrapsInFxBlocks()
    {
        var notes = new[] { 60, 64, 67 };
        var fx = new Dictionary<string, double>
        {
            ["reverb"] = 0.8,
            ["echo"] = 0.3
        };

        var code = RubyCodeGenerator.GenerateLiveLoop("ambient", "dark_ambience", notes, 90, fx);

        Assert.Contains("with_fx :reverb", code);
        Assert.Contains("with_fx :echo", code);
        Assert.Contains("use_synth :dark_ambience", code);
    }

    [Fact]
    public void SonicPiInstrument_Activate_SetsIsActiveAndGeneratesCode()
    {
        var instrument = new SonicPiInstrument("Lead", "prophet", channel: 1);
        instrument.FxChain["reverb"] = 0.5;

        // OscClient sends UDP (fire-and-forget), so this won't throw even
        // without Sonic Pi running — it just sends a packet into the void.
        using var client = new OscClient(port: 19999);
        instrument.Activate(client);

        Assert.True(instrument.IsActive);
    }

    [Fact]
    public void SonicPiInstrument_Deactivate_SetsIsActiveFalse()
    {
        var instrument = new SonicPiInstrument("Lead", "prophet");
        using var client = new OscClient(port: 19998);

        instrument.Activate(client);
        Assert.True(instrument.IsActive);

        instrument.Deactivate(client);
        Assert.False(instrument.IsActive);
    }

    [Fact]
    public void OscClient_IsAvailable_ReturnsTrueForUdp()
    {
        // UDP is connectionless — IsAvailable sends a probe packet.
        // On localhost with a random port, this should succeed (UDP send doesn't fail
        // on unreachable ports because it's fire-and-forget).
        using var client = new OscClient(port: 19997);
        var available = client.IsAvailable();

        // UDP send typically succeeds regardless of whether anyone is listening
        Assert.True(available);
    }

    [Fact]
    public void FullPipeline_InstrumentGeneratesCorrectRubyForNotes()
    {
        // Simulate the pipeline: create instrument → generate play note code
        var instrument = new SonicPiInstrument("Bass", "tb303", channel: 2);

        // Verify the synth setup code
        var setupCode = RubyCodeGenerator.GenerateSynthSetup(instrument.SynthName);
        Assert.Contains(":tb303", setupCode);

        // Verify play note code for specific MIDI notes
        var noteCode = RubyCodeGenerator.GeneratePlayNote(60, 0.5, 100);
        Assert.Contains("play 60", noteCode);
        Assert.Contains("sustain:", noteCode);
        Assert.Contains("amp:", noteCode);

        // Verify drone generation
        var droneCode = RubyCodeGenerator.GenerateDrone("dark_ambience", 48,
            new Dictionary<string, double> { ["cutoff"] = 70 });
        Assert.Contains("live_loop :drone do", droneCode);
        Assert.Contains("play 48", droneCode);
        Assert.Contains("cutoff:", droneCode);
    }
}
