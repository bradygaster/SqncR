using SqncR.Core.Generation;
using SqncR.Core.Persistence;
using SqncR.Core.Rhythm;
using SqncR.Theory;

namespace SqncR.Core.Tests.Persistence;

public class SceneTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SceneStore _store;

    public SceneTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "sqncr-scene-tests-" + Guid.NewGuid().ToString("N"));
        _store = new SceneStore(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrips_Correctly()
    {
        var scene = new Scene
        {
            Name = "test-scene",
            Description = "A test scene",
            Tempo = 140,
            ScaleName = "Dorian",
            RootNote = "D4",
            Octave = 5,
            GeneratorName = "Scale Walk",
            VarietyLevel = "Adventurous",
            DrumPatternName = "jazz",
            MelodicChannel = 2,
            DrumChannel = 9,
        };

        await _store.SaveAsync(scene);
        var loaded = await _store.LoadAsync("test-scene");

        Assert.Equal("test-scene", loaded.Name);
        Assert.Equal("A test scene", loaded.Description);
        Assert.Equal(140, loaded.Tempo);
        Assert.Equal("Dorian", loaded.ScaleName);
        Assert.Equal("D4", loaded.RootNote);
        Assert.Equal(5, loaded.Octave);
        Assert.Equal("Scale Walk", loaded.GeneratorName);
        Assert.Equal("Adventurous", loaded.VarietyLevel);
        Assert.Equal("jazz", loaded.DrumPatternName);
        Assert.Equal(2, loaded.MelodicChannel);
        Assert.Equal(9, loaded.DrumChannel);
    }

    [Fact]
    public async Task BuiltInPresets_HaveValidValues()
    {
        var ambientPad = await _store.LoadAsync("ambient-pad");
        Assert.Equal(65, ambientPad.Tempo);
        Assert.Equal("Pentatonic Minor", ambientPad.ScaleName);
        Assert.Equal("C4", ambientPad.RootNote);
        Assert.Equal("Conservative", ambientPad.VarietyLevel);

        var drivingTechno = await _store.LoadAsync("driving-techno");
        Assert.Equal(128, drivingTechno.Tempo);
        Assert.Equal("Natural Minor", drivingTechno.ScaleName);
        Assert.Equal("A3", drivingTechno.RootNote);
        Assert.Equal("Adventurous", drivingTechno.VarietyLevel);

        var chillLofi = await _store.LoadAsync("chill-lofi");
        Assert.Equal(85, chillLofi.Tempo);
        Assert.Equal("Dorian", chillLofi.ScaleName);
        Assert.Equal("D4", chillLofi.RootNote);
        Assert.Equal("Moderate", chillLofi.VarietyLevel);
    }

    [Fact]
    public async Task ListAsync_IncludesBothUserAndBuiltInScenes()
    {
        var userScene = new Scene { Name = "my-scene", Description = "Custom scene" };
        await _store.SaveAsync(userScene);

        var list = await _store.ListAsync();

        Assert.True(list.Count >= 4); // 3 built-in + 1 user
        Assert.Contains(list, s => s.Name == "ambient-pad" && s.IsBuiltIn);
        Assert.Contains(list, s => s.Name == "driving-techno" && s.IsBuiltIn);
        Assert.Contains(list, s => s.Name == "chill-lofi" && s.IsBuiltIn);
        Assert.Contains(list, s => s.Name == "my-scene" && !s.IsBuiltIn);
    }

    [Fact]
    public async Task DeleteAsync_RemovesUserScene()
    {
        var scene = new Scene { Name = "delete-me" };
        await _store.SaveAsync(scene);

        var listBefore = await _store.ListAsync();
        Assert.Contains(listBefore, s => s.Name == "delete-me");

        await _store.DeleteAsync("delete-me");

        var listAfter = await _store.ListAsync();
        Assert.DoesNotContain(listAfter, s => s.Name == "delete-me" && !s.IsBuiltIn);
    }

    [Fact]
    public async Task LoadAsync_MissingScene_ThrowsFileNotFoundException()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _store.LoadAsync("nonexistent-scene"));
    }

    [Fact]
    public async Task DeleteAsync_MissingScene_DoesNotThrow()
    {
        await _store.DeleteAsync("nonexistent"); // should not throw
    }

    [Fact]
    public async Task SaveAsync_Overwrites_ExistingScene()
    {
        var scene1 = new Scene { Name = "overwrite-me", Tempo = 100 };
        var scene2 = new Scene { Name = "overwrite-me", Tempo = 200 };

        await _store.SaveAsync(scene1);
        await _store.SaveAsync(scene2);

        var loaded = await _store.LoadAsync("overwrite-me");
        Assert.Equal(200, loaded.Tempo);
    }

    [Fact]
    public void FromGenerationState_CapturesAllFields()
    {
        var state = new GenerationState
        {
            Tempo = 90,
            Scale = Scale.Blues(48), // C3
            Octave = 3,
            MelodicChannel = 5,
            DrumChannel = 9,
            Variety = new VarietyEngine(VarietyLevel.Adventurous),
        };
        state.NoteGenerator = new ScaleWalkGenerator();

        var scene = Scene.FromGenerationState(state, "snapshot", "test description");

        Assert.Equal("snapshot", scene.Name);
        Assert.Equal("test description", scene.Description);
        Assert.Equal(90, scene.Tempo);
        Assert.Equal("Blues", scene.ScaleName);
        Assert.Equal("C3", scene.RootNote);
        Assert.Equal(3, scene.Octave);
        Assert.Equal(5, scene.MelodicChannel);
        Assert.Equal(9, scene.DrumChannel);
        Assert.Equal("Scale Walk", scene.GeneratorName);
        Assert.Equal("Adventurous", scene.VarietyLevel);
    }

    [Fact]
    public async Task UserScene_ShadowsBuiltIn_InList()
    {
        // Save a user scene with same name as built-in
        var userScene = new Scene { Name = "ambient-pad", Description = "My custom ambient" };
        await _store.SaveAsync(userScene);

        var list = await _store.ListAsync();

        // Should only appear once, as user scene (not built-in)
        var ambientEntries = list.Where(s => s.Name == "ambient-pad").ToList();
        Assert.Single(ambientEntries);
        Assert.False(ambientEntries[0].IsBuiltIn);
    }

    [Fact]
    public async Task ListAsync_EmptyDirectory_ReturnsOnlyBuiltIns()
    {
        var list = await _store.ListAsync();

        Assert.Equal(3, list.Count);
        Assert.All(list, s => Assert.True(s.IsBuiltIn));
    }
}
