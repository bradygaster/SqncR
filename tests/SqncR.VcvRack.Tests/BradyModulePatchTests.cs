using SqncR.VcvRack.Models;

namespace SqncR.VcvRack.Tests;

public class BradyModulePatchTests
{
    // ---- Tremor (4-voice drum sequencer) ----

    [Fact]
    public void TremorPatch_HasCorrectModuleCount()
    {
        var patch = PatchTemplates.TremorPatch();
        Assert.Equal(15, patch.Modules.Count);
    }

    [Fact]
    public void TremorPatch_HasMidiGateModule()
    {
        var patch = PatchTemplates.TremorPatch();
        Assert.Contains(patch.Modules, m => m.Model == "MIDI-Gate");
    }

    [Fact]
    public void TremorPatch_HasFourVoiceChains()
    {
        var patch = PatchTemplates.TremorPatch();
        var vcaCount = patch.Modules.Count(m => m.Model == "VCA-1");
        Assert.Equal(4, vcaCount);
    }

    [Fact]
    public void TremorPatch_NoDuplicateModuleIds()
    {
        var patch = PatchTemplates.TremorPatch();
        var ids = patch.Modules.Select(m => m.Id).ToList();
        Assert.Equal(ids.Distinct().Count(), ids.Count);
    }

    [Fact]
    public void TremorPatch_NoDuplicateCableIds()
    {
        var patch = PatchTemplates.TremorPatch();
        var ids = patch.Cables.Select(c => c.Id).ToList();
        Assert.Equal(ids.Distinct().Count(), ids.Count);
    }

    // ---- Tidegate (Euclidean rhythm) ----

    [Fact]
    public void TidegatePatch_HasCorrectModuleCount()
    {
        var patch = PatchTemplates.TidegatePatch();
        Assert.Equal(7, patch.Modules.Count);
    }

    [Fact]
    public void TidegatePatch_NoDuplicateModuleIds()
    {
        var patch = PatchTemplates.TidegatePatch();
        var ids = patch.Modules.Select(m => m.Id).ToList();
        Assert.Equal(ids.Distinct().Count(), ids.Count);
    }

    [Fact]
    public void TidegatePatch_NoDuplicateCableIds()
    {
        var patch = PatchTemplates.TidegatePatch();
        var ids = patch.Cables.Select(c => c.Id).ToList();
        Assert.Equal(ids.Distinct().Count(), ids.Count);
    }

    // ---- Driftwave (wavetable pad) ----

    [Fact]
    public void DriftwavePatch_HasCorrectModuleCount()
    {
        var patch = PatchTemplates.DriftwavePatch();
        Assert.Equal(10, patch.Modules.Count);
    }

    [Fact]
    public void DriftwavePatch_NoDuplicateModuleIds()
    {
        var patch = PatchTemplates.DriftwavePatch();
        var ids = patch.Modules.Select(m => m.Id).ToList();
        Assert.Equal(ids.Distinct().Count(), ids.Count);
    }

    [Fact]
    public void DriftwavePatch_NoDuplicateCableIds()
    {
        var patch = PatchTemplates.DriftwavePatch();
        var ids = patch.Cables.Select(c => c.Id).ToList();
        Assert.Equal(ids.Distinct().Count(), ids.Count);
    }

    // ---- Rustle (stereo delay) ----

    [Fact]
    public void RustlePatch_HasCorrectModuleCount()
    {
        var patch = PatchTemplates.RustlePatch();
        Assert.Equal(7, patch.Modules.Count);
    }

    [Fact]
    public void RustlePatch_NoDuplicateModuleIds()
    {
        var patch = PatchTemplates.RustlePatch();
        var ids = patch.Modules.Select(m => m.Id).ToList();
        Assert.Equal(ids.Distinct().Count(), ids.Count);
    }

    [Fact]
    public void RustlePatch_NoDuplicateCableIds()
    {
        var patch = PatchTemplates.RustlePatch();
        var ids = patch.Cables.Select(c => c.Id).ToList();
        Assert.Equal(ids.Distinct().Count(), ids.Count);
    }

    // ---- BradyJamSession (combined) ----

    [Fact]
    public void BradyJamSession_ContainsAllSubPatchModuleTypes()
    {
        var patch = PatchTemplates.BradyJamSession();

        var models = patch.Modules.Select(m => m.Model).Distinct().ToHashSet();

        Assert.Contains("MIDI-Gate", models);
        Assert.Contains("MIDI-CC", models);
        Assert.Contains("MIDI-CV", models);
        Assert.Contains("VCO", models);
        Assert.Contains("VCF", models);
        Assert.Contains("VCA-1", models);
        Assert.Contains("ADSR", models);
        Assert.Contains("LFO", models);
        Assert.Contains("Noise", models);
        Assert.Contains("Delay", models);
        Assert.Contains("VCMixer", models);
        Assert.Contains("AudioInterface", models);
    }

    [Fact]
    public void BradyJamSession_HasStereoOutput()
    {
        var patch = PatchTemplates.BradyJamSession();

        var audioModule = patch.Modules.First(m => m.Model == "AudioInterface");
        var audioCables = patch.Cables
            .Where(c => c.InputModuleId == audioModule.Id)
            .Select(c => c.InputPortId)
            .Distinct()
            .ToList();

        Assert.True(audioCables.Count >= 2, "Audio output should have at least 2 distinct input ports (stereo).");
    }

    // ---- Cross-patch validation ----

    [Theory]
    [MemberData(nameof(AllBradyPatches))]
    public void AllBradyPatches_CablesReferenceValidModules(string name, VcvPatch patch)
    {
        var moduleIds = patch.Modules.Select(m => m.Id).ToHashSet();

        foreach (var cable in patch.Cables)
        {
            Assert.True(moduleIds.Contains(cable.OutputModuleId),
                $"[{name}] Cable {cable.Id} references non-existent output module {cable.OutputModuleId}.");
            Assert.True(moduleIds.Contains(cable.InputModuleId),
                $"[{name}] Cable {cable.Id} references non-existent input module {cable.InputModuleId}.");
        }
    }

    public static TheoryData<string, VcvPatch> AllBradyPatches() => new()
    {
        { "Tremor", PatchTemplates.TremorPatch() },
        { "Tidegate", PatchTemplates.TidegatePatch() },
        { "Driftwave", PatchTemplates.DriftwavePatch() },
        { "Rustle", PatchTemplates.RustlePatch() },
        { "BradyJamSession", PatchTemplates.BradyJamSession() },
    };
}
