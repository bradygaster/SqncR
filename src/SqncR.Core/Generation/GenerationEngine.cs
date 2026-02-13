using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

    /// <summary>Writer for enqueuing commands from external callers (MCP tools).</summary>
    public ChannelWriter<GenerationCommand> Commands => _commandChannel.Writer;

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

                // Calculate microseconds per tick from BPM
                // BPM = quarter notes per minute
                // microseconds per quarter note = 60_000_000 / BPM
                // microseconds per tick = microseconds per quarter note / PPQ
                double usPerTick = 60_000_000.0 / (_state.Tempo * Ppq);

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

                // Emit melody on beat boundaries (every quarter note = every PPQ ticks)
                long tickInMeasure = currentTick - measureStartTick;
                if (tickInMeasure % Ppq == 0)
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
                    _logger.LogInformation("Tempo set to {Bpm} BPM", setTempo.Bpm);
                    break;

                case GenerationCommand.SetScale setScale:
                    _state.Scale = setScale.Scale;
                    _state.MelodyScaleIndex = 0;
                    _state.MelodyDirection = 1;
                    _logger.LogInformation("Scale set to {Scale}", setScale.Scale.Name);
                    break;

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

                case GenerationCommand.Start:
                    _state.IsPlaying = true;
                    sessionActivity = ActivitySource.StartActivity("Session", ActivityKind.Internal);
                    sessionActivity?.SetTag("generation.tempo", _state.Tempo);
                    sessionActivity?.SetTag("generation.scale", _state.Scale.Name);
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
                    _logger.LogInformation("Playback stopped");
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
            _midiOutput.SendNoteOn(_state.DrumChannel, midiNote, evt.Velocity);
            GenerationMetrics.NotesEmitted.Add(1);
            GenerationMetrics.ActiveVoices.Add(1);
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

        var note = _state.NoteGenerator.NextNote(_state);
        if (note == null) return; // rest

        int midiNote = note.Value;
        _midiOutput.SendNoteOn(_state.MelodicChannel, midiNote, 80);
        lastMelodyNotePlayed = midiNote;
        GenerationMetrics.NotesEmitted.Add(1);
        GenerationMetrics.ActiveVoices.Add(1);
        eventsInCurrentMeasure++;
    }

    private void TurnOffMelodyNote(ref int lastMelodyNotePlayed)
    {
        if (lastMelodyNotePlayed >= 0)
        {
            _midiOutput.SendNoteOff(_state.MelodicChannel, lastMelodyNotePlayed);
            GenerationMetrics.ActiveVoices.Add(-1);
            lastMelodyNotePlayed = -1;
        }
    }

    private void SendAllNotesOff()
    {
        var channels = new HashSet<int> { _state.MelodicChannel, _state.DrumChannel };
        foreach (var ch in channels)
        {
            _midiOutput.AllNotesOff(ch);
        }
    }
}
