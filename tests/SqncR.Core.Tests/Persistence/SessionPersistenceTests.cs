using SqncR.Core.Generation;
using SqncR.Core.Persistence;
using SqncR.Core.Rhythm;
using SqncR.Theory;

namespace SqncR.Core.Tests.Persistence;

public class SessionPersistenceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SessionStore _store;

    public SessionPersistenceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "sqncr-tests-" + Guid.NewGuid().ToString("N"));
        _store = new SessionStore(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrips_Correctly()
    {
        var state = new GenerationState
        {
            Tempo = 140,
            Scale = ScaleLibrary.Get("Dorian", 62), // D4
            Octave = 5,
            MelodicChannel = 2,
            DrumChannel = 10,
            IsPlaying = true,
            DrumPattern = PatternLibrary.Get("jazz"),
        };

        var session = SessionState.FromGenerationState(state, "test-session");
        await _store.SaveAsync(session);

        var loaded = await _store.LoadAsync("test-session");

        Assert.Equal("test-session", loaded.Name);
        Assert.Equal(140, loaded.Tempo);
        Assert.Equal("Dorian", loaded.ScaleName);
        Assert.Equal("D4", loaded.RootNote);
        Assert.Equal("jazz", loaded.PatternName);
        Assert.Equal(5, loaded.Octave);
        Assert.Equal(2, loaded.MelodicChannel);
        Assert.Equal(10, loaded.DrumChannel);
        Assert.True(loaded.IsPlaying);
    }

    [Fact]
    public async Task Load_MissingSession_ThrowsFileNotFoundException()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _store.LoadAsync("nonexistent"));
    }

    [Fact]
    public async Task List_ReturnsSavedSessions()
    {
        var state = new GenerationState();

        await _store.SaveAsync(SessionState.FromGenerationState(state, "alpha"));
        await _store.SaveAsync(SessionState.FromGenerationState(state, "beta"));
        await _store.SaveAsync(SessionState.FromGenerationState(state, "gamma"));

        var names = await _store.ListAsync();

        Assert.Equal(3, names.Count);
        Assert.Contains("alpha", names);
        Assert.Contains("beta", names);
        Assert.Contains("gamma", names);
    }

    [Fact]
    public async Task List_EmptyDirectory_ReturnsEmpty()
    {
        var names = await _store.ListAsync();
        Assert.Empty(names);
    }

    [Fact]
    public async Task Delete_RemovesSessionFile()
    {
        var state = new GenerationState();
        await _store.SaveAsync(SessionState.FromGenerationState(state, "to-delete"));

        var namesBefore = await _store.ListAsync();
        Assert.Contains("to-delete", namesBefore);

        await _store.DeleteAsync("to-delete");

        var namesAfter = await _store.ListAsync();
        Assert.DoesNotContain("to-delete", namesAfter);
    }

    [Fact]
    public async Task Delete_MissingSession_DoesNotThrow()
    {
        await _store.DeleteAsync("nonexistent"); // should not throw
    }

    [Fact]
    public async Task Load_InvalidJson_ThrowsJsonException()
    {
        Directory.CreateDirectory(_tempDir);
        var filePath = Path.Combine(_tempDir, "bad.json");
        await File.WriteAllTextAsync(filePath, "not valid json {{{");

        await Assert.ThrowsAsync<System.Text.Json.JsonException>(
            () => _store.LoadAsync("bad"));
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
            IsPlaying = false,
        };
        state.NoteGenerator = new ScaleWalkGenerator();

        var session = SessionState.FromGenerationState(state, "snapshot");

        Assert.Equal("snapshot", session.Name);
        Assert.Equal(90, session.Tempo);
        Assert.Equal("Blues", session.ScaleName);
        Assert.Equal("C3", session.RootNote);
        Assert.Equal(3, session.Octave);
        Assert.Equal(5, session.MelodicChannel);
        Assert.Equal(9, session.DrumChannel);
        Assert.False(session.IsPlaying);
        Assert.Equal("Scale Walk", session.GeneratorName);
    }

    [Fact]
    public async Task Save_Overwrites_ExistingSession()
    {
        var state1 = new GenerationState { Tempo = 100 };
        var state2 = new GenerationState { Tempo = 200 };

        await _store.SaveAsync(SessionState.FromGenerationState(state1, "overwrite-me"));
        await _store.SaveAsync(SessionState.FromGenerationState(state2, "overwrite-me"));

        var loaded = await _store.LoadAsync("overwrite-me");
        Assert.Equal(200, loaded.Tempo);

        var names = await _store.ListAsync();
        Assert.Single(names);
    }
}
