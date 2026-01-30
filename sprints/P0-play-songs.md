# P0: Play Songs to Hardware

**Priority:** 0 (First)
**Goal:** `sqncr play chill-ambient.sqnc.yaml` outputs MIDI to your hardware
**Duration:** ~1 week

---

## The Wow Moment

This is the first demo. You run a command, and music comes out of your hardware synths.

```bash
sqncr play examples/chill-ambient.sqnc.yaml --device "Polyend Synth"
```

That's it. Everything else builds on this.

---

## What We're Building

1. **Minimal .NET solution** - Just enough to compile and run
2. **MIDI output** - DryWetMidi sends notes to hardware
3. **.sqnc.yaml parser** - Read patterns and play them
4. **CLI** - `play`, `stop`, `list-devices` commands
5. **Device enumeration** - See what's connected

## What We're NOT Building Yet

- Full music theory library (just read what's in the file)
- Skills framework
- Agents
- MCP server / REST API / SDK
- Session persistence (save comes in P1)
- Full observability (basic logging only)
- Tests (get it working first)

---

## Tasks

### Task 1: Initialize Solution ✅

```bash
cd C:\src\SqncR
dotnet new sln -n SqncR

mkdir src
cd src
dotnet new console -n SqncR.Cli -f net9.0
dotnet new classlib -n SqncR.Core -f net9.0
dotnet new classlib -n SqncR.Midi -f net9.0
cd ..

dotnet sln add src/SqncR.Cli
dotnet sln add src/SqncR.Core
dotnet sln add src/SqncR.Midi
```

**Project References:**
- SqncR.Cli → SqncR.Core
- SqncR.Core → SqncR.Midi

**Deliverable:** Solution builds with `dotnet build`

---

### Task 2: Add NuGet Packages ✅

```bash
cd src/SqncR.Midi
dotnet add package Melanchall.DryWetMidi

cd ../SqncR.Core
dotnet add package YamlDotNet
```

**Packages:**
- `Melanchall.DryWetMidi` - MIDI I/O
- `YamlDotNet` - Parse .sqnc.yaml files

*Note: System.CommandLine was dropped in favor of simple manual arg parsing for fewer dependencies.*

---

### Task 3: MIDI Device Enumeration ✅

**File:** `src/SqncR.Midi/MidiService.cs`

```csharp
using Melanchall.DryWetMidi.Multimedia;

namespace SqncR.Midi;

public class MidiService
{
    public IReadOnlyList<MidiDeviceInfo> ListOutputDevices()
    {
        return OutputDevice.GetAll()
            .Select((d, i) => new MidiDeviceInfo(i, d.Name))
            .ToList();
    }
}

public record MidiDeviceInfo(int Index, string Name);
```

**CLI Command:** `sqncr list-devices`

```csharp
var listCommand = new Command("list-devices", "List available MIDI output devices");
listCommand.SetHandler(() =>
{
    var midi = new MidiService();
    var devices = midi.ListOutputDevices();

    if (devices.Count == 0)
    {
        Console.WriteLine("No MIDI devices found.");
        return;
    }

    Console.WriteLine("MIDI Output Devices:");
    foreach (var d in devices)
    {
        Console.WriteLine($"  [{d.Index}] {d.Name}");
    }
});
```

**Deliverable:** `sqncr list-devices` shows connected hardware

---

### Task 4: Send MIDI Notes ✅

**Add to MidiService:**

```csharp
public class MidiService : IDisposable
{
    private OutputDevice? _outputDevice;

    public void OpenDevice(int deviceIndex)
    {
        var devices = OutputDevice.GetAll().ToList();
        if (deviceIndex >= devices.Count)
            throw new ArgumentException($"Device index {deviceIndex} not found");

        _outputDevice = devices[deviceIndex];
    }

    public void OpenDevice(string deviceName)
    {
        var devices = OutputDevice.GetAll().ToList();
        var device = devices.FirstOrDefault(d =>
            d.Name.Contains(deviceName, StringComparison.OrdinalIgnoreCase));

        if (device == null)
            throw new ArgumentException($"Device '{deviceName}' not found");

        _outputDevice = device;
    }

    public void SendNoteOn(int channel, int note, int velocity)
    {
        if (_outputDevice == null)
            throw new InvalidOperationException("No device open");

        _outputDevice.SendEvent(new NoteOnEvent(
            (SevenBitNumber)note,
            (SevenBitNumber)velocity)
        {
            Channel = (FourBitNumber)(channel - 1)
        });
    }

    public void SendNoteOff(int channel, int note)
    {
        if (_outputDevice == null)
            throw new InvalidOperationException("No device open");

        _outputDevice.SendEvent(new NoteOffEvent(
            (SevenBitNumber)note,
            (SevenBitNumber)0)
        {
            Channel = (FourBitNumber)(channel - 1)
        });
    }

    public void Dispose()
    {
        _outputDevice?.Dispose();
    }
}
```

**Deliverable:** Can send note on/off to hardware

---

### Task 5: Parse .sqnc.yaml Files ✅

**File:** `src/SqncR.Core/SequenceParser.cs`

```csharp
using YamlDotNet.Serialization;

namespace SqncR.Core;

public class SequenceParser
{
    public Sequence Parse(string filePath)
    {
        var yaml = File.ReadAllText(filePath);
        var deserializer = new DeserializerBuilder().Build();
        return deserializer.Deserialize<Sequence>(yaml);
    }
}

// Models matching .sqnc.yaml structure
public record Sequence
{
    public MetaData Meta { get; init; } = new();
    public Dictionary<string, Pattern> Patterns { get; init; } = new();
    public Dictionary<string, Section> Sections { get; init; } = new();
    public List<ArrangeEntry> Arrange { get; init; } = new();
}

public record MetaData
{
    public string Title { get; init; } = "";
    public int Tempo { get; init; } = 120;
    public string Key { get; init; } = "C";
    public TimeSignature Time { get; init; } = new();
    public int Tpq { get; init; } = 480;
}

public record TimeSignature
{
    public int Beats { get; init; } = 4;
    public int Division { get; init; } = 4;
}

public record Pattern
{
    public int Length { get; init; }
    public List<NoteEvent> Events { get; init; } = new();
}

public record NoteEvent
{
    public int T { get; init; }        // Time in ticks
    public string Type { get; init; } = "note";
    public string Note { get; init; } = "C4";
    public int Vel { get; init; } = 80;
    public int Dur { get; init; } = 480;
}

public record Section
{
    public int Length { get; init; }
    public bool Loopable { get; init; }
    public List<Track> Tracks { get; init; } = new();
}

public record Track
{
    public int Ch { get; init; } = 1;
    public List<SequenceEntry> Sequence { get; init; } = new();
}

public record SequenceEntry
{
    public int At { get; init; }
    public string Pattern { get; init; } = "";
    public int Repeat { get; init; } = 1;
}

public record ArrangeEntry
{
    public int At { get; init; }
    public string Section { get; init; } = "";
}
```

**Deliverable:** Can load and parse example .sqnc.yaml files

---

### Task 6: Note Name to MIDI Conversion ✅

**File:** `src/SqncR.Core/NoteParser.cs`

```csharp
namespace SqncR.Core;

public static class NoteParser
{
    private static readonly Dictionary<string, int> NoteOffsets = new()
    {
        ["C"] = 0, ["C#"] = 1, ["Db"] = 1,
        ["D"] = 2, ["D#"] = 3, ["Eb"] = 3,
        ["E"] = 4, ["Fb"] = 4, ["E#"] = 5,
        ["F"] = 5, ["F#"] = 6, ["Gb"] = 6,
        ["G"] = 7, ["G#"] = 8, ["Ab"] = 8,
        ["A"] = 9, ["A#"] = 10, ["Bb"] = 10,
        ["B"] = 11, ["Cb"] = 11, ["B#"] = 0
    };

    public static int Parse(string noteName)
    {
        // Parse "C4", "F#2", "Bb5", etc.
        var match = System.Text.RegularExpressions.Regex.Match(
            noteName, @"^([A-Ga-g][#b]?)(-?\d)$");

        if (!match.Success)
            throw new ArgumentException($"Invalid note: {noteName}");

        var note = match.Groups[1].Value;
        var octave = int.Parse(match.Groups[2].Value);

        // C4 = MIDI 60
        return NoteOffsets[note] + ((octave + 1) * 12);
    }
}
```

**Deliverable:** "C4" → 60, "A4" → 69, etc.

---

### Task 7: Sequence Player ✅

**File:** `src/SqncR.Core/SequencePlayer.cs`

```csharp
namespace SqncR.Core;

public class SequencePlayer
{
    private readonly MidiService _midi;
    private CancellationTokenSource? _cts;
    private bool _isPlaying;

    public SequencePlayer(MidiService midi)
    {
        _midi = midi;
    }

    public async Task PlayAsync(Sequence sequence, CancellationToken cancellationToken)
    {
        _isPlaying = true;
        var msPerTick = 60000.0 / sequence.Meta.Tempo / sequence.Meta.Tpq;

        // Build timeline from arrange → sections → patterns
        var timeline = BuildTimeline(sequence);

        Console.WriteLine($"Playing: {sequence.Meta.Title}");
        Console.WriteLine($"Tempo: {sequence.Meta.Tempo} BPM, Key: {sequence.Meta.Key}");
        Console.WriteLine("Press Ctrl+C to stop");

        var startTime = DateTime.UtcNow;

        foreach (var evt in timeline.OrderBy(e => e.AbsoluteTime))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var targetTime = startTime.AddMilliseconds(evt.AbsoluteTime * msPerTick);
            var delay = targetTime - DateTime.UtcNow;

            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, cancellationToken);

            if (evt.IsNoteOn)
            {
                _midi.SendNoteOn(evt.Channel, evt.MidiNote, evt.Velocity);
                Console.WriteLine($"  ON:  Ch{evt.Channel} {evt.NoteName} vel={evt.Velocity}");
            }
            else
            {
                _midi.SendNoteOff(evt.Channel, evt.MidiNote);
            }
        }

        _isPlaying = false;
    }

    private List<TimelineEvent> BuildTimeline(Sequence sequence)
    {
        var events = new List<TimelineEvent>();

        foreach (var arrange in sequence.Arrange)
        {
            if (!sequence.Sections.TryGetValue(arrange.Section, out var section))
                continue;

            foreach (var track in section.Tracks)
            {
                foreach (var seqEntry in track.Sequence)
                {
                    if (!sequence.Patterns.TryGetValue(seqEntry.Pattern, out var pattern))
                        continue;

                    for (int rep = 0; rep < seqEntry.Repeat; rep++)
                    {
                        var offset = arrange.At + seqEntry.At + (rep * pattern.Length);

                        foreach (var note in pattern.Events.Where(e => e.Type == "note"))
                        {
                            var midiNote = NoteParser.Parse(note.Note);
                            var noteOnTime = offset + note.T;
                            var noteOffTime = noteOnTime + note.Dur;

                            events.Add(new TimelineEvent
                            {
                                AbsoluteTime = noteOnTime,
                                Channel = track.Ch,
                                MidiNote = midiNote,
                                NoteName = note.Note,
                                Velocity = note.Vel,
                                IsNoteOn = true
                            });

                            events.Add(new TimelineEvent
                            {
                                AbsoluteTime = noteOffTime,
                                Channel = track.Ch,
                                MidiNote = midiNote,
                                NoteName = note.Note,
                                IsNoteOn = false
                            });
                        }
                    }
                }
            }
        }

        return events;
    }

    public void Stop()
    {
        _cts?.Cancel();
    }
}

public class TimelineEvent
{
    public int AbsoluteTime { get; init; }
    public int Channel { get; init; }
    public int MidiNote { get; init; }
    public string NoteName { get; init; } = "";
    public int Velocity { get; init; }
    public bool IsNoteOn { get; init; }
}
```

**Deliverable:** Plays .sqnc.yaml files with correct timing

---

### Task 8: CLI Play Command ✅

**File:** `src/SqncR.Cli/Program.cs`

```csharp
using System.CommandLine;
using SqncR.Core;
using SqncR.Midi;

var rootCommand = new RootCommand("SqncR - AI-Native Generative Music");

// list-devices
var listCommand = new Command("list-devices", "List MIDI output devices");
listCommand.SetHandler(() =>
{
    using var midi = new MidiService();
    var devices = midi.ListOutputDevices();

    if (devices.Count == 0)
    {
        Console.WriteLine("No MIDI devices found.");
        return;
    }

    foreach (var d in devices)
        Console.WriteLine($"[{d.Index}] {d.Name}");
});
rootCommand.AddCommand(listCommand);

// play
var playCommand = new Command("play", "Play a .sqnc.yaml sequence file");
var fileArg = new Argument<FileInfo>("file", "The .sqnc.yaml file to play");
var deviceOpt = new Option<string>("--device", "MIDI device name or index");
deviceOpt.AddAlias("-d");

playCommand.AddArgument(fileArg);
playCommand.AddOption(deviceOpt);

playCommand.SetHandler(async (file, device) =>
{
    if (!file.Exists)
    {
        Console.WriteLine($"File not found: {file.FullName}");
        return;
    }

    using var midi = new MidiService();
    var devices = midi.ListOutputDevices();

    if (devices.Count == 0)
    {
        Console.WriteLine("No MIDI devices found.");
        return;
    }

    // Open device
    if (string.IsNullOrEmpty(device))
    {
        Console.WriteLine("Available devices:");
        foreach (var d in devices)
            Console.WriteLine($"  [{d.Index}] {d.Name}");
        Console.WriteLine("\nUse --device to specify one.");
        return;
    }

    if (int.TryParse(device, out var idx))
        midi.OpenDevice(idx);
    else
        midi.OpenDevice(device);

    // Parse and play
    var parser = new SequenceParser();
    var sequence = parser.Parse(file.FullName);
    var player = new SequencePlayer(midi);

    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
        Console.WriteLine("\nStopping...");
    };

    try
    {
        await player.PlayAsync(sequence, cts.Token);
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Stopped.");
    }

}, fileArg, deviceOpt);
rootCommand.AddCommand(playCommand);

// stop (for future use with background playback)
var stopCommand = new Command("stop", "Stop playback");
stopCommand.SetHandler(() => Console.WriteLine("Nothing playing."));
rootCommand.AddCommand(stopCommand);

return await rootCommand.InvokeAsync(args);
```

**Deliverable:** Full CLI with play, stop, list-devices

---

## Definition of Done

- [x] `dotnet build` succeeds
- [x] `sqncr list-devices` shows connected MIDI hardware
- [x] `sqncr play examples/chill-ambient.sqnc.yaml --device 0` plays music
- [x] Notes play at correct tempo with correct timing
- [x] Ctrl+C stops playback cleanly
- [x] Works with at least one real hardware device (Microsoft GS Wavetable Synth tested)

---

## Demo

```bash
# Build
dotnet build

# List devices
dotnet run --project src/SqncR.Cli -- list-devices
# Output: [0] Polyend Synth MIDI 1
#         [1] Moog Mother-32

# Play!
dotnet run --project src/SqncR.Cli -- play examples/chill-ambient.sqnc.yaml -d 0
# Output: Playing: Late Night Ambient
#         Tempo: 70 BPM, Key: Cm
#         Press Ctrl+C to stop
#           ON:  Ch1 C2 vel=70
#           ON:  Ch1 C2 vel=68
#         ...
```

---

## What's Next

**P1: Save Sessions** - Add save/load functionality, basic persistence

---

**Priority:** P0 (First)
**Status:** ✅ Complete
**Updated:** January 29, 2026
