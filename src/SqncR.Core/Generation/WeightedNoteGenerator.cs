namespace SqncR.Core.Generation;

/// <summary>
/// Musically-aware note generator using weighted random selection.
/// Favors root and fifth, prefers stepwise motion, limits large leaps, and inserts occasional rests.
/// </summary>
public sealed class WeightedNoteGenerator : INoteGenerator
{
    private readonly double _restProbability;
    private readonly Random _random;
    private int _lastNote = -1;

    public string Name => "Weighted";

    public WeightedNoteGenerator(double restProbability = 0.15, Random? random = null)
    {
        _restProbability = restProbability;
        _random = random ?? Random.Shared;
    }

    public int? NextNote(GenerationState state)
    {
        // Rest gate
        if (_random.NextDouble() < _restProbability)
            return null;

        var notes = state.Scale.GetNotesInOctave(state.Octave);
        if (notes.Count == 0) return null;

        var weights = new double[notes.Count];

        for (int i = 0; i < notes.Count; i++)
        {
            int degreeIndex = i;

            // Base weight by scale degree
            if (degreeIndex == 0)
                weights[i] = 3; // Root
            else if (degreeIndex == 4 && notes.Count >= 7)
                weights[i] = 2; // Fifth in 7-note scales
            else
                weights[i] = 1;

            // Stepwise motion bonus: adjacent to last played note
            if (_lastNote >= 0)
            {
                int semitoneDistance = Math.Abs(notes[i] - _lastNote);

                // Adjacent notes get a bonus
                if (semitoneDistance <= 2)
                    weights[i] += 1;

                // Large leap penalty: >5 semitones, only 10% chance
                if (semitoneDistance > 5)
                    weights[i] *= 0.1;
            }
        }

        // Weighted random selection
        double totalWeight = 0;
        for (int i = 0; i < weights.Length; i++)
            totalWeight += weights[i];

        double roll = _random.NextDouble() * totalWeight;
        double cumulative = 0;
        int selectedIndex = 0;

        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative)
            {
                selectedIndex = i;
                break;
            }
        }

        int selectedNote = notes[selectedIndex];

        // Clamp to valid MIDI range
        selectedNote = Math.Clamp(selectedNote, 0, 127);

        _lastNote = selectedNote;
        return selectedNote;
    }
}
