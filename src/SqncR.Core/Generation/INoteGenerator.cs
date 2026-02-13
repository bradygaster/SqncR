namespace SqncR.Core.Generation;

/// <summary>
/// Pluggable algorithm that picks the next MIDI note for melodic generation.
/// </summary>
public interface INoteGenerator
{
    /// <summary>
    /// Returns the next MIDI note to play, given current state. Null means rest.
    /// </summary>
    int? NextNote(GenerationState state);

    /// <summary>
    /// Human-readable name for status display.
    /// </summary>
    string Name { get; }
}
