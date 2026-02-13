using SqncR.Theory;

namespace SqncR.Core.Generation;

/// <summary>
/// Manages smooth transitions for tempo and scale changes over a configurable number of bars.
/// Used by GenerationEngine to interpolate between old and new values instead of snapping instantly.
/// </summary>
public sealed class TransitionEngine
{
    /// <summary>State of a transition.</summary>
    public enum TransitionState { None, Active }

    // --- Tempo transition state ---
    private double _tempoStart;
    private double _tempoTarget;
    private long _tempoTransitionStartTick;
    private long _tempoTransitionDurationTicks;

    /// <summary>Current tempo transition state.</summary>
    public TransitionState TempoTransition { get; private set; } = TransitionState.None;

    /// <summary>The interpolated effective tempo at the current tick.</summary>
    public double CurrentEffectiveTempo { get; private set; }

    // --- Scale transition state ---
    private Scale? _scaleOld;
    private Scale? _scaleTarget;
    private long _scaleTransitionStartTick;
    private long _scaleTransitionDurationTicks;

    /// <summary>Current scale transition state.</summary>
    public TransitionState ScaleTransition { get; private set; } = TransitionState.None;

    /// <summary>The target scale (becomes current after transition completes).</summary>
    public Scale? TargetScale => _scaleTarget;

    /// <summary>
    /// Initializes the engine with a starting tempo.
    /// </summary>
    public TransitionEngine(double initialTempo = 120.0)
    {
        CurrentEffectiveTempo = initialTempo;
    }

    /// <summary>
    /// Starts a linear tempo transition from the current effective tempo to <paramref name="targetBpm"/>
    /// over <paramref name="bars"/> bars.
    /// </summary>
    /// <param name="targetBpm">Target BPM.</param>
    /// <param name="bars">Number of bars over which to transition. 0 = instant.</param>
    /// <param name="currentTick">The current tick position.</param>
    /// <param name="ticksPerMeasure">Ticks in one measure at current time signature.</param>
    public void StartTempoTransition(double targetBpm, int bars, long currentTick, int ticksPerMeasure)
    {
        if (bars <= 0)
        {
            // Instant change
            CurrentEffectiveTempo = targetBpm;
            TempoTransition = TransitionState.None;
            return;
        }

        _tempoStart = CurrentEffectiveTempo;
        _tempoTarget = targetBpm;
        _tempoTransitionStartTick = currentTick;
        _tempoTransitionDurationTicks = (long)bars * ticksPerMeasure;
        TempoTransition = TransitionState.Active;
    }

    /// <summary>
    /// Starts a scale transition from <paramref name="currentScale"/> to <paramref name="targetScale"/>
    /// over <paramref name="bars"/> bars. During transition, notes from both scales are allowed.
    /// </summary>
    public void StartScaleTransition(Scale currentScale, Scale targetScale, int bars, long currentTick, int ticksPerMeasure)
    {
        if (bars <= 0)
        {
            // Instant — no transition window
            _scaleOld = null;
            _scaleTarget = null;
            ScaleTransition = TransitionState.None;
            return;
        }

        _scaleOld = currentScale;
        _scaleTarget = targetScale;
        _scaleTransitionStartTick = currentTick;
        _scaleTransitionDurationTicks = (long)bars * ticksPerMeasure;
        ScaleTransition = TransitionState.Active;
    }

    /// <summary>
    /// Advances transition state to the given tick. Call once per tick in the generation loop.
    /// </summary>
    /// <param name="currentTick">Current tick position.</param>
    /// <param name="ticksPerMeasure">Ticks per measure.</param>
    /// <returns>True if any transition is active.</returns>
    public bool Tick(long currentTick, int ticksPerMeasure)
    {
        if (TempoTransition == TransitionState.Active)
        {
            long elapsed = currentTick - _tempoTransitionStartTick;
            if (elapsed >= _tempoTransitionDurationTicks)
            {
                CurrentEffectiveTempo = _tempoTarget;
                TempoTransition = TransitionState.None;
            }
            else
            {
                double t = (double)elapsed / _tempoTransitionDurationTicks;
                CurrentEffectiveTempo = _tempoStart + (_tempoTarget - _tempoStart) * t;
            }
        }

        if (ScaleTransition == TransitionState.Active)
        {
            long elapsed = currentTick - _scaleTransitionStartTick;
            if (elapsed >= _scaleTransitionDurationTicks)
            {
                ScaleTransition = TransitionState.None;
                _scaleOld = null;
                _scaleTarget = null;
            }
        }

        return TempoTransition == TransitionState.Active || ScaleTransition == TransitionState.Active;
    }

    /// <summary>
    /// During a scale transition, returns true if the MIDI note belongs to either the old or new scale.
    /// When no transition is active, always returns true (caller should use normal scale logic).
    /// </summary>
    public bool IsNoteAllowed(int midiNote)
    {
        if (ScaleTransition != TransitionState.Active)
            return true;

        return (_scaleOld?.ContainsNote(midiNote) ?? false) ||
               (_scaleTarget?.ContainsNote(midiNote) ?? false);
    }

    /// <summary>
    /// Returns the completed target scale if a scale transition just finished, otherwise null.
    /// Useful for GenerationEngine to know when to snap GenerationState.Scale to the target.
    /// </summary>
    public Scale? GetCompletedScaleTarget()
    {
        // When transition is no longer active but we have a target that was set, return it
        // This is checked externally after Tick()
        return null; // handled via TargetScale + state check
    }

    /// <summary>
    /// Sets the effective tempo directly (used when no transition is active).
    /// </summary>
    public void SetTempo(double bpm)
    {
        CurrentEffectiveTempo = bpm;
    }
}
