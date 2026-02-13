namespace SqncR.Core.Generation;

/// <summary>
/// Tracks active MIDI notes for cleanup and polyphony limiting.
/// Ensures no stuck notes during long-running sessions.
/// </summary>
public sealed class NoteTracker
{
    private readonly Dictionary<(int Channel, int Note), long> _activeNotes = new();
    private long _tickCounter;

    /// <summary>Maximum concurrent active notes. Oldest note is forced off when exceeded.</summary>
    public int MaxActiveNotes { get; set; } = 32;

    /// <summary>Records a note-on event. Returns forced note-offs if max active notes exceeded.</summary>
    public IReadOnlyList<(int Channel, int Note)> NoteOn(int channel, int note, int velocity)
    {
        var forced = new List<(int Channel, int Note)>();
        var key = (channel, note);

        // If this exact note is already active, treat as re-trigger (remove old entry)
        _activeNotes.Remove(key);

        // Enforce polyphony limit before adding new note
        while (_activeNotes.Count >= MaxActiveNotes)
        {
            var oldest = _activeNotes.MinBy(kvp => kvp.Value);
            forced.Add(oldest.Key);
            _activeNotes.Remove(oldest.Key);
        }

        _activeNotes[key] = _tickCounter++;
        return forced;
    }

    /// <summary>Records a note-off event.</summary>
    public void NoteOff(int channel, int note)
    {
        _activeNotes.Remove((channel, note));
    }

    /// <summary>Returns all currently active notes.</summary>
    public IReadOnlyList<(int Channel, int Note)> GetActiveNotes()
    {
        return _activeNotes.Keys.ToList();
    }

    /// <summary>Returns all active notes and clears tracking. Used for panic/shutdown.</summary>
    public IReadOnlyList<(int Channel, int Note)> AllNotesOff()
    {
        var notes = _activeNotes.Keys.ToList();
        _activeNotes.Clear();
        return notes;
    }

    /// <summary>Number of currently active notes.</summary>
    public int ActiveNoteCount => _activeNotes.Count;
}
