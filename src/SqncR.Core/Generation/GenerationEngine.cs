using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SqncR.Core.Instruments;
using SqncR.Core.Rhythm;
using SqncR.Midi.Testing;

namespace SqncR.Core.Generation;

/// <summary>
/// Background service that runs the core generation loop.
/// Ticks at 480 TPQ, generates drum and melodic events, sends MIDI via IMidiOutput.
/// </summary>
public sealed class GenerationEngine : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new("SqncR.Generation");

    /// <summary>Ticks per quarter note — matches the project standard.</summary>
    public const int Ppq = 480;

    private readonly GenerationState _state;
    private readonly IMidiOutput _midiOutput;
    private readonly ILogger<GenerationEngine> _logger;
    private readonly Channel<GenerationCommand> _commandChannel;
    private readonly TransitionEngine _transition;
    private readonly NoteTracker _noteTracker;
    private readonly HealthMonitor _healthMonitor;
    private readonly SessionTelemetry _sessionTelemetry = new();
    private readonly ChannelRouter _channelRouter;
    private readonly InstrumentTelemetry _instrumentTelemetry = new();
    private readonly PerInstrumentNoteTracker _perInstrumentNoteTracker = new();

    /// <summary>Writer for enqueuing commands from external callers (MCP tools).</summary>
    public ChannelWriter<GenerationCommand> Commands => _commandChannel.Writer;

    /// <summary>Provides access to the note tracker for health reporting.</summary>
    public NoteTracker NoteTracker => _noteTracker;

    /// <summary>Provides access to the health monitor for health reporting.</summary>
    public HealthMonitor HealthMonitor => _healthMonitor;

    /// <summary>Provides access to session telemetry for observability.</summary>
    public SessionTelemetry SessionTelemetry => _sessionTelemetry;

    public GenerationEngine(
        GenerationState state,
        IMidiOutput midiOutput,
        ILogger<GenerationEngine> logger)
    {
        _state = state;
        _midiOutput = midiOutput;
        _logger = logger;
        _commandChannel = Channel.CreateUnbounded<GenerationCommand>(
            new UnboundedChannelOptions { SingleReader = true });
        _transition = new TransitionEngine(state.Tempo);
        _noteTracker = new NoteTracker();
        _healthMonitor = new HealthMonitor(_noteTracker);
        _channelRouter = new ChannelRouter(state);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield immediately so StartAsync returns and the host can continue booting
        await Task.Yield();

        _logger.LogInformation("GenerationEngine starting");

        var stopwatch = new Stopwatch();
        long currentTick = 0;
        StepSequencer? sequencer = null;
        IReadOnlyList<SequencerEvent>? measureEvents = null;
        long measureStartTick = 0;
        int measureEventIndex = 0;
        int lastMelodyNotePlayed = -1;
        int measureNumber = 0;
        int eventsInCurrentMeasure = 0;
        Activity? sessionActivity = null;
        Activity? measureActivity = null;
        int _lastHealthSnapshotMeasure = -1;
        const int HealthSnapshotIntervalMeasures = 40; // ~5 min at 120 BPM (4/4)

        try
        {
            stopwatch.Start();

            while (!stoppingToken.IsCancellationRequested)
            {
                // Process all pending commands
                ProcessCommands(ref sequencer, ref measureEvents, ref measureStartTick,
                    ref measureEventIndex, ref currentTick,
                    ref sessionActivity, ref measureActivity, ref measureNumber,
                    ref eventsInCurrentMeasure);

                if (!_state.IsPlaying)
                {
                    // When not playing, yield and wait for commands
                    await Task.Delay(10, stoppingToken).ConfigureAwait(false);
                    stopwatch.Restart();
                    currentTick = 0;
                    measureStartTick = 0;
                    measureEventIndex = 0;
                    continue;
                }

                // Advance transitions
                int transitionTicksPerMeasure = 4 * Ppq; // 4/4 time
                _transition.Tick(currentTick, transitionTicksPerMeasure);

                // If a tempo transition completed, update state
                if (_transition.TempoTransition == TransitionEngine.TransitionState.None)
                    _state.Tempo = _transition.CurrentEffectiveTempo;

                // Calculate microseconds per tick from BPM (use interpolated tempo)
                // BPM = quarter notes per minute
                // microseconds per quarter note = 60_000_000 / BPM
                // microseconds per tick = microseconds per quarter note / PPQ
                double usPerTick = 60_000_000.0 / (_transition.CurrentEffectiveTempo * Ppq);

                // Wait for the next tick using Stopwatch + spin-wait
                double targetUs = (currentTick + 1) * usPerTick;
                double elapsedUs = stopwatch.Elapsed.TotalMicroseconds;

                if (elapsedUs < targetUs)
                {
                    double remainingMs = (targetUs - elapsedUs) / 1000.0;
                    if (remainingMs > 2)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(remainingMs - 1), stoppingToken)
                            .ConfigureAwait(false);
                    }

                    // Spin-wait for remaining time
                    while (stopwatch.Elapsed.TotalMicroseconds < targetUs)
                    {
                        if (stoppingToken.IsCancellationRequested) return;
                        Thread.SpinWait(10);
                    }
                }

                // Record tick latency
                double actualUs = stopwatch.Elapsed.TotalMicroseconds;
                double latencyUs = Math.Abs(actualUs - targetUs);
                GenerationMetrics.TickLatency.Record(latencyUs);

                // Record tick for health monitoring
                _healthMonitor.SetExpectedTickDuration(usPerTick / 1000.0);
                _healthMonitor.RecordTick(currentTick, (long)(latencyUs / 1000.0));

                // If tick is >2x late, skip ahead to catch up
                if (latencyUs > usPerTick * 2)
                {
                    long ticksToSkip = (long)(latencyUs / usPerTick);
                    currentTick += ticksToSkip;
                    _logger.LogWarning("Tick {Tick} late by {LatencyUs}µs, skipping {Skip} ticks",
                        currentTick, latencyUs, ticksToSkip);
                }

                currentTick++;

                // Rebuild sequencer if needed
                if (sequencer == null && _state.DrumPattern != null)
                {
                    sequencer = _state.DrumPattern.ToSequencer(Ppq);
                    measureEvents = sequencer.GetMeasureEvents(measureStartTick);
                    measureEventIndex = 0;
                }

                // Handle measure boundary
                if (sequencer != null && currentTick >= measureStartTick + sequencer.TicksPerMeasure)
                {
                    // Turn off last melody note at measure boundary
                    TurnOffMelodyNote(ref lastMelodyNotePlayed);

                    // Record density for the completed measure
                    GenerationMetrics.PatternDensity.Record(eventsInCurrentMeasure);

                    // End previous measure span
                    measureActivity?.Dispose();

                    measureStartTick = currentTick;
                    measureEvents = sequencer.GetMeasureEvents(measureStartTick);
                    measureEventIndex = 0;
                    measureNumber++;
                    eventsInCurrentMeasure = 0;

                    // Trigger variety engine on measure boundary
                    if (_state.Variety != null)
                    {
                        int prevOctave = _state.Octave;
                        int prevVelocityDrift = _state.Variety.VelocityDrift;
                        bool prevRest = _state.Variety.RestInsertionActive;

                        _state.Variety.OnMeasureBoundary(_state, measureNumber);

                        if (_state.Octave != prevOctave)
                        {
                            using var vSpan = _sessionTelemetry.TraceVarietyDecision(
                                "octave_drift", $"octave changed to {_state.Octave}", measureNumber);
                        }
                        if (_state.Variety.VelocityDrift != prevVelocityDrift)
                        {
                            using var vSpan = _sessionTelemetry.TraceVarietyDecision(
                                "velocity_drift", $"drift={_state.Variety.VelocityDrift}", measureNumber);
                        }
                        if (_state.Variety.RestInsertionActive != prevRest)
                        {
                            using var vSpan = _sessionTelemetry.TraceVarietyDecision(
                                "rest_insertion", $"active={_state.Variety.RestInsertionActive}", measureNumber);
                        }
                    }

                    // Periodic health snapshot
                    if (measureNumber - _lastHealthSnapshotMeasure >= HealthSnapshotIntervalMeasures)
                    {
                        var snapshot = _healthMonitor.GetHealth();
                        using var hSpan = _sessionTelemetry.RecordHealthSnapshot(snapshot);
                        _lastHealthSnapshotMeasure = measureNumber;
                    }

                    // Start new measure span as child of session
                    measureActivity = ActivitySource.StartActivity("Measure",
                        ActivityKind.Internal, sessionActivity?.Context ?? default);
                    measureActivity?.SetTag("generation.measure", measureNumber);
                    measureActivity?.SetTag("generation.tempo", _state.Tempo);
                    measureActivity?.SetTag("generation.pattern",
                        _state.DrumPattern?.Name ?? "none");
                }

                // Emit drum events for this tick
                if (measureEvents != null)
                {
                    while (measureEventIndex < measureEvents.Count &&
                           measureEvents[measureEventIndex].Tick <= currentTick)
                    {
                        var evt = measureEvents[measureEventIndex];
                        EmitDrumEvent(evt, measureNumber, measureActivity);
                        eventsInCurrentMeasure++;
                        measureEventIndex++;
                    }
                }

                // Check if instruments are registered for multi-channel routing
                long tickInMeasure = currentTick - measureStartTick;
                bool hasInstruments = _state.Instruments.GetAll().Count > 0;

                if (hasInstruments)
                {
                    // Multi-channel: route via ChannelRouter
                    var plan = _channelRouter.GeneratePlan(tickInMeasure, Ppq, measureEvents, currentTick);
                    foreach (var planned in plan.Notes)
                    {
                        EmitPlannedNote(planned, measureNumber, measureActivity, ref eventsInCurrentMeasure);
                    }
                }

                // Legacy melody: always emit when no instruments or as fallback
                if (!hasInstruments && tickInMeasure % Ppq == 0)
                {
                    int beatNumber = (int)(tickInMeasure / Ppq);
                    EmitMelodyTick(ref lastMelodyNotePlayed, measureNumber,
                        beatNumber, measureActivity, ref eventsInCurrentMeasure);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown
        }
        finally
        {
            // Clean shutdown: turn off any active melody note, then AllNotesOff on all used channels
            TurnOffMelodyNote(ref lastMelodyNotePlayed);
            SendAllNotesOff();
            measureActivity?.Dispose();
            sessionActivity?.Dispose();
            _sessionTelemetry.EndSession();
            stopwatch.Stop();
            _logger.LogInformation("GenerationEngine stopped");
        }
    }

    private void ProcessCommands(
        ref StepSequencer? sequencer,
        ref IReadOnlyList<SequencerEvent>? measureEvents,
        ref long measureStartTick,
        ref int measureEventIndex,
        ref long currentTick,
        ref Activity? sessionActivity,
        ref Activity? measureActivity,
        ref int measureNumber,
        ref int eventsInCurrentMeasure)
    {
        while (_commandChannel.Reader.TryRead(out var command))
        {
            using var activity = ActivitySource.StartActivity("ProcessCommand");

            switch (command)
            {
                case GenerationCommand.SetTempo setTempo:
                    _state.Tempo = setTempo.Bpm;
                    _transition.SetTempo(setTempo.Bpm);
                    _logger.LogInformation("Tempo set to {Bpm} BPM", setTempo.Bpm);
                    break;

                case GenerationCommand.SetScale setScale:
                    _state.Scale = setScale.Scale;
                    _state.MelodyScaleIndex = 0;
                    _state.MelodyDirection = 1;
                    _logger.LogInformation("Scale set to {Scale}", setScale.Scale.Name);
                    break;

                case GenerationCommand.SetTempoSmooth setTempoSmooth:
                {
                    int ticksPerMeasure = 4 * Ppq; // 4/4 time
                    _transition.StartTempoTransition(setTempoSmooth.Bpm, setTempoSmooth.TransitionBars, currentTick, ticksPerMeasure);
                    _logger.LogInformation("Smooth tempo transition to {Bpm} BPM over {Bars} bars", setTempoSmooth.Bpm, setTempoSmooth.TransitionBars);
                    break;
                }

                case GenerationCommand.SetScaleSmooth setScaleSmooth:
                {
                    int ticksPerMeasure = 4 * Ppq; // 4/4 time
                    _transition.StartScaleTransition(_state.Scale, setScaleSmooth.Scale, setScaleSmooth.TransitionBars, currentTick, ticksPerMeasure);
                    _state.Scale = setScaleSmooth.Scale;
                    _state.MelodyScaleIndex = 0;
                    _state.MelodyDirection = 1;
                    _logger.LogInformation("Smooth scale transition to {Scale} over {Bars} bars", setScaleSmooth.Scale.Name, setScaleSmooth.TransitionBars);
                    break;
                }

                case GenerationCommand.SetPattern setPattern:
                    _state.DrumPattern = setPattern.Pattern;
                    sequencer = setPattern.Pattern.ToSequencer(Ppq);
                    measureStartTick = currentTick;
                    measureEvents = sequencer.GetMeasureEvents(measureStartTick);
                    measureEventIndex = 0;
                    _logger.LogInformation("Pattern set to {Pattern}", setPattern.Pattern.Name);
                    break;

                case GenerationCommand.SetOctave setOctave:
                    _state.Octave = setOctave.Octave;
                    _logger.LogInformation("Octave set to {Octave}", setOctave.Octave);
                    break;

                case GenerationCommand.SetMelodicChannel setMelodicChannel:
                    _state.MelodicChannel = setMelodicChannel.Channel;
                    break;

                case GenerationCommand.SetGenerator setGenerator:
                    _state.NoteGenerator = setGenerator.Generator;
                    _logger.LogInformation("Generator set to {Generator}", setGenerator.Generator.Name);
                    break;

                case GenerationCommand.SetDrumChannel setDrumChannel:
                    _state.DrumChannel = setDrumChannel.Channel;
                    break;

                case GenerationCommand.SetVarietyLevel setVariety:
                    if (setVariety.Level is { } level)
                    {
                        _state.Variety ??= new VarietyEngine();
                        _state.Variety.Level = level;
                    }
                    _logger.LogInformation("Variety level set to {Level}", setVariety.Level);
                    break;

                case GenerationCommand.Start:
                    _state.IsPlaying = true;
                    sessionActivity = ActivitySource.StartActivity("Session", ActivityKind.Internal);
                    sessionActivity?.SetTag("generation.tempo", _state.Tempo);
                    sessionActivity?.SetTag("generation.scale", _state.Scale.Name);
                    _sessionTelemetry.StartSession(
                        Guid.NewGuid().ToString("N"),
                        _state.Tempo,
                        _state.Scale.Name);
                    measureNumber = 0;
                    eventsInCurrentMeasure = 0;
                    _logger.LogInformation("Playback started");
                    break;

                case GenerationCommand.Stop:
                    _state.IsPlaying = false;
                    SendAllNotesOff();
                    measureActivity?.Dispose();
                    measureActivity = null;
                    sessionActivity?.Dispose();
                    sessionActivity = null;
                    _sessionTelemetry.EndSession();
                    _logger.LogInformation("Playback stopped");
                    break;

                case GenerationCommand.AllNotesOff:
                    SendAllNotesOff();
                    _logger.LogInformation("All notes off (panic)");
                    break;

                case GenerationCommand.AddInstrument addInstrument:
                    _state.Instruments.Add(addInstrument.Instrument);
                    _logger.LogInformation("Instrument added: {Id} ({Name})",
                        addInstrument.Instrument.Id, addInstrument.Instrument.Name);
                    break;

                case GenerationCommand.RemoveInstrument removeInstrument:
                    _state.Instruments.Remove(removeInstrument.InstrumentId);
                    _logger.LogInformation("Instrument removed: {Id}",
                        removeInstrument.InstrumentId);
                    break;
            }
        }
    }

    private void EmitDrumEvent(SequencerEvent evt, int measureNumber,
        Activity? measureActivity)
    {
        // Probability gate
        if (evt.Probability < 1.0 && Random.Shared.NextDouble() > evt.Probability)
            return;

        using var activity = ActivitySource.StartActivity("DrumEvent",
            ActivityKind.Internal, measureActivity?.Context ?? default);
        activity?.SetTag("generation.measure", measureNumber);
        activity?.SetTag("generation.beat", evt.StepIndex);
        activity?.SetTag("generation.voice", evt.Voice.ToString());
        activity?.SetTag("generation.pattern", _state.DrumPattern?.Name ?? "none");
        activity?.SetTag("generation.tempo", _state.Tempo);

        var drumMap = DrumMap.GeneralMidi;
        if (drumMap.Contains(evt.Voice))
        {
            int midiNote = drumMap.GetMidiNote(evt.Voice);
            try
            {
                var forcedOff = _noteTracker.NoteOn(_state.DrumChannel, midiNote, evt.Velocity);
                foreach (var (ch, n) in forcedOff)
                {
                    _midiOutput.SendNoteOff(ch, n);
                    GenerationMetrics.ActiveVoices.Add(-1);
                }
                _midiOutput.SendNoteOn(_state.DrumChannel, midiNote, evt.Velocity);
                GenerationMetrics.NotesEmitted.Add(1);
                GenerationMetrics.ActiveVoices.Add(1);
                _sessionTelemetry.RecordNote();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send drum event on channel {Channel}, note {Note}",
                    _state.DrumChannel, midiNote);
            }
        }
    }

    private void EmitMelodyTick(ref int lastMelodyNotePlayed, int measureNumber,
        int beatNumber, Activity? measureActivity, ref int eventsInCurrentMeasure)
    {
        using var beatActivity = ActivitySource.StartActivity("Beat",
            ActivityKind.Internal, measureActivity?.Context ?? default);
        beatActivity?.SetTag("generation.measure", measureNumber);
        beatActivity?.SetTag("generation.beat", beatNumber);
        beatActivity?.SetTag("generation.tempo", _state.Tempo);

        using var activity = ActivitySource.StartActivity("MelodyEvent",
            ActivityKind.Internal, beatActivity?.Context ?? default);
        activity?.SetTag("generation.measure", measureNumber);
        activity?.SetTag("generation.beat", beatNumber);
        activity?.SetTag("generation.scale", _state.Scale.Name);
        activity?.SetTag("generation.tempo", _state.Tempo);
        activity?.SetTag("generation.pattern", _state.DrumPattern?.Name ?? "none");
        activity?.SetTag("generation.generator", _state.NoteGenerator.Name);

        // Turn off previous note
        TurnOffMelodyNote(ref lastMelodyNotePlayed);

        // Variety engine: rest insertion for breathing room
        if (_state.Variety?.ShouldInsertRest() == true) return;

        var note = _state.NoteGenerator.NextNote(_state);
        if (note == null) return; // rest

        int midiNote = note.Value;
        int velocity = _state.Variety?.ApplyVelocityDrift(80) ?? 80;
        try
        {
            var forcedOff = _noteTracker.NoteOn(_state.MelodicChannel, midiNote, velocity);
            foreach (var (ch, n) in forcedOff)
            {
                _midiOutput.SendNoteOff(ch, n);
                GenerationMetrics.ActiveVoices.Add(-1);
            }
            _midiOutput.SendNoteOn(_state.MelodicChannel, midiNote, velocity);
            lastMelodyNotePlayed = midiNote;
            GenerationMetrics.NotesEmitted.Add(1);
            GenerationMetrics.ActiveVoices.Add(1);
            _sessionTelemetry.RecordNote();
            eventsInCurrentMeasure++;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send melody event on channel {Channel}, note {Note}",
                _state.MelodicChannel, midiNote);
        }
    }

    private void TurnOffMelodyNote(ref int lastMelodyNotePlayed)
    {
        if (lastMelodyNotePlayed >= 0)
        {
            _midiOutput.SendNoteOff(_state.MelodicChannel, lastMelodyNotePlayed);
            _noteTracker.NoteOff(_state.MelodicChannel, lastMelodyNotePlayed);
            GenerationMetrics.ActiveVoices.Add(-1);
            lastMelodyNotePlayed = -1;
        }
    }

    private void EmitPlannedNote(PlannedNote planned, int measureNumber,
        Activity? measureActivity, ref int eventsInCurrentMeasure)
    {
        using var activity = ActivitySource.StartActivity("RoutedNote",
            ActivityKind.Internal, measureActivity?.Context ?? default);
        activity?.SetTag("generation.measure", measureNumber);
        activity?.SetTag("generation.channel", planned.Channel);
        activity?.SetTag("generation.note", planned.Note);

        try
        {
            // Turn off previous note on this channel
            var lastNote = _channelRouter.GetLastNote(planned.Channel);
            if (lastNote.HasValue)
            {
                _midiOutput.SendNoteOff(planned.Channel, lastNote.Value);
                _noteTracker.NoteOff(planned.Channel, lastNote.Value);
                if (planned.InstrumentId != null)
                    _perInstrumentNoteTracker.NoteOff(planned.InstrumentId, planned.Channel, lastNote.Value);
                GenerationMetrics.ActiveVoices.Add(-1);
            }

            var forcedOff = _noteTracker.NoteOn(planned.Channel, planned.Note, planned.Velocity);
            foreach (var (ch, n) in forcedOff)
            {
                _midiOutput.SendNoteOff(ch, n);
                _channelRouter.ClearChannel(ch);
                GenerationMetrics.ActiveVoices.Add(-1);
            }

            _midiOutput.SendNoteOn(planned.Channel, planned.Note, planned.Velocity);
            _channelRouter.RecordNotePlayed(planned.Channel, planned.Note);
            GenerationMetrics.NotesEmitted.Add(1);
            GenerationMetrics.ActiveVoices.Add(1);
            _sessionTelemetry.RecordNote();
            eventsInCurrentMeasure++;

            // Per-instrument telemetry
            if (planned.InstrumentId != null)
            {
                using var noteSpan = _instrumentTelemetry.TraceNoteSent(
                    planned.InstrumentId, planned.Channel, planned.Note, planned.Velocity);
                _perInstrumentNoteTracker.NoteOn(planned.InstrumentId, planned.Channel, planned.Note);

                var instrument = _state.Instruments.Get(planned.InstrumentId);
                if (instrument != null)
                {
                    int activeCount = _perInstrumentNoteTracker.GetActiveCount(planned.InstrumentId);
                    _instrumentTelemetry.RecordPolyphony(
                        planned.InstrumentId, activeCount, instrument.Capabilities.MaxPolyphony);
                }
            }
        }
        catch (Exception ex)
        {
            if (planned.InstrumentId != null)
                _instrumentTelemetry.RecordError(planned.InstrumentId);
            _logger.LogError(ex, "Failed to send routed note on channel {Channel}, note {Note}",
                planned.Channel, planned.Note);
        }
    }

    private void SendAllNotesOff()
    {
        // Send note-offs for all tracked active notes
        foreach (var (ch, n) in _noteTracker.AllNotesOff())
        {
            _midiOutput.SendNoteOff(ch, n);
        }

        // Also send channel-wide AllNotesOff as safety net
        var channels = new HashSet<int> { _state.MelodicChannel, _state.DrumChannel };

        // Include channels used by registered instruments
        foreach (var instrument in _state.Instruments.GetAll())
            channels.Add(instrument.MidiChannel);

        // Include channels tracked by the router
        foreach (var ch in _channelRouter.GetActiveChannels())
            channels.Add(ch);

        foreach (var ch in channels)
        {
            _midiOutput.AllNotesOff(ch);
        }
    }
}
