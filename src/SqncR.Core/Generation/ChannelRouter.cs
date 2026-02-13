using SqncR.Core.Instruments;
using SqncR.Core.Rhythm;

namespace SqncR.Core.Generation;

/// <summary>
/// Routes musical content to MIDI channels based on instrument roles.
/// Each role gets role-appropriate note generation (register, rhythm, articulation).
/// </summary>
public sealed class ChannelRouter
{
    private readonly GenerationState _state;
    private readonly Random _random;

    // Track last note per channel for note-off
    private readonly Dictionary<int, int> _lastNotePerChannel = new();

    public ChannelRouter(GenerationState state, Random? random = null)
    {
        _state = state;
        _random = random ?? Random.Shared;
    }

    /// <summary>
    /// Returns all registered instruments grouped by role.
    /// </summary>
    public IReadOnlyDictionary<InstrumentRole, IReadOnlyList<Instrument>> GetActiveInstruments()
    {
        var all = _state.Instruments.GetAll();
        var grouped = new Dictionary<InstrumentRole, IReadOnlyList<Instrument>>();

        foreach (InstrumentRole role in Enum.GetValues<InstrumentRole>())
        {
            var instruments = all.Where(i => i.Role == role).ToList();
            if (instruments.Count > 0)
                grouped[role] = instruments;
        }

        return grouped;
    }

    /// <summary>
    /// Generates a ChannelPlan for the current tick. Called on beat boundaries.
    /// </summary>
    /// <param name="tickInMeasure">Current tick offset within the measure.</param>
    /// <param name="ppq">Ticks per quarter note.</param>
    /// <param name="measureEvents">Current drum sequencer events (if any).</param>
    /// <param name="currentTick">Absolute tick position.</param>
    public ChannelPlan GeneratePlan(long tickInMeasure, int ppq,
        IReadOnlyList<SequencerEvent>? measureEvents, long currentTick)
    {
        var instruments = GetActiveInstruments();
        if (instruments.Count == 0)
            return new ChannelPlan([]);

        var notes = new List<PlannedNote>();

        foreach (var (role, roleInstruments) in instruments)
        {
            foreach (var instrument in roleInstruments)
            {
                var planned = role switch
                {
                    InstrumentRole.Bass => GenerateBassNote(instrument, tickInMeasure, ppq),
                    InstrumentRole.Pad => GeneratePadNote(instrument, tickInMeasure, ppq),
                    InstrumentRole.Lead or InstrumentRole.Melody => GenerateLeadNote(instrument, tickInMeasure, ppq),
                    InstrumentRole.Drums => null, // Drums handled separately via StepSequencer
                    _ => null
                };

                if (planned != null)
                    notes.Add(planned);
            }
        }

        return new ChannelPlan(notes);
    }

    /// <summary>
    /// Bass: root notes, low register (octave 2-3), quarter/half note durations.
    /// </summary>
    private PlannedNote? GenerateBassNote(Instrument instrument, long tickInMeasure, int ppq)
    {
        // Bass plays on quarter note boundaries only
        if (tickInMeasure % ppq != 0)
            return null;

        // Play every other beat (half notes) or every beat (quarter notes)
        bool halfNoteRhythm = _random.NextDouble() < 0.4;
        if (halfNoteRhythm && (tickInMeasure / ppq) % 2 != 0)
            return null;

        // Low register: octave 2-3
        int octave = _random.NextDouble() < 0.6 ? 2 : 3;
        var scaleNotes = _state.Scale.GetNotesInOctave(octave);
        if (scaleNotes.Count == 0)
            return null;

        // Favor root note strongly
        int noteIndex = _random.NextDouble() < 0.6 ? 0 : _random.Next(Math.Min(3, scaleNotes.Count));
        int note = scaleNotes[noteIndex];

        // Clamp to instrument capabilities
        note = Math.Clamp(note, instrument.Capabilities.MinNote, instrument.Capabilities.MaxNote);

        int durationTicks = halfNoteRhythm ? ppq * 2 : ppq;
        int velocity = 70 + _random.Next(20); // 70-89

        return new PlannedNote(instrument.MidiChannel, note, velocity, durationTicks, instrument.Id);
    }

    /// <summary>
    /// Pad: chord tones, mid register (octave 3-5), sustained notes.
    /// </summary>
    private PlannedNote? GeneratePadNote(Instrument instrument, long tickInMeasure, int ppq)
    {
        // Pads change every whole note (4 beats) or half note (2 beats)
        int changeInterval = ppq * 2; // half note changes
        if (tickInMeasure % changeInterval != 0)
            return null;

        // Mid register: octave 3-5
        int octave = 3 + _random.Next(3); // 3, 4, or 5
        var scaleNotes = _state.Scale.GetNotesInOctave(octave);
        if (scaleNotes.Count == 0)
            return null;

        // Pick chord tones (root, 3rd, 5th when available)
        int[] chordDegrees = scaleNotes.Count >= 5
            ? [0, 2, 4]
            : scaleNotes.Count >= 3 ? [0, 2] : [0];

        int degree = chordDegrees[_random.Next(chordDegrees.Length)];
        int note = scaleNotes[degree];

        // Clamp to instrument capabilities
        note = Math.Clamp(note, instrument.Capabilities.MinNote, instrument.Capabilities.MaxNote);

        int durationTicks = changeInterval;
        int velocity = 50 + _random.Next(20); // 50-69, softer for pads

        return new PlannedNote(instrument.MidiChannel, note, velocity, durationTicks, instrument.Id);
    }

    /// <summary>
    /// Lead/Melody: uses the current NoteGenerator, high register (octave 4-6).
    /// </summary>
    private PlannedNote? GenerateLeadNote(Instrument instrument, long tickInMeasure, int ppq)
    {
        // Lead plays on quarter note boundaries
        if (tickInMeasure % ppq != 0)
            return null;

        // Use the state's NoteGenerator but override octave to high register
        int originalOctave = _state.Octave;
        try
        {
            // High register: octave 4-6
            int octave = 4 + _random.Next(3); // 4, 5, or 6
            _state.Octave = octave;

            int? generatedNote = _state.NoteGenerator.NextNote(_state);
            if (generatedNote == null)
                return null;

            int note = generatedNote.Value;

            // Clamp to instrument capabilities
            note = Math.Clamp(note, instrument.Capabilities.MinNote, instrument.Capabilities.MaxNote);

            int velocity = 75 + _random.Next(25); // 75-99
            int durationTicks = ppq; // quarter note

            return new PlannedNote(instrument.MidiChannel, note, velocity, durationTicks, instrument.Id);
        }
        finally
        {
            _state.Octave = originalOctave;
        }
    }

    /// <summary>
    /// Returns the last played note on a given channel (for note-off tracking).
    /// </summary>
    public int? GetLastNote(int channel) =>
        _lastNotePerChannel.TryGetValue(channel, out var note) ? note : null;

    /// <summary>
    /// Records that a note was played on a channel (for note-off tracking).
    /// </summary>
    public void RecordNotePlayed(int channel, int note)
    {
        _lastNotePerChannel[channel] = note;
    }

    /// <summary>
    /// Clears tracking for a channel (after note-off).
    /// </summary>
    public void ClearChannel(int channel)
    {
        _lastNotePerChannel.Remove(channel);
    }

    /// <summary>
    /// Returns all channels currently tracking a last-played note.
    /// </summary>
    public IReadOnlyList<int> GetActiveChannels() =>
        _lastNotePerChannel.Keys.ToList();
}
