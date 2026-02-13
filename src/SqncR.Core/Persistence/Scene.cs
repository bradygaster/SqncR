using SqncR.Core.Generation;

namespace SqncR.Core.Persistence;

/// <summary>
/// A named musical preset capturing the "recipe" for a generation configuration.
/// Simpler than SessionState — captures settings, not runtime state.
/// </summary>
public sealed class Scene
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double Tempo { get; set; } = 120.0;
    public string ScaleName { get; set; } = "Pentatonic Minor";
    public string RootNote { get; set; } = "C4";
    public int Octave { get; set; } = 4;
    public string GeneratorName { get; set; } = "Weighted";
    public string VarietyLevel { get; set; } = "Moderate";
    public string? DrumPatternName { get; set; }
    public int MelodicChannel { get; set; } = 1;
    public int DrumChannel { get; set; } = 10;

    /// <summary>
    /// Captures current generation state into a scene preset.
    /// </summary>
    public static Scene FromGenerationState(GenerationState state, string name, string? description = null)
    {
        return new Scene
        {
            Name = name,
            Description = description,
            Tempo = state.Tempo,
            ScaleName = state.Scale.Name,
            RootNote = FormatRootNote(state.Scale.RootNote),
            Octave = state.Octave,
            GeneratorName = state.NoteGenerator.Name,
            VarietyLevel = (state.Variety?.Level ?? Generation.VarietyLevel.Moderate).ToString(),
            DrumPatternName = state.DrumPattern?.Name,
            MelodicChannel = state.MelodicChannel,
            DrumChannel = state.DrumChannel,
        };
    }

    /// <summary>
    /// Applies this scene to the generation engine via commands.
    /// </summary>
    public void ApplyTo(GenerationEngine engine)
    {
        var midiRoot = NoteParser.Parse(RootNote);
        var scale = Theory.ScaleLibrary.Get(ScaleName, midiRoot);
        engine.Commands.TryWrite(new GenerationCommand.SetTempo(Tempo));
        engine.Commands.TryWrite(new GenerationCommand.SetScale(scale));
        engine.Commands.TryWrite(new GenerationCommand.SetOctave(Octave));
        engine.Commands.TryWrite(new GenerationCommand.SetMelodicChannel(MelodicChannel));
        engine.Commands.TryWrite(new GenerationCommand.SetDrumChannel(DrumChannel));

        if (Enum.TryParse<Generation.VarietyLevel>(VarietyLevel, ignoreCase: true, out var level))
        {
            engine.Commands.TryWrite(new GenerationCommand.SetVarietyLevel(level));
        }

        if (!string.IsNullOrWhiteSpace(DrumPatternName))
        {
            try
            {
                var pattern = Rhythm.PatternLibrary.Get(DrumPatternName);
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

/// <summary>
/// Lightweight summary of a scene for list display.
/// </summary>
public sealed record SceneSummary(string Name, string? Description, bool IsBuiltIn);
