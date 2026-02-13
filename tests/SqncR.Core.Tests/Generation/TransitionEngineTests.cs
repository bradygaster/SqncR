using SqncR.Core.Generation;
using SqncR.Theory;

namespace SqncR.Core.Tests.Generation;

public class TransitionEngineTests
{
    private const int Ppq = 480;
    private const int TicksPerMeasure = 4 * Ppq; // 4/4 time = 1920 ticks

    [Fact]
    public void TempoTransition_RampsLinearlyOverBars()
    {
        var engine = new TransitionEngine(120.0);
        engine.StartTempoTransition(180.0, 2, currentTick: 0, ticksPerMeasure: TicksPerMeasure);

        Assert.Equal(TransitionEngine.TransitionState.Active, engine.TempoTransition);

        // Halfway through (1 bar = 1920 ticks)
        engine.Tick(TicksPerMeasure, TicksPerMeasure);
        Assert.Equal(150.0, engine.CurrentEffectiveTempo, precision: 1);

        // Complete (2 bars = 3840 ticks)
        engine.Tick(2 * TicksPerMeasure, TicksPerMeasure);
        Assert.Equal(180.0, engine.CurrentEffectiveTempo, precision: 1);
        Assert.Equal(TransitionEngine.TransitionState.None, engine.TempoTransition);
    }

    [Fact]
    public void TempoTransition_QuarterWay_InterpolatesCorrectly()
    {
        var engine = new TransitionEngine(100.0);
        engine.StartTempoTransition(200.0, 4, currentTick: 0, ticksPerMeasure: TicksPerMeasure);

        // 1 bar out of 4 = 25%
        engine.Tick(TicksPerMeasure, TicksPerMeasure);
        Assert.Equal(125.0, engine.CurrentEffectiveTempo, precision: 1);
    }

    [Fact]
    public void ScaleTransition_AllowsNotesFromBothScales()
    {
        var engine = new TransitionEngine();
        var cMajor = Scale.Major(60);       // C Major: C D E F G A B
        var aPentMinor = Scale.PentatonicMinor(69); // A Pentatonic Minor: A C D E G

        engine.StartScaleTransition(cMajor, aPentMinor, 2, currentTick: 0, ticksPerMeasure: TicksPerMeasure);

        Assert.Equal(TransitionEngine.TransitionState.Active, engine.ScaleTransition);

        // Tick partway through
        engine.Tick(TicksPerMeasure, TicksPerMeasure);

        // C (60) is in C Major → allowed
        Assert.True(engine.IsNoteAllowed(60));

        // A (69) is in A Pentatonic Minor → allowed
        Assert.True(engine.IsNoteAllowed(69));

        // F (65) is in C Major only → allowed during transition
        Assert.True(engine.IsNoteAllowed(65));

        // F# (66) is in neither scale → not allowed
        Assert.False(engine.IsNoteAllowed(66));
    }

    [Fact]
    public void ScaleTransition_SnapsToTargetAfterCompletion()
    {
        var engine = new TransitionEngine();
        var cMajor = Scale.Major(60);
        var dMajor = Scale.Major(62); // D Major: D E F# G A B C#

        engine.StartScaleTransition(cMajor, dMajor, 1, currentTick: 0, ticksPerMeasure: TicksPerMeasure);

        // Complete the transition
        engine.Tick(TicksPerMeasure, TicksPerMeasure);

        Assert.Equal(TransitionEngine.TransitionState.None, engine.ScaleTransition);

        // After transition, IsNoteAllowed returns true for everything (no active transition)
        Assert.True(engine.IsNoteAllowed(60)); // caller uses normal scale logic
        Assert.True(engine.IsNoteAllowed(66)); // F# — would be in target, but transition is done
    }

    [Fact]
    public void NoTransition_InstantChangePreservesExistingBehavior()
    {
        var engine = new TransitionEngine(120.0);

        // No transition started — tempo stays at initial
        Assert.Equal(120.0, engine.CurrentEffectiveTempo);
        Assert.Equal(TransitionEngine.TransitionState.None, engine.TempoTransition);
        Assert.Equal(TransitionEngine.TransitionState.None, engine.ScaleTransition);

        // IsNoteAllowed always true when no transition
        Assert.True(engine.IsNoteAllowed(60));
        Assert.True(engine.IsNoteAllowed(66));
    }

    [Fact]
    public void ZeroBarTransition_IsInstant()
    {
        var engine = new TransitionEngine(120.0);
        engine.StartTempoTransition(200.0, bars: 0, currentTick: 0, ticksPerMeasure: TicksPerMeasure);

        Assert.Equal(200.0, engine.CurrentEffectiveTempo);
        Assert.Equal(TransitionEngine.TransitionState.None, engine.TempoTransition);
    }

    [Fact]
    public void ZeroBarScaleTransition_IsInstant()
    {
        var engine = new TransitionEngine();
        var cMajor = Scale.Major(60);
        var dMajor = Scale.Major(62);

        engine.StartScaleTransition(cMajor, dMajor, bars: 0, currentTick: 0, ticksPerMeasure: TicksPerMeasure);

        Assert.Equal(TransitionEngine.TransitionState.None, engine.ScaleTransition);
        // No transition = IsNoteAllowed returns true
        Assert.True(engine.IsNoteAllowed(60));
    }

    [Fact]
    public void MultipleTransitions_NewOverridesOld()
    {
        var engine = new TransitionEngine(100.0);

        // Start first transition: 100 → 200 over 4 bars
        engine.StartTempoTransition(200.0, 4, currentTick: 0, ticksPerMeasure: TicksPerMeasure);

        // Tick to halfway (2 bars) → should be at 150
        engine.Tick(2 * TicksPerMeasure, TicksPerMeasure);
        Assert.Equal(150.0, engine.CurrentEffectiveTempo, precision: 1);

        // Start new transition from current (150) → 80 over 2 bars
        engine.StartTempoTransition(80.0, 2, currentTick: 2 * TicksPerMeasure, ticksPerMeasure: TicksPerMeasure);

        // Tick to halfway of new transition (1 bar further)
        engine.Tick(3 * TicksPerMeasure, TicksPerMeasure);
        Assert.Equal(115.0, engine.CurrentEffectiveTempo, precision: 1); // 150 + (80-150)*0.5

        // Complete new transition
        engine.Tick(4 * TicksPerMeasure, TicksPerMeasure);
        Assert.Equal(80.0, engine.CurrentEffectiveTempo, precision: 1);
    }

    [Fact]
    public void MultipleScaleTransitions_NewOverridesOld()
    {
        var engine = new TransitionEngine();
        var cMajor = Scale.Major(60);
        var dMajor = Scale.Major(62);
        var eMinor = Scale.Minor(64);

        // Start first scale transition
        engine.StartScaleTransition(cMajor, dMajor, 4, currentTick: 0, ticksPerMeasure: TicksPerMeasure);
        Assert.Equal(TransitionEngine.TransitionState.Active, engine.ScaleTransition);

        // Start new one before first finishes
        engine.StartScaleTransition(dMajor, eMinor, 2, currentTick: TicksPerMeasure, ticksPerMeasure: TicksPerMeasure);

        // During new transition, old scale is now D Major, new is E Minor
        engine.Tick(2 * TicksPerMeasure, TicksPerMeasure);
        Assert.Equal(TransitionEngine.TransitionState.Active, engine.ScaleTransition);

        // D (62) in D Major → allowed
        Assert.True(engine.IsNoteAllowed(62));
        // E (64) in E Minor → allowed
        Assert.True(engine.IsNoteAllowed(64));
    }

    [Fact]
    public void Tick_ReturnsTrueWhenTransitionActive()
    {
        var engine = new TransitionEngine(120.0);
        engine.StartTempoTransition(180.0, 2, currentTick: 0, ticksPerMeasure: TicksPerMeasure);

        bool active = engine.Tick(TicksPerMeasure, TicksPerMeasure);
        Assert.True(active);

        // Complete it
        engine.Tick(2 * TicksPerMeasure, TicksPerMeasure);
        active = engine.Tick(2 * TicksPerMeasure + 1, TicksPerMeasure);
        Assert.False(active);
    }

    [Fact]
    public void SetTempo_UpdatesEffectiveTempoDirectly()
    {
        var engine = new TransitionEngine(120.0);
        engine.SetTempo(140.0);
        Assert.Equal(140.0, engine.CurrentEffectiveTempo);
    }

    [Fact]
    public void TempoTransition_NegativeBarsTreatedAsInstant()
    {
        var engine = new TransitionEngine(120.0);
        engine.StartTempoTransition(200.0, bars: -1, currentTick: 0, ticksPerMeasure: TicksPerMeasure);

        Assert.Equal(200.0, engine.CurrentEffectiveTempo);
        Assert.Equal(TransitionEngine.TransitionState.None, engine.TempoTransition);
    }

    [Fact]
    public void TempoTransition_DecreasingTempo()
    {
        var engine = new TransitionEngine(180.0);
        engine.StartTempoTransition(60.0, 2, currentTick: 0, ticksPerMeasure: TicksPerMeasure);

        // Halfway
        engine.Tick(TicksPerMeasure, TicksPerMeasure);
        Assert.Equal(120.0, engine.CurrentEffectiveTempo, precision: 1);

        // Complete
        engine.Tick(2 * TicksPerMeasure, TicksPerMeasure);
        Assert.Equal(60.0, engine.CurrentEffectiveTempo, precision: 1);
    }
}
