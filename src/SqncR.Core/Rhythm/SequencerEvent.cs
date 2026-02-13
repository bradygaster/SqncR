namespace SqncR.Core.Rhythm;

/// <summary>
/// A single event emitted by the step sequencer — a drum hit at a specific time.
/// </summary>
/// <param name="Tick">The tick time this event should fire.</param>
/// <param name="StepIndex">Which step in the pattern produced this event.</param>
/// <param name="Voice">The drum voice for this hit.</param>
/// <param name="Velocity">MIDI velocity 0-127.</param>
/// <param name="Probability">Probability this hit actually fires (for generative use).</param>
public readonly record struct SequencerEvent(
    long Tick,
    int StepIndex,
    DrumVoice Voice,
    int Velocity,
    double Probability);
