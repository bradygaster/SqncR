using SqncR.Core.Instruments;
using SqncR.Core.Rhythm;
using SqncR.Theory;

namespace SqncR.Core.Generation;

/// <summary>
/// Commands that can be enqueued to modify the generation engine mid-playback.
/// Thread-safe when used via System.Threading.Channels.
/// </summary>
public abstract record GenerationCommand
{
    public sealed record SetTempo(double Bpm) : GenerationCommand;
    public sealed record SetScale(Scale Scale) : GenerationCommand;
    public sealed record SetPattern(LayeredPattern Pattern) : GenerationCommand;
    public sealed record SetOctave(int Octave) : GenerationCommand;
    public sealed record SetMelodicChannel(int Channel) : GenerationCommand;
    public sealed record SetDrumChannel(int Channel) : GenerationCommand;
    public sealed record SetGenerator(INoteGenerator Generator) : GenerationCommand;
    public sealed record SetTempoSmooth(double Bpm, int TransitionBars = 4) : GenerationCommand;
    public sealed record SetScaleSmooth(Scale Scale, int TransitionBars = 4) : GenerationCommand;
    public sealed record SetVarietyLevel(VarietyLevel Level) : GenerationCommand;
    public sealed record AddInstrument(Instrument Instrument) : GenerationCommand;
    public sealed record RemoveInstrument(string InstrumentId) : GenerationCommand;
    public sealed record Start : GenerationCommand;
    public sealed record Stop : GenerationCommand;
    public sealed record AllNotesOff : GenerationCommand;
}
