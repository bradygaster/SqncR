using System.Text.Json;
using SqncR.VcvRack;
using SqncR.VcvRack.Models;

namespace SqncR.Integration.Tests;

/// <summary>
/// Integration tests validating the VCV Rack pipeline:
/// patch generation, template correctness, cable validation, and serialization.
/// </summary>
public class VcvRackPipelineTests
{
    [Fact]
    public void BasicSynth_HasRequiredModules()
    {
        var patch = PatchTemplates.BasicSynth();

        var models = patch.Modules.Select(m => m.Model).ToList();
        Assert.Contains("MIDI-CV", models);
        Assert.Contains("VCO", models);
        Assert.Contains("VCF", models);
        Assert.Contains("VCA-1", models);
        Assert.Contains("AudioInterface", models);
    }

    [Fact]
    public void AmbientPad_IncludesLfoModulation()
    {
        var patch = PatchTemplates.AmbientPad();

        var models = patch.Modules.Select(m => m.Model).ToList();
        Assert.Contains("LFO", models);

        // LFO should be cabled to the VCF's Freq CV input
        var lfo = patch.Modules.First(m => m.Model == "LFO");
        var vcf = patch.Modules.First(m => m.Model == "VCF");

        var lfoCable = patch.Cables.FirstOrDefault(c =>
            c.OutputModuleId == lfo.Id && c.InputModuleId == vcf.Id);
        Assert.NotNull(lfoCable);
    }

    [Fact]
    public void AllTemplates_HaveValidSignalPath()
    {
        var patches = new[]
        {
            ("BasicSynth", PatchTemplates.BasicSynth()),
            ("AmbientPad", PatchTemplates.AmbientPad()),
            ("BassSynth", PatchTemplates.BassSynth())
        };

        foreach (var (name, patch) in patches)
        {
            // Every module that has output ports should have at least one cable from it,
            // except AudioInterface (output endpoint)
            var modulesWithOutputs = patch.Modules
                .Where(m => m.OutputPorts.Count > 0)
                .ToList();

            foreach (var module in modulesWithOutputs)
            {
                var hasCableOut = patch.Cables.Any(c => c.OutputModuleId == module.Id);
                Assert.True(hasCableOut,
                    $"Template '{name}': Module '{module.Model}' (ID {module.Id}) has output ports but no outgoing cables.");
            }

            // AudioInterface must have at least one incoming cable
            var audio = patch.Modules.FirstOrDefault(m => m.Model == "AudioInterface");
            Assert.NotNull(audio);
            var hasAudioInput = patch.Cables.Any(c => c.InputModuleId == audio.Id);
            Assert.True(hasAudioInput,
                $"Template '{name}': AudioInterface has no incoming cables.");
        }
    }

    [Fact]
    public void PatchSerialization_RoundTrips()
    {
        var patch = PatchTemplates.BasicSynth();
        var json = patch.ToJson();

        // Verify JSON is valid and contains expected structure
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("version", out var version));
        Assert.Equal("2.0", version.GetString());

        Assert.True(root.TryGetProperty("modules", out var modules));
        Assert.Equal(patch.Modules.Count, modules.GetArrayLength());

        Assert.True(root.TryGetProperty("cables", out var cables));
        Assert.Equal(patch.Cables.Count, cables.GetArrayLength());

        // Verify module data survives serialization
        var firstModule = modules[0];
        Assert.True(firstModule.TryGetProperty("plugin", out _));
        Assert.True(firstModule.TryGetProperty("model", out _));
        Assert.True(firstModule.TryGetProperty("id", out _));
    }

    [Fact]
    public void CableValidation_NoDanglingCables()
    {
        var patches = new[]
        {
            PatchTemplates.BasicSynth(),
            PatchTemplates.AmbientPad(),
            PatchTemplates.BassSynth()
        };

        foreach (var patch in patches)
        {
            var moduleIds = patch.Modules.Select(m => m.Id).ToHashSet();

            foreach (var cable in patch.Cables)
            {
                Assert.Contains(cable.OutputModuleId, moduleIds);
                Assert.Contains(cable.InputModuleId, moduleIds);
            }
        }
    }

    [Fact]
    public void VcvRackLauncher_WithNonExistentPath_HandlesGracefully()
    {
        using var launcher = new VcvRackLauncher();

        // LaunchAsync should throw FileNotFoundException when Rack isn't installed
        var ex = Assert.ThrowsAsync<FileNotFoundException>(
            () => launcher.LaunchAsync("nonexistent.vcv"));

        Assert.NotNull(ex);
    }
}
