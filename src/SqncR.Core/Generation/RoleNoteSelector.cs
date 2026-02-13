using SqncR.Core.Instruments;
using SqncR.Core.Rhythm;
using SqncR.Theory;

namespace SqncR.Core.Generation;

/// <summary>
/// Output from role-aware note selection — a set of notes appropriate for the role.
/// </summary>
public record RoleOutput(IReadOnlyList<RoleNote> Notes);

/// <summary>
/// A single note produced by role-aware selection.
/// </summary>
public record RoleNote(int MidiNote, int Velocity, int DurationTicks);

/// <summary>
/// Selects notes appropriate for each instrument role (Bass, Pad, Lead, Drums, Melody).
/// Each role gets musically appropriate pitch range, rhythm, velocity, and voicing.
/// </summary>
public sealed class RoleNoteSelector
{
    private readonly Random _random;
    private readonly INoteGenerator? _leadGenerator;
    private readonly StepSequencer? _drumSequencer;
    private readonly DrumMap _drumMap;

    private int _bassLastRoot = -1;
    private int _padLastChangeBeat = -4;

    // Cached pad chord so it doesn't change every tick
    private IReadOnlyList<RoleNote>? _padChordCache;

    public RoleNoteSelector(
        INoteGenerator? leadGenerator = null,
        StepSequencer? drumSequencer = null,
        DrumMap? drumMap = null,
        Random? random = null)
    {
        _leadGenerator = leadGenerator;
        _drumSequencer = drumSequencer;
        _drumMap = drumMap ?? DrumMap.GeneralMidi;
        _random = random ?? Random.Shared;
    }

    /// <summary>
    /// Get notes for a specific role at the current tick position.
    /// </summary>
    public RoleOutput SelectNotes(InstrumentRole role, Scale scale, int rootNote, int tick, int ticksPerBeat)
    {
        return role switch
        {
            InstrumentRole.Bass => SelectBass(scale, rootNote, tick, ticksPerBeat),
            InstrumentRole.Pad => SelectPad(scale, rootNote, tick, ticksPerBeat),
            InstrumentRole.Lead => SelectLead(scale, rootNote, tick, ticksPerBeat),
            InstrumentRole.Melody => SelectLead(scale, rootNote, tick, ticksPerBeat),
            InstrumentRole.Drums => SelectDrums(tick, ticksPerBeat),
            _ => new RoleOutput([])
        };
    }

    private RoleOutput SelectBass(Scale scale, int rootNote, int tick, int ticksPerBeat)
    {
        int beatInMeasure = (tick / ticksPerBeat) % 4;
        int tickInBeat = tick % ticksPerBeat;

        // Bass plays on beat boundaries (quarter or half notes)
        if (tickInBeat != 0)
            return new RoleOutput([]);

        // Half note: only play on beats 0 and 2 sometimes
        bool isHalfNote = _random.NextDouble() < 0.3;
        if (isHalfNote && beatInMeasure % 2 != 0)
            return new RoleOutput([]);

        int rootPitchClass = rootNote % 12;
        int fifthInterval = GetFifthInterval(scale);

        // Choose between root and fifth, with occasional walkup
        int pitchClass;
        double roll = _random.NextDouble();
        if (roll < 0.6)
        {
            // Play root
            pitchClass = rootPitchClass;
        }
        else if (roll < 0.85)
        {
            // Play fifth
            pitchClass = (rootPitchClass + fifthInterval) % 12;
        }
        else
        {
            // Walking bass: pick a scale tone between last root and current
            pitchClass = GetWalkingTone(scale, rootPitchClass);
        }

        // Place in octave 2-3 (MIDI 36-59)
        int octave = _random.NextDouble() < 0.6 ? 2 : 3;
        int midiNote = ((octave + 1) * 12) + pitchClass;
        midiNote = Math.Clamp(midiNote, 36, 59);

        // Snap to scale (except it's already a scale tone in most cases)
        midiNote = scale.ContainsNote(midiNote) ? midiNote : scale.GetNearestScaleNote(midiNote);
        midiNote = Math.Clamp(midiNote, 36, 59);

        int velocity = 80 + _random.Next(21); // 80-100
        int duration = isHalfNote ? ticksPerBeat * 2 : ticksPerBeat;

        _bassLastRoot = pitchClass;

        return new RoleOutput([new RoleNote(midiNote, velocity, duration)]);
    }

    private RoleOutput SelectPad(Scale scale, int rootNote, int tick, int ticksPerBeat)
    {
        int currentBeat = tick / ticksPerBeat;
        int tickInBeat = tick % ticksPerBeat;

        // Pads change every 4-8 beats
        int changeInterval = 4 + _random.Next(5); // 4-8
        bool shouldChange = (currentBeat - _padLastChangeBeat) >= changeInterval;

        // Only emit on beat boundaries
        if (tickInBeat != 0)
            return new RoleOutput([]);

        // Return cached chord if not time to change
        if (!shouldChange && _padChordCache != null)
            return new RoleOutput(_padChordCache);

        _padLastChangeBeat = currentBeat;

        // Build 3-note chord voicing (root + 3rd + 5th) in octave 3-5
        int rootPitchClass = rootNote % 12;
        var scaleIntervals = scale.Intervals;

        // Root in octave 3-4
        int baseOctave = 3 + _random.Next(2); // 3 or 4
        int chordRoot = ((baseOctave + 1) * 12) + rootPitchClass;

        // 3rd: scale degree 2 (index 2 in intervals)
        int thirdSemitones = scaleIntervals.Count > 2 ? scaleIntervals[2] : 3;
        int chordThird = chordRoot + thirdSemitones;

        // 5th: scale degree 4 (index 4 in intervals)
        int fifthSemitones = scaleIntervals.Count > 4 ? scaleIntervals[4] : 7;
        int chordFifth = chordRoot + fifthSemitones;

        // Clamp to pad range (48-83)
        chordRoot = Math.Clamp(chordRoot, 48, 83);
        chordThird = Math.Clamp(chordThird, 48, 83);
        chordFifth = Math.Clamp(chordFifth, 48, 83);

        int velocity = 50 + _random.Next(21); // 50-70

        // Whole note or longer duration
        int duration = ticksPerBeat * 4;

        var notes = new List<RoleNote>
        {
            new(chordRoot, velocity, duration),
            new(chordThird, velocity, duration),
            new(chordFifth, velocity, duration)
        };

        _padChordCache = notes;
        return new RoleOutput(notes);
    }

    private RoleOutput SelectLead(Scale scale, int rootNote, int tick, int ticksPerBeat)
    {
        int tickInBeat = tick % ticksPerBeat;
        int halfBeat = ticksPerBeat / 2;

        // Lead plays 8th notes to quarter notes — trigger on beat or half-beat
        if (tickInBeat != 0 && tickInBeat != halfBeat)
            return new RoleOutput([]);

        int? note;
        if (_leadGenerator != null)
        {
            // Use the pluggable generator via a temporary state
            var tempState = new GenerationState
            {
                Scale = scale,
                Octave = 5 // Default lead octave
            };
            note = _leadGenerator.NextNote(tempState);
        }
        else
        {
            // Fallback: pick a random scale note in octave 4-6
            int octave = 4 + _random.Next(3);
            var notes = scale.GetNotesInOctave(octave);
            if (notes.Count == 0)
                return new RoleOutput([]);
            note = notes[_random.Next(notes.Count)];
        }

        if (note == null)
            return new RoleOutput([]);

        // Constrain to lead range (MIDI 60-95)
        int midiNote = Math.Clamp(note.Value, 60, 95);
        if (!scale.ContainsNote(midiNote))
            midiNote = scale.GetNearestScaleNote(midiNote);
        midiNote = Math.Clamp(midiNote, 60, 95);

        int velocity = 70 + _random.Next(21); // 70-90

        // Duration: 8th note or quarter note
        bool isEighth = tickInBeat == halfBeat || _random.NextDouble() < 0.5;
        int duration = isEighth ? halfBeat : ticksPerBeat;

        return new RoleOutput([new RoleNote(midiNote, velocity, duration)]);
    }

    private RoleOutput SelectDrums(int tick, int ticksPerBeat)
    {
        if (_drumSequencer == null || _drumSequencer.StepsPerMeasure == 0)
            return new RoleOutput([]);

        int ticksPerMeasure = _drumSequencer.TicksPerMeasure;
        if (ticksPerMeasure == 0)
            return new RoleOutput([]);

        long measureStartTick = (tick / ticksPerMeasure) * ticksPerMeasure;
        int tickInMeasure = tick - (int)measureStartTick;

        // Find events at the current tick
        var measureEvents = _drumSequencer.GetMeasureEvents(measureStartTick);
        var notes = new List<RoleNote>();

        foreach (var evt in measureEvents)
        {
            if (evt.Tick == tick)
            {
                int midiNote = _drumMap.GetMidiNote(evt.Voice);
                // Drum note duration is short (1/4 of a beat)
                int duration = ticksPerBeat / 4;
                notes.Add(new RoleNote(midiNote, evt.Velocity, duration));
            }
        }

        return new RoleOutput(notes);
    }

    private static int GetFifthInterval(Scale scale)
    {
        // Find the perfect fifth (7 semitones) or nearest in scale
        if (scale.Intervals.Count > 4)
            return scale.Intervals[4]; // 5th scale degree
        return 7; // Default to perfect fifth
    }

    private int GetWalkingTone(Scale scale, int rootPitchClass)
    {
        // Pick a passing tone from the scale
        var intervals = scale.Intervals;
        if (intervals.Count <= 1)
            return rootPitchClass;

        // Choose a random non-root scale degree as a passing tone
        int idx = 1 + _random.Next(intervals.Count - 1);
        return (rootPitchClass + intervals[idx]) % 12;
    }
}
