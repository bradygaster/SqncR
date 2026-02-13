using SqncR.Core.Rhythm;
using SqncR.Theory;

namespace SqncR.Core.Generation;

/// <summary>
/// Mutable state read by the generation loop. Updated via the command queue.
/// </summary>
public sealed class GenerationState
{
    /// <summary>Tempo in beats per minute.</summary>
    public double Tempo { get; set; } = 120.0;

    /// <summary>Current musical scale for melodic generation.</summary>
    public Scale Scale { get; set; } = Scale.PentatonicMinor(60); // C4 pentatonic minor

    /// <summary>Current drum pattern.</summary>
    public LayeredPattern? DrumPattern { get; set; }

    /// <summary>Whether the engine is actively generating and playing.</summary>
    public bool IsPlaying { get; set; }

    /// <summary>MIDI channel for melodic output (1-based, GM standard).</summary>
    public int MelodicChannel { get; set; } = 1;

    /// <summary>MIDI channel for drums (1-based, GM standard channel 10).</summary>
    public int DrumChannel { get; set; } = 10;

    /// <summary>Base octave for melodic generation.</summary>
    public int Octave { get; set; } = 4;

    /// <summary>Pluggable note generation algorithm.</summary>
    public INoteGenerator NoteGenerator { get; set; } = new WeightedNoteGenerator();

    /// <summary>Index tracking the current position in the scale walk for melody.</summary>
    internal int MelodyScaleIndex { get; set; }

    /// <summary>Direction of scale walk: +1 ascending, -1 descending.</summary>
    internal int MelodyDirection { get; set; } = 1;
}
