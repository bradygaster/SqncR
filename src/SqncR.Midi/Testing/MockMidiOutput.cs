using System.Collections.Concurrent;
using System.Diagnostics;

namespace SqncR.Midi.Testing;

/// <summary>
/// Thread-safe mock MIDI output that captures all events for test assertions.
/// Uses Stopwatch for relative timing from first event.
/// </summary>
public class MockMidiOutput : IMidiOutput
{
    private readonly ConcurrentQueue<CapturedMidiEvent> _events = new();
    private readonly Stopwatch _stopwatch = new();
    private int _started; // 0 = not started, 1 = started

    public string? CurrentDeviceName { get; set; } = "MockDevice";

    public IReadOnlyList<CapturedMidiEvent> Events => _events.ToArray();

    public void SendNoteOn(int channel, int note, int velocity)
    {
        EnsureTimerStarted();
        _events.Enqueue(new CapturedMidiEvent(
            MidiEventType.NoteOn, channel, note, velocity, _stopwatch.Elapsed));
    }

    public void SendNoteOff(int channel, int note)
    {
        EnsureTimerStarted();
        _events.Enqueue(new CapturedMidiEvent(
            MidiEventType.NoteOff, channel, note, 0, _stopwatch.Elapsed));
    }

    public void AllNotesOff(int channel)
    {
        EnsureTimerStarted();
        _events.Enqueue(new CapturedMidiEvent(
            MidiEventType.AllNotesOff, channel, 0, 0, _stopwatch.Elapsed));
    }

    /// <summary>
    /// Resets all captured events and the internal timer.
    /// </summary>
    public void Reset()
    {
        while (_events.TryDequeue(out _)) { }
        _stopwatch.Reset();
        Interlocked.Exchange(ref _started, 0);
    }

    public void Dispose()
    {
        _stopwatch.Stop();
    }

    private void EnsureTimerStarted()
    {
        if (Interlocked.CompareExchange(ref _started, 1, 0) == 0)
        {
            _stopwatch.Start();
        }
    }
}
