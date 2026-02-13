using System.Text.Json.Serialization;
using SqncR.Core.Generation;
using SqncR.Core.Rhythm;
using SqncR.Theory;

namespace SqncR.Core.Persistence;

/// <summary>
/// Serializable snapshot of a generation session.
/// </summary>
public sealed class SessionState
{
    public string Name { get; set; } = string.Empty;
    public double Tempo { get; set; } = 120.0;
    public string ScaleName { get; set; } = "Pentatonic Minor";
    public string RootNote { get; set; } = "C4";
    public string? PatternName { get; set; }
    public int Octave { get; set; } = 4;
    public int MelodicChannel { get; set; } = 1;
    public int DrumChannel { get; set; } = 10;
    public string GeneratorName { get; set; } = "Weighted";
    public bool IsPlaying { get; set; }
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Captures current generation state into a serializable snapshot.
    /// </summary>
    public static SessionState FromGenerationState(GenerationState state, string name)
    {
        return new SessionState
        {
            Name = name,
            Tempo = state.Tempo,
            ScaleName = state.Scale.Name,
            RootNote = FormatRootNote(state.Scale.RootNote),
            PatternName = state.DrumPattern?.Name,
            Octave = state.Octave,
            MelodicChannel = state.MelodicChannel,
            DrumChannel = state.DrumChannel,
            GeneratorName = state.NoteGenerator.Name,
            IsPlaying = state.IsPlaying,
            SavedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Restores this session snapshot into the generation state via engine commands.
    /// </summary>
    public void ApplyTo(GenerationEngine engine)
    {
        var midiRoot = NoteParser.Parse(RootNote);
        var scale = ScaleLibrary.Get(ScaleName, midiRoot);
        engine.Commands.TryWrite(new GenerationCommand.SetTempo(Tempo));
        engine.Commands.TryWrite(new GenerationCommand.SetScale(scale));
        engine.Commands.TryWrite(new GenerationCommand.SetOctave(Octave));
        engine.Commands.TryWrite(new GenerationCommand.SetMelodicChannel(MelodicChannel));
        engine.Commands.TryWrite(new GenerationCommand.SetDrumChannel(DrumChannel));

        if (!string.IsNullOrWhiteSpace(PatternName))
        {
            try
            {
                var pattern = PatternLibrary.Get(PatternName);
                engine.Commands.TryWrite(new GenerationCommand.SetPattern(pattern));
            }
            catch (ArgumentException)
            {
                // Gracefully skip missing pattern
            }
        }
    }

    private static string FormatRootNote(int midiNote)
    {
        string[] noteNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
        int octave = (midiNote / 12) - 1;
        int noteIndex = midiNote % 12;
        return $"{noteNames[noteIndex]}{octave}";
    }
}
