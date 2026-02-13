namespace SqncR.Midi.Testing;

/// <summary>
/// Abstraction over MIDI output for testability.
/// MidiService implements this for real hardware; MockMidiOutput for tests.
/// </summary>
public interface IMidiOutput : IDisposable
{
    void SendNoteOn(int channel, int note, int velocity);
    void SendNoteOff(int channel, int note);
    void AllNotesOff(int channel);
    string? CurrentDeviceName { get; }
}
