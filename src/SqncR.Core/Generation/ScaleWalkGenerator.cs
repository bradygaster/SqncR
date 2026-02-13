namespace SqncR.Core.Generation;

/// <summary>
/// Walks up and down the current scale linearly. Extracted from the original EmitMelodyTick logic.
/// </summary>
public sealed class ScaleWalkGenerator : INoteGenerator
{
    public string Name => "Scale Walk";

    public int? NextNote(GenerationState state)
    {
        var notes = state.Scale.GetNotesInOctave(state.Octave);
        if (notes.Count == 0) return null;

        int index = state.MelodyScaleIndex;
        if (index >= notes.Count) index = notes.Count - 1;
        if (index < 0) index = 0;

        int note = notes[index];

        // Advance scale walk
        state.MelodyScaleIndex += state.MelodyDirection;
        if (state.MelodyScaleIndex >= notes.Count)
        {
            state.MelodyScaleIndex = notes.Count - 2;
            state.MelodyDirection = -1;
        }
        else if (state.MelodyScaleIndex < 0)
        {
            state.MelodyScaleIndex = 1;
            state.MelodyDirection = 1;
        }

        return note;
    }
}
