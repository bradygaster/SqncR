using System.Diagnostics;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using SqncR.Midi.Testing;

namespace SqncR.Midi;

public record MidiDeviceInfo(int Index, string Name);

public class MidiService : IMidiOutput
{
    internal static readonly ActivitySource ActivitySource = new("SqncR.Midi");

    private OutputDevice? _outputDevice;

    public IReadOnlyList<MidiDeviceInfo> ListOutputDevices()
    {
        return OutputDevice.GetAll()
            .Select((d, i) => new MidiDeviceInfo(i, d.Name))
            .ToList();
    }

    public void OpenDevice(int deviceIndex)
    {
        _outputDevice?.Dispose();

        var devices = OutputDevice.GetAll().ToList();
        if (deviceIndex < 0 || deviceIndex >= devices.Count)
            throw new ArgumentException($"Device index {deviceIndex} not found. Use list-devices to see available devices.");

        _outputDevice = devices[deviceIndex];
    }

    public void OpenDevice(string deviceName)
    {
        _outputDevice?.Dispose();

        var devices = OutputDevice.GetAll().ToList();
        var device = devices.FirstOrDefault(d =>
            d.Name.Contains(deviceName, StringComparison.OrdinalIgnoreCase));

        if (device == null)
            throw new ArgumentException($"Device '{deviceName}' not found. Use list-devices to see available devices.");

        _outputDevice = device;
    }

    public string? CurrentDeviceName => _outputDevice?.Name;

    public void SendNoteOn(int channel, int note, int velocity)
    {
        if (_outputDevice == null)
            throw new InvalidOperationException("No MIDI device open. Call OpenDevice first.");

        using var activity = ActivitySource.StartActivity("midi.note_on");
        activity?.SetTag("midi.channel", channel);
        activity?.SetTag("midi.note", note);
        activity?.SetTag("midi.velocity", velocity);

        _outputDevice.SendEvent(new NoteOnEvent(
            (SevenBitNumber)note,
            (SevenBitNumber)velocity)
        {
            Channel = (FourBitNumber)(channel - 1) // MIDI channels are 0-indexed internally
        });
    }

    public void SendNoteOff(int channel, int note)
    {
        if (_outputDevice == null)
            throw new InvalidOperationException("No MIDI device open. Call OpenDevice first.");

        using var activity = ActivitySource.StartActivity("midi.note_off");
        activity?.SetTag("midi.channel", channel);
        activity?.SetTag("midi.note", note);

        _outputDevice.SendEvent(new NoteOffEvent(
            (SevenBitNumber)note,
            (SevenBitNumber)0)
        {
            Channel = (FourBitNumber)(channel - 1)
        });
    }

    public void AllNotesOff(int channel)
    {
        if (_outputDevice == null) return;

        using var activity = ActivitySource.StartActivity("midi.all_notes_off");
        activity?.SetTag("midi.channel", channel);

        // Send note off for all possible notes
        for (int note = 0; note < 128; note++)
        {
            _outputDevice.SendEvent(new NoteOffEvent(
                (SevenBitNumber)note,
                (SevenBitNumber)0)
            {
                Channel = (FourBitNumber)(channel - 1)
            });
        }
    }

    public void Dispose()
    {
        _outputDevice?.Dispose();
        _outputDevice = null;
    }
}
