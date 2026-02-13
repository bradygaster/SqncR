using System.Collections.Concurrent;

namespace SqncR.Core.Instruments;

/// <summary>
/// Per-instrument note tracking. Partitions note tracking by instrument ID
/// for independent polyphony monitoring per device.
/// </summary>
public sealed class PerInstrumentNoteTracker
{
    private readonly ConcurrentDictionary<string, HashSet<(int Channel, int Note)>> _activeNotes = new();

    /// <summary>Records a note-on for a specific instrument.</summary>
    public void NoteOn(string instrumentId, int channel, int note)
    {
        var set = _activeNotes.GetOrAdd(instrumentId, _ => new HashSet<(int, int)>());
        lock (set)
        {
            set.Add((channel, note));
        }
    }

    /// <summary>Records a note-off for a specific instrument.</summary>
    public void NoteOff(string instrumentId, int channel, int note)
    {
        if (_activeNotes.TryGetValue(instrumentId, out var set))
        {
            lock (set)
            {
                set.Remove((channel, note));
            }
        }
    }

    /// <summary>Returns the count of active notes for a specific instrument.</summary>
    public int GetActiveCount(string instrumentId)
    {
        if (_activeNotes.TryGetValue(instrumentId, out var set))
        {
            lock (set)
            {
                return set.Count;
            }
        }
        return 0;
    }

    /// <summary>Clears all active notes for a specific instrument. Returns the cleared notes.</summary>
    public IReadOnlyList<(int Channel, int Note)> AllNotesOff(string instrumentId)
    {
        if (_activeNotes.TryGetValue(instrumentId, out var set))
        {
            lock (set)
            {
                var notes = set.ToList();
                set.Clear();
                return notes;
            }
        }
        return [];
    }
}
