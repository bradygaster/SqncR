namespace SqncR.Midi.Testing;

public enum MidiEventType
{
    NoteOn,
    NoteOff,
    AllNotesOff
}

public record CapturedMidiEvent(
    MidiEventType Type,
    int Channel,
    int Note,
    int Velocity,
    TimeSpan Timestamp
);
