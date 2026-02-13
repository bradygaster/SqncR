using SqncR.VcvRack.Models;

namespace SqncR.VcvRack.Tests;

public class PatchBuilderTests
{
    [Fact]
    public void BasicSynth_HasExpectedModuleCount()
    {
        var patch = PatchTemplates.BasicSynth();

        Assert.Equal(6, patch.Modules.Count);
    }

    [Fact]
    public void BasicSynth_HasExpectedCableCount()
    {
        var patch = PatchTemplates.BasicSynth();

        Assert.Equal(6, patch.Cables.Count);
    }

    [Fact]
    public void ModuleIds_AreUnique()
    {
        var patch = PatchTemplates.BasicSynth();

        var ids = patch.Modules.Select(m => m.Id).ToList();
        Assert.Equal(ids.Distinct().Count(), ids.Count);
    }

    [Fact]
    public void CableIds_AreUnique()
    {
        var patch = PatchTemplates.BasicSynth();

        var ids = patch.Cables.Select(c => c.Id).ToList();
        Assert.Equal(ids.Distinct().Count(), ids.Count);
    }

    [Fact]
    public void AutoLayout_PositionsModulesWithoutOverlap()
    {
        var patch = PatchTemplates.BasicSynth();

        var positions = patch.Modules.Select(m => m.PositionX).ToList();
        // All X positions should be unique (no overlap)
        Assert.Equal(positions.Distinct().Count(), positions.Count);
        // Positions should be in ascending order (left to right)
        Assert.Equal(positions.OrderBy(p => p).ToList(), positions);
    }

    [Fact]
    public void AutoLayout_AllModulesOnSameRow()
    {
        var patch = PatchTemplates.BasicSynth();

        var yPositions = patch.Modules.Select(m => m.PositionY).Distinct().ToList();
        Assert.Single(yPositions);
    }

    [Fact]
    public void Cable_InvalidOutputPort_ThrowsArgumentException()
    {
        var builder = new PatchBuilder()
            .AddModule(ModuleLibrary.Vco(), out var vco)
            .AddModule(ModuleLibrary.Vcf(), out var vcf);

        Assert.Throws<ArgumentException>(() => builder.Cable(vco, "NonExistent", vcf, "In"));
    }

    [Fact]
    public void Cable_InvalidInputPort_ThrowsArgumentException()
    {
        var builder = new PatchBuilder()
            .AddModule(ModuleLibrary.Vco(), out var vco)
            .AddModule(ModuleLibrary.Vcf(), out var vcf);

        Assert.Throws<ArgumentException>(() => builder.Cable(vco, "Saw", vcf, "NonExistent"));
    }

    [Fact]
    public void Cable_ValidPorts_ResolvesToCorrectPortIds()
    {
        var patch = new PatchBuilder()
            .AddModule(ModuleLibrary.MidiCv(), out var midi)
            .AddModule(ModuleLibrary.Vco(), out var vco)
            .Cable(midi, "V/Oct", vco, "V/Oct")
            .Build();

        var cable = patch.Cables.Single();
        Assert.Equal(midi.Id, cable.OutputModuleId);
        Assert.Equal(0, cable.OutputPortId); // V/Oct output is port 0
        Assert.Equal(vco.Id, cable.InputModuleId);
        Assert.Equal(0, cable.InputPortId); // V/Oct input is port 0
    }

    [Fact]
    public void AmbientPad_HasLfoModule()
    {
        var patch = PatchTemplates.AmbientPad();

        Assert.Contains(patch.Modules, m => m.Model == "LFO");
    }

    [Fact]
    public void AmbientPad_HasMoreModulesThanBasicSynth()
    {
        var basic = PatchTemplates.BasicSynth();
        var ambient = PatchTemplates.AmbientPad();

        Assert.True(ambient.Modules.Count > basic.Modules.Count);
    }

    [Fact]
    public void BassSynth_UsesSquareWaveFromVco()
    {
        var patch = PatchTemplates.BassSynth();

        // Bass synth should cable VCO Sqr output to VCF
        var vco = patch.Modules.First(m => m.Model == "VCO");
        var sqrPortId = vco.OutputPorts["Sqr"];
        Assert.Contains(patch.Cables, c => c.OutputModuleId == vco.Id && c.OutputPortId == sqrPortId);
    }

    [Fact]
    public void AddModule_NullTemplate_ThrowsArgumentNullException()
    {
        var builder = new PatchBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.AddModule(null!, out _));
    }
}
