namespace SqncR.Core.Rhythm;

/// <summary>
/// Plays layered beat patterns over time. Advances through steps and emits events.
/// Decoupled from MIDI — outputs SequencerEvents with tick times.
/// </summary>
public sealed class StepSequencer
{
    private readonly List<(DrumVoice Voice, BeatPattern Pattern)> _layers = new();
    private SwingProfile _swing = SwingProfile.Straight;
    private int _ticksPerStep;

    /// <summary>Ticks per quarter note (PPQ). Default 480 matches the project standard.</summary>
    public int TicksPerQuarterNote { get; }

    /// <summary>Steps per measure across all layers. Determined by the first pattern added.</summary>
    public int StepsPerMeasure { get; private set; }

    /// <summary>Ticks per full measure.</summary>
    public int TicksPerMeasure => _ticksPerStep * StepsPerMeasure;

    public StepSequencer(int ticksPerQuarterNote = 480)
    {
        if (ticksPerQuarterNote <= 0)
            throw new ArgumentOutOfRangeException(nameof(ticksPerQuarterNote));
        TicksPerQuarterNote = ticksPerQuarterNote;
    }

    /// <summary>Add a pattern layer for a drum voice.</summary>
    public StepSequencer AddLayer(DrumVoice voice, BeatPattern pattern)
    {
        if (_layers.Count == 0)
        {
            StepsPerMeasure = pattern.StepsPerMeasure;
            // 16 steps per measure in 4/4 = each step is a 16th note = 1/4 of a quarter note
            _ticksPerStep = (TicksPerQuarterNote * 4) / pattern.StepsPerMeasure;
        }
        else if (pattern.StepsPerMeasure != StepsPerMeasure)
        {
            throw new ArgumentException(
                $"Pattern has {pattern.StepsPerMeasure} steps but sequencer expects {StepsPerMeasure}.");
        }

        _layers.Add((voice, pattern));
        return this;
    }

    /// <summary>Set the swing profile for timing adjustments.</summary>
    public StepSequencer WithSwing(SwingProfile swing)
    {
        _swing = swing;
        return this;
    }

    /// <summary>
    /// Get all events for one measure, starting at the given tick offset.
    /// </summary>
    public IReadOnlyList<SequencerEvent> GetMeasureEvents(long measureStartTick = 0)
    {
        var events = new List<SequencerEvent>();

        foreach (var (voice, pattern) in _layers)
        {
            for (int step = 0; step < pattern.StepsPerMeasure; step++)
            {
                var info = pattern.Steps[step];
                if (!info.IsActive)
                    continue;

                long baseTick = measureStartTick + (step * _ticksPerStep);
                long swungTick = _swing.ApplySwing(step, baseTick, _ticksPerStep);

                events.Add(new SequencerEvent(
                    Tick: swungTick,
                    StepIndex: step,
                    Voice: voice,
                    Velocity: info.Velocity,
                    Probability: info.Probability));
            }
        }

        events.Sort((a, b) => a.Tick.CompareTo(b.Tick));
        return events;
    }

    /// <summary>
    /// Get events for a specific step index across all layers.
    /// </summary>
    public IReadOnlyList<SequencerEvent> GetEventsAtStep(int stepIndex, long measureStartTick = 0)
    {
        if (stepIndex < 0 || stepIndex >= StepsPerMeasure)
            throw new ArgumentOutOfRangeException(nameof(stepIndex));

        var events = new List<SequencerEvent>();

        foreach (var (voice, pattern) in _layers)
        {
            var info = pattern.Steps[stepIndex];
            if (!info.IsActive)
                continue;

            long baseTick = measureStartTick + (stepIndex * _ticksPerStep);
            long swungTick = _swing.ApplySwing(stepIndex, baseTick, _ticksPerStep);

            events.Add(new SequencerEvent(
                Tick: swungTick,
                StepIndex: stepIndex,
                Voice: voice,
                Velocity: info.Velocity,
                Probability: info.Probability));
        }

        return events;
    }
}
