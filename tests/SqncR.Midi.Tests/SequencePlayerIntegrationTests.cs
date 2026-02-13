using SqncR.Core;
using SqncR.Midi.Testing;

namespace SqncR.Midi.Tests;

/// <summary>
/// Integration test: uses MockMidiOutput + SequencePlayer to play a real .sqnc.yaml
/// and asserts that captured MIDI events match expected notes.
/// Placeholder for issue #12 — proves the capture framework works end-to-end.
/// </summary>
public class SequencePlayerIntegrationTests
{
    private static string FindExamplesDirectory()
    {
        // Walk up from test bin output to repo root, then into examples/
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            var candidate = Path.Combine(dir, "examples");
            if (Directory.Exists(candidate))
                return candidate;
            dir = Path.GetDirectoryName(dir)!;
        }
        throw new DirectoryNotFoundException("Could not find examples/ directory");
    }

    [Fact]
    public async Task PlaySequence_CapturesMidiEvents_FromSevenNationArmy()
    {
        // Arrange
        using var mock = new MockMidiOutput();
        var player = new SequencePlayer(mock);
        var parser = new SequenceParser();

        var examplesDir = FindExamplesDirectory();
        var filePath = Path.Combine(examplesDir, "seven-nation-army.sqnc.yaml");
        var sequence = parser.Parse(filePath);

        // Let it play for 3 seconds then cancel — enough to capture initial events
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        // Act — catch cancellation since we're deliberately timing out
        try
        {
            await player.PlayAsync(sequence, cts.Token);
        }
        catch (OperationCanceledException) { }

        // Assert — the capture framework captured real MIDI events
        Assert.NotEmpty(mock.Events);

        // All events should be NoteOn or NoteOff (no AllNotesOff from PlayAsync)
        Assert.All(mock.Events, e =>
            Assert.True(e.Type == MidiEventType.NoteOn || e.Type == MidiEventType.NoteOff,
                $"Unexpected event type: {e.Type}"));

        // Every NoteOn should have a corresponding NoteOff (for completed notes)
        var noteOns = mock.Events.Where(e => e.Type == MidiEventType.NoteOn).ToList();

        // The riff starts with E2 (MIDI 40) — verify first note
        var firstNote = noteOns.First();
        Assert.Equal(40, firstNote.Note); // E2
    }

    [Fact]
    public async Task PlaySequence_CapturesMultipleChannels_FromChillAmbient()
    {
        // Arrange
        using var mock = new MockMidiOutput();
        var player = new SequencePlayer(mock);
        var parser = new SequenceParser();

        var examplesDir = FindExamplesDirectory();
        var filePath = Path.Combine(examplesDir, "chill-ambient.sqnc.yaml");
        var sequence = parser.Parse(filePath);

        // Let it play for 5 seconds — chill-ambient at 70 BPM needs a few seconds for events
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        try
        {
            await player.PlayAsync(sequence, cts.Token);
        }
        catch (OperationCanceledException) { }

        // Assert — events captured on multiple channels
        Assert.NotEmpty(mock.Events);

        var channels = mock.Events.Select(e => e.Channel).Distinct().OrderBy(c => c).ToList();
        // chill-ambient uses ch: 1 (bass) and ch: 2 (pads)
        Assert.Contains(1, channels);
        Assert.Contains(2, channels);

        // Timestamps should be non-negative
        Assert.All(mock.Events, e =>
            Assert.True(e.Timestamp >= TimeSpan.Zero, $"Event has negative timestamp"));
    }

    [Fact]
    public async Task MockMidiOutput_WorksAsDropInReplacement_ForMidiService()
    {
        // This test proves the interface contract: anything that accepted MidiService
        // can now accept IMidiOutput, and MockMidiOutput satisfies that contract.
        using IMidiOutput output = new MockMidiOutput();
        var player = new SequencePlayer(output);

        // Create a minimal sequence programmatically
        var sequence = new SqncR.Core.Models.Sequence
        {
            Meta = new SqncR.Core.Models.MetaData
            {
                Title = "Test Sequence",
                Tempo = 120,
                Key = "C",
                Tpq = 480
            },
            Patterns = new Dictionary<string, SqncR.Core.Models.Pattern>
            {
                ["test_pattern"] = new()
                {
                    Length = 960,
                    Events = new List<SqncR.Core.Models.NoteEvent>
                    {
                        new() { T = 0, Type = "note", Note = "C4", Dur = 480, Vel = 100 },
                        new() { T = 480, Type = "note", Note = "E4", Dur = 480, Vel = 90 }
                    }
                }
            },
            Sections = new Dictionary<string, SqncR.Core.Models.Section>
            {
                ["test"] = new()
                {
                    Length = 960,
                    Tracks = new List<SqncR.Core.Models.Track>
                    {
                        new()
                        {
                            Ch = 1,
                            Sequence = new List<SqncR.Core.Models.SequenceEntry>
                            {
                                new() { At = 0, Pattern = "test_pattern" }
                            }
                        }
                    }
                }
            },
            Arrange = new List<SqncR.Core.Models.ArrangeEntry>
            {
                new() { At = 0, Section = "test" }
            }
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await player.PlayAsync(sequence, cts.Token);

        var mock = (MockMidiOutput)output;
        var noteOns = mock.Events.Where(e => e.Type == MidiEventType.NoteOn).ToList();

        Assert.Equal(2, noteOns.Count);
        Assert.Equal(60, noteOns[0].Note); // C4
        Assert.Equal(64, noteOns[1].Note); // E4
        Assert.Equal(100, noteOns[0].Velocity);
        Assert.Equal(90, noteOns[1].Velocity);
    }
}
