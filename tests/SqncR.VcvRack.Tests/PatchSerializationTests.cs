using System.Text.Json;

namespace SqncR.VcvRack.Tests;

public class PatchSerializationTests
{
    [Fact]
    public void ToJson_ProducesValidJson()
    {
        var patch = PatchTemplates.BasicSynth();

        var json = patch.ToJson();

        // Should not throw
        var doc = JsonDocument.Parse(json);
        Assert.NotNull(doc);
    }

    [Fact]
    public void ToJson_ContainsExpectedModuleSlugs()
    {
        var patch = PatchTemplates.BasicSynth();

        var json = patch.ToJson();

        Assert.Contains("\"MIDI-CV\"", json);
        Assert.Contains("\"VCO\"", json);
        Assert.Contains("\"VCF\"", json);
        Assert.Contains("\"ADSR\"", json);
        Assert.Contains("\"VCA-1\"", json);
        Assert.Contains("\"AudioInterface\"", json);
    }

    [Fact]
    public void ToJson_ContainsPluginNames()
    {
        var patch = PatchTemplates.BasicSynth();

        var json = patch.ToJson();

        Assert.Contains("\"Core\"", json);
        Assert.Contains("\"Fundamental\"", json);
    }

    [Fact]
    public void ToJson_ContainsCableConnections()
    {
        var patch = PatchTemplates.BasicSynth();

        var json = patch.ToJson();
        var doc = JsonDocument.Parse(json);

        var cables = doc.RootElement.GetProperty("cables");
        Assert.Equal(6, cables.GetArrayLength());

        // Each cable has required fields
        var firstCable = cables[0];
        Assert.True(firstCable.TryGetProperty("outputModuleId", out _));
        Assert.True(firstCable.TryGetProperty("inputModuleId", out _));
        Assert.True(firstCable.TryGetProperty("outputId", out _));
        Assert.True(firstCable.TryGetProperty("inputId", out _));
        Assert.True(firstCable.TryGetProperty("color", out _));
    }

    [Fact]
    public void ToJson_ContainsModulePositions()
    {
        var patch = PatchTemplates.BasicSynth();

        var json = patch.ToJson();
        var doc = JsonDocument.Parse(json);

        var modules = doc.RootElement.GetProperty("modules");
        foreach (var module in modules.EnumerateArray())
        {
            var pos = module.GetProperty("pos");
            Assert.Equal(2, pos.GetArrayLength());
        }
    }

    [Fact]
    public void ToJson_ContainsVersion()
    {
        var patch = PatchTemplates.BasicSynth();

        var json = patch.ToJson();

        Assert.Contains("\"version\"", json);
        Assert.Contains("\"2.0\"", json);
    }

    [Fact]
    public void SaveAs_CreatesFile()
    {
        var patch = PatchTemplates.BasicSynth();
        var tempPath = Path.Combine(Path.GetTempPath(), $"test_patch_{Guid.NewGuid()}.vcv");

        try
        {
            patch.SaveAs(tempPath);

            Assert.True(File.Exists(tempPath));
            Assert.True(new FileInfo(tempPath).Length > 0);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public void SaveAs_NullPath_ThrowsArgumentException()
    {
        var patch = PatchTemplates.BasicSynth();

        Assert.Throws<ArgumentNullException>(() => patch.SaveAs(null!));
    }
}
