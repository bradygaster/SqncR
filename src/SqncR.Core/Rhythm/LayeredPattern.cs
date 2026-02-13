using System.Collections.Immutable;

namespace SqncR.Core.Rhythm;

/// <summary>
/// A set of layered BeatPatterns — kick + snare + hi-hat etc.
/// Used by PatternLibrary to represent complete drum grooves.
/// </summary>
public sealed class LayeredPattern
{
    /// <summary>Name of this groove (e.g., "house", "hip-hop").</summary>
    public string Name { get; }

    /// <summary>The individual voice/pattern layers.</summary>
    public ImmutableArray<(DrumVoice Voice, BeatPattern Pattern)> Layers { get; }

    public LayeredPattern(string name, IEnumerable<(DrumVoice Voice, BeatPattern Pattern)> layers)
    {
        Name = name;
        Layers = layers.ToImmutableArray();
    }

    /// <summary>Load this layered pattern into a StepSequencer.</summary>
    public StepSequencer ToSequencer(int ticksPerQuarterNote = 480)
    {
        var seq = new StepSequencer(ticksPerQuarterNote);
        foreach (var (voice, pattern) in Layers)
            seq.AddLayer(voice, pattern);
        return seq;
    }
}
