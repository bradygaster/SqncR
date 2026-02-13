using SqncR.Core.Rhythm;

namespace SqncR.Core.Tests.Rhythm;

public class StepSequencerTests
{
    [Fact]
    public void SingleLayer_EmitsCorrectEvents()
    {
        var seq = new StepSequencer(480)
            .AddLayer(DrumVoice.Kick, BeatPattern.FourOnTheFloor());

        var events = seq.GetMeasureEvents();

        Assert.Equal(4, events.Count);
        Assert.All(events, e => Assert.Equal(DrumVoice.Kick, e.Voice));
    }

    [Fact]
    public void SingleLayer_CorrectTickTiming()
    {
        var seq = new StepSequencer(480)
            .AddLayer(DrumVoice.Kick, BeatPattern.FourOnTheFloor());

        var events = seq.GetMeasureEvents();
        // 16 steps over 4 beats, PPQ=480 → each step = 120 ticks
        // Kicks on 0, 4, 8, 12 → ticks 0, 480, 960, 1440
        Assert.Equal(0, events[0].Tick);
        Assert.Equal(480, events[1].Tick);
        Assert.Equal(960, events[2].Tick);
        Assert.Equal(1440, events[3].Tick);
    }

    [Fact]
    public void MultipleLayers_CombinesEvents()
    {
        var seq = new StepSequencer(480)
            .AddLayer(DrumVoice.Kick, BeatPattern.FourOnTheFloor())
            .AddLayer(DrumVoice.Snare, BeatPattern.Backbeat());

        var events = seq.GetMeasureEvents();

        // 4 kicks + 2 snares = 6 events
        Assert.Equal(6, events.Count);
        Assert.Equal(4, events.Count(e => e.Voice == DrumVoice.Kick));
        Assert.Equal(2, events.Count(e => e.Voice == DrumVoice.Snare));
    }

    [Fact]
    public void EventsSortedByTick()
    {
        var seq = new StepSequencer(480)
            .AddLayer(DrumVoice.Kick, BeatPattern.FourOnTheFloor())
            .AddLayer(DrumVoice.ClosedHiHat, BeatPattern.OffBeat());

        var events = seq.GetMeasureEvents();

        for (int i = 1; i < events.Count; i++)
            Assert.True(events[i].Tick >= events[i - 1].Tick);
    }

    [Fact]
    public void WithSwing_AdjustsOddStepTiming()
    {
        var seq = new StepSequencer(480)
            .AddLayer(DrumVoice.ClosedHiHat, BeatPattern.Straight())
            .WithSwing(SwingProfile.Full);

        var events = seq.GetMeasureEvents();

        // Step 0 (even) at tick 0 — no swing
        Assert.Equal(0, events[0].Tick);

        // Step 1 (odd) at tick 120 + 40 offset = 160
        var step1 = events.First(e => e.StepIndex == 1);
        Assert.Equal(160, step1.Tick);
    }

    [Fact]
    public void GetEventsAtStep_ReturnsCorrectSlice()
    {
        var seq = new StepSequencer(480)
            .AddLayer(DrumVoice.Kick, BeatPattern.FourOnTheFloor())
            .AddLayer(DrumVoice.Snare, BeatPattern.Backbeat());

        // Step 4: both kick and snare hit
        var events = seq.GetEventsAtStep(4);
        Assert.Equal(2, events.Count);
        Assert.Contains(events, e => e.Voice == DrumVoice.Kick);
        Assert.Contains(events, e => e.Voice == DrumVoice.Snare);
    }

    [Fact]
    public void GetEventsAtStep_EmptyStep_ReturnsEmpty()
    {
        var seq = new StepSequencer(480)
            .AddLayer(DrumVoice.Kick, BeatPattern.FourOnTheFloor());

        var events = seq.GetEventsAtStep(1);
        Assert.Empty(events);
    }

    [Fact]
    public void MismatchedSteps_Throws()
    {
        var seq = new StepSequencer(480)
            .AddLayer(DrumVoice.Kick, BeatPattern.FourOnTheFloor(16));

        Assert.Throws<ArgumentException>(() =>
            seq.AddLayer(DrumVoice.Snare, BeatPattern.FourOnTheFloor(8)));
    }

    [Fact]
    public void MeasureStartTick_OffsetsAllEvents()
    {
        var seq = new StepSequencer(480)
            .AddLayer(DrumVoice.Kick, BeatPattern.FourOnTheFloor());

        var events = seq.GetMeasureEvents(measureStartTick: 1920);

        Assert.Equal(1920, events[0].Tick);
        Assert.Equal(2400, events[1].Tick);
    }

    [Fact]
    public void TicksPerMeasure_Correct()
    {
        var seq = new StepSequencer(480)
            .AddLayer(DrumVoice.Kick, BeatPattern.FourOnTheFloor(16));

        // 4 beats * 480 ticks = 1920 ticks per measure
        Assert.Equal(1920, seq.TicksPerMeasure);
    }
}
