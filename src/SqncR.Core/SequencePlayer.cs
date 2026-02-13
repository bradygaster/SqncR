using System.Diagnostics;
using SqncR.Core.Models;
using SqncR.Midi;

namespace SqncR.Core;

public class SequencePlayer
{
    internal static readonly ActivitySource ActivitySource = new("SqncR.Playback");

    private readonly MidiService _midi;

    public SequencePlayer(MidiService midi)
    {
        _midi = midi;
    }

    public async Task PlayAsync(Sequence sequence, CancellationToken cancellationToken = default)
    {
        using var playActivity = ActivitySource.StartActivity("playback.play_sequence");
        playActivity?.SetTag("sequence.title", sequence.Meta.Title);
        playActivity?.SetTag("sequence.tempo", sequence.Meta.Tempo);
        playActivity?.SetTag("sequence.key", sequence.Meta.Key);

        // Calculate timing
        var tpq = sequence.Meta.Tpq > 0 ? sequence.Meta.Tpq : 480;
        var tempo = sequence.Meta.Tempo > 0 ? sequence.Meta.Tempo : 120;
        var msPerTick = 60000.0 / tempo / tpq;

        Console.WriteLine($"Playing: {sequence.Meta.Title}");
        Console.WriteLine($"Tempo: {tempo} BPM, Key: {sequence.Meta.Key}");
        Console.WriteLine($"Device: {_midi.CurrentDeviceName}");
        Console.WriteLine("Press Ctrl+C to stop");
        Console.WriteLine();

        // Build timeline of all events
        var timeline = BuildTimeline(sequence);

        if (timeline.Count == 0)
        {
            Console.WriteLine("No events to play.");
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var random = new Random();

        foreach (var evt in timeline.OrderBy(e => e.AbsoluteTime))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Wait until the right time
            var targetMs = evt.AbsoluteTime * msPerTick;
            var currentMs = stopwatch.Elapsed.TotalMilliseconds;
            var waitMs = targetMs - currentMs;

            if (waitMs > 0)
            {
                await Task.Delay((int)waitMs, cancellationToken);
            }

            // Resolve velocity (could be a number or a range)
            var velocity = ResolveVelocity(evt.Velocity, random);

            // Send the event
            if (evt.IsNoteOn)
            {
                using var noteActivity = ActivitySource.StartActivity("playback.note_on");
                noteActivity?.SetTag("note.channel", evt.Channel);
                noteActivity?.SetTag("note.midi", evt.MidiNote);
                noteActivity?.SetTag("note.name", evt.NoteName);
                noteActivity?.SetTag("note.velocity", ResolveVelocity(evt.Velocity, random));

                _midi.SendNoteOn(evt.Channel, evt.MidiNote, velocity);
                Console.WriteLine($"  ON:  Ch{evt.Channel} {evt.NoteName,-4} vel={velocity}");
            }
            else
            {
                using var noteActivity = ActivitySource.StartActivity("playback.note_off");
                noteActivity?.SetTag("note.channel", evt.Channel);
                noteActivity?.SetTag("note.midi", evt.MidiNote);
                noteActivity?.SetTag("note.name", evt.NoteName);

                _midi.SendNoteOff(evt.Channel, evt.MidiNote);
            }
        }

        Console.WriteLine();
        Console.WriteLine("Playback complete.");
    }

    private List<TimelineEvent> BuildTimeline(Sequence sequence)
    {
        var events = new List<TimelineEvent>();

        foreach (var arrange in sequence.Arrange)
        {
            if (!sequence.Sections.TryGetValue(arrange.Section, out var section))
            {
                Console.WriteLine($"Warning: Section '{arrange.Section}' not found.");
                continue;
            }

            foreach (var track in section.Tracks)
            {
                foreach (var seqEntry in track.Sequence)
                {
                    if (!sequence.Patterns.TryGetValue(seqEntry.Pattern, out var pattern))
                    {
                        Console.WriteLine($"Warning: Pattern '{seqEntry.Pattern}' not found.");
                        continue;
                    }

                    var repeatCount = seqEntry.Repeat > 0 ? seqEntry.Repeat : 1;

                    for (int rep = 0; rep < repeatCount; rep++)
                    {
                        var baseOffset = arrange.At + seqEntry.At + (rep * pattern.Length);

                        foreach (var noteEvt in pattern.Events.Where(e => e.Type == "note"))
                        {
                            try
                            {
                                var midiNote = NoteParser.Parse(noteEvt.Note) + seqEntry.Transpose;

                                // Clamp to valid MIDI range
                                midiNote = Math.Clamp(midiNote, 0, 127);

                                var noteOnTime = baseOffset + noteEvt.T;
                                var noteOffTime = noteOnTime + noteEvt.Dur;

                                // Note On
                                events.Add(new TimelineEvent
                                {
                                    AbsoluteTime = noteOnTime,
                                    Channel = track.Ch,
                                    MidiNote = midiNote,
                                    NoteName = NoteParser.ToNoteName(midiNote),
                                    Velocity = noteEvt.Vel,
                                    IsNoteOn = true
                                });

                                // Note Off
                                events.Add(new TimelineEvent
                                {
                                    AbsoluteTime = noteOffTime,
                                    Channel = track.Ch,
                                    MidiNote = midiNote,
                                    NoteName = NoteParser.ToNoteName(midiNote),
                                    Velocity = 0,
                                    IsNoteOn = false
                                });
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Warning: Skipping note '{noteEvt.Note}': {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        return events;
    }

    private int ResolveVelocity(object? velocitySpec, Random random)
    {
        if (velocitySpec == null)
            return 80;

        // If it's just a number
        if (velocitySpec is int intVel)
            return Math.Clamp(intVel, 0, 127);

        if (velocitySpec is long longVel)
            return Math.Clamp((int)longVel, 0, 127);

        if (velocitySpec is double doubleVel)
            return Math.Clamp((int)doubleVel, 0, 127);

        // If it's a string representation of a number
        if (velocitySpec is string strVel && int.TryParse(strVel, out var parsedVel))
            return Math.Clamp(parsedVel, 0, 127);

        // If it's a dictionary with "range" key (randomization)
        if (velocitySpec is Dictionary<object, object> dict)
        {
            if (dict.TryGetValue("range", out var rangeObj) && rangeObj is List<object> rangeList && rangeList.Count >= 2)
            {
                var min = Convert.ToInt32(rangeList[0]);
                var max = Convert.ToInt32(rangeList[1]);
                return random.Next(min, max + 1);
            }
        }

        return 80; // Default
    }
}

internal class TimelineEvent
{
    public int AbsoluteTime { get; init; }
    public int Channel { get; init; }
    public int MidiNote { get; init; }
    public string NoteName { get; init; } = "";
    public object? Velocity { get; init; }
    public bool IsNoteOn { get; init; }
}
