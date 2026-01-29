# Sprint 01: Music Theory & MIDI Foundation

**Duration:** 2 weeks  
**Goal:** Build core music theory library and MIDI service with device scanning

---

## Sprint Objectives

- ✅ Implement music theory value types (Note, Scale, Chord, Interval)
- ✅ Build MIDI service with DryWetMidi
- ✅ Device scanning and profile matching
- ✅ First device profile working (Polyend Synth)
- ✅ Unit tests with music theory correctness

---

## User Stories

### US-01-01: Music theory value types
**As a developer, I need strongly-typed music theory primitives**

**Acceptance Criteria:**
- [ ] Note struct with MIDI number, name, octave, frequency
- [ ] Interval struct with semitones, quality, name
- [ ] Scale record with notes, mode, key
- [ ] Chord record with notes, quality, extensions
- [ ] All types are immutable
- [ ] 100% unit test coverage
- [ ] XML documentation complete

### US-01-02: MIDI device scanning
**As a user, I want SqncR to detect my connected MIDI devices**

**Acceptance Criteria:**
- [ ] List all MIDI input devices
- [ ] List all MIDI output devices
- [ ] Return device names as reported by OS
- [ ] Return device indices (port numbers)
- [ ] Handle no devices connected gracefully
- [ ] Observable in Aspire Dashboard

### US-01-03: Device profile matching
**As a user, I want devices auto-matched to SqncR profiles**

**Acceptance Criteria:**
- [ ] "Polyend Synth MIDI 1" matches to polyend-synth profile
- [ ] "Moog Mother-32" matches to moog-mother32 profile
- [ ] Profile loads capabilities (channels, polyphony, roles)
- [ ] Unmatched devices shown as "generic MIDI device"
- [ ] User can reference device by name

### US-01-04: Send MIDI notes
**As a user, I want to send notes to devices by name/index**

**Acceptance Criteria:**
- [ ] Send NOTE_ON by device index + channel
- [ ] Send NOTE_OFF by device index + channel
- [ ] Specify velocity (0-127)
- [ ] Every message traced in Aspire Dashboard
- [ ] Latency < 10ms
- [ ] Works with real hardware

---

## Tasks

### Task 1: Implement Note Value Type

**File:** `src/SqncR.Theory/Models/Note.cs`

```csharp
namespace SqncR.Theory.Models;

/// <summary>
/// Represents a musical note as a MIDI number (0-127).
/// </summary>
public readonly record struct Note(int MidiNumber)
{
    public Note(string noteName) : this(ParseNoteName(noteName)) { }
    
    public string Name => GetNoteName();
    public int Octave => (MidiNumber / 12) - 1;
    public int PitchClass => MidiNumber % 12;
    
    public double Frequency(double a4Tuning = 440.0)
    {
        return a4Tuning * Math.Pow(2, (MidiNumber - 69) / 12.0);
    }
    
    public Note Transpose(int semitones) => new(MidiNumber + semitones);
    
    private string GetNoteName()
    {
        var noteNames = new[] { "C", "C#", "D", "D#", "E", "F", 
                                "F#", "G", "G#", "A", "A#", "B" };
        return $"{noteNames[PitchClass]}{Octave}";
    }
    
    private static int ParseNoteName(string name)
    {
        // Implementation: "C4" -> 60, "A4" -> 69, etc.
    }
}
```

**Tests to Write:**
- [ ] Note(60) returns "C4"
- [ ] Note("A4") equals Note(69)
- [ ] Note(69).Frequency() returns 440.0
- [ ] Transpose works correctly
- [ ] Invalid MIDI numbers throw exceptions

**Estimated Time:** 3 hours

---

### Task 2: Implement Interval Value Type

**File:** `src/SqncR.Theory/Models/Interval.cs`

```csharp
namespace SqncR.Theory.Models;

/// <summary>
/// Represents a musical interval between two notes.
/// </summary>
public readonly record struct Interval(int Semitones)
{
    public string Name => GetIntervalName();
    public string Quality => GetQuality();
    
    private string GetIntervalName()
    {
        // P1, m2, M2, m3, M3, P4, TT, P5, m6, M6, m7, M7, P8
    }
    
    public static Interval Between(Note note1, Note note2)
    {
        return new Interval(Math.Abs(note2.MidiNumber - note1.MidiNumber));
    }
}
```

**Tests:**
- [ ] Interval(0) is "Perfect Unison"
- [ ] Interval(7) is "Perfect Fifth"
- [ ] Interval.Between(C4, G4) is "Perfect Fifth"

**Estimated Time:** 2 hours

---

### Task 3: Implement Scale Record

**File:** `src/SqncR.Theory/Models/Scale.cs`

```csharp
namespace SqncR.Theory.Models;

public record Scale(Note Root, ScaleMode Mode)
{
    public IReadOnlyList<Note> Notes => GenerateNotes();
    public string Name => $"{Root.Name} {Mode}";
    
    private IReadOnlyList<Note> GenerateNotes()
    {
        var intervals = Mode switch
        {
            ScaleMode.Major => new[] { 0, 2, 4, 5, 7, 9, 11 },
            ScaleMode.Minor => new[] { 0, 2, 3, 5, 7, 8, 10 },
            ScaleMode.Dorian => new[] { 0, 2, 3, 5, 7, 9, 10 },
            ScaleMode.Phrygian => new[] { 0, 1, 3, 5, 7, 8, 10 },
            // ... all modes
        };
        
        return intervals.Select(i => Root.Transpose(i)).ToList();
    }
}

public enum ScaleMode
{
    Major, Minor, Dorian, Phrygian, Lydian, Mixolydian, Locrian
}
```

**Tests:**
- [ ] C Major has correct notes [C, D, E, F, G, A, B]
- [ ] A Minor has correct notes [A, B, C, D, E, F, G]
- [ ] All modes tested for correctness

**Estimated Time:** 4 hours

---

### Task 4: Implement Chord Record

**File:** `src/SqncR.Theory/Models/Chord.cs`

```csharp
namespace SqncR.Theory.Models;

public record Chord(Note Root, ChordQuality Quality)
{
    public IReadOnlyList<Note> Notes => GenerateNotes();
    public string Symbol => $"{Root.Name}{GetSymbol()}";
    
    private IReadOnlyList<Note> GenerateNotes()
    {
        var intervals = Quality switch
        {
            ChordQuality.Major => new[] { 0, 4, 7 },
            ChordQuality.Minor => new[] { 0, 3, 7 },
            ChordQuality.Major7 => new[] { 0, 4, 7, 11 },
            ChordQuality.Minor7 => new[] { 0, 3, 7, 10 },
            // ... all qualities
        };
        
        return intervals.Select(i => Root.Transpose(i)).ToList();
    }
}

public enum ChordQuality
{
    Major, Minor, Diminished, Augmented,
    Major7, Minor7, Dominant7, MinorMajor7,
    // ... etc
}
```

**Tests:**
- [ ] Cmaj has notes [C, E, G]
- [ ] Am7 has notes [A, C, E, G]
- [ ] All chord qualities correct

**Estimated Time:** 4 hours

---

### Task 5: Implement MIDI Device Scanning

**File:** `src/SqncR.Midi/MidiService.cs`

```csharp
using Melanchall.DryWetMidi.Devices;

namespace SqncR.Midi;

public class MidiService : IMidiService
{
    private readonly ActivitySource _activitySource;
    private readonly ILogger<MidiService> _logger;
    
    public async Task<IReadOnlyList<MidiDevice>> ListDevicesAsync()
    {
        using var activity = _activitySource.StartActivity("ListMidiDevices");
        
        var inputDevices = InputDevice.GetAll().ToList();
        var outputDevices = OutputDevice.GetAll().ToList();
        
        activity?.SetTag("midi.input_count", inputDevices.Count);
        activity?.SetTag("midi.output_count", outputDevices.Count);
        
        var devices = new List<MidiDevice>();
        
        for (int i = 0; i < outputDevices.Count; i++)
        {
            var device = new MidiDevice
            {
                Index = i,
                PortName = outputDevices[i].Name,
                // Profile matching in next task
            };
            devices.Add(device);
        }
        
        return devices;
    }
}
```

**Checklist:**
- [ ] Implement IMidiService interface
- [ ] Use DryWetMidi to enumerate devices
- [ ] Add OpenTelemetry tracing
- [ ] Handle no devices gracefully
- [ ] Test with real MIDI devices connected
- [ ] Test with no devices connected

**Estimated Time:** 4 hours

---

### Task 6: Implement Device Profile System

**File:** `src/SqncR.Midi/Devices/IDeviceProfile.cs`

```csharp
namespace SqncR.Midi.Devices;

public interface IDeviceProfile
{
    string ProfileId { get; }
    string DeviceName { get; }
    string Manufacturer { get; }
    DeviceType Type { get; }
    int[] MidiChannels { get; }
    int Polyphony { get; }
    string[] Roles { get; }
    string[] Characteristics { get; }
    bool Matches(string portName);
}
```

**File:** `src/SqncR.Midi/Devices/Profiles/PolyendSynthProfile.cs`

```csharp
public class PolyendSynthProfile : IDeviceProfile
{
    public string ProfileId => "polyend-synth";
    public string DeviceName => "Polyend Synth";
    public string Manufacturer => "Polyend";
    public DeviceType Type => DeviceType.Synth;
    public int[] MidiChannels => new[] { 1, 2, 3 };
    public int Polyphony => 8;
    public string[] Roles => new[] { "bass", "chords", "pads", "lead" };
    public string[] Characteristics => new[] { "versatile", "modern", "digital" };
    
    public bool Matches(string portName)
    {
        return portName.Contains("Polyend Synth", StringComparison.OrdinalIgnoreCase);
    }
}
```

**Checklist:**
- [ ] Create IDeviceProfile
- [ ] Implement PolyendSynthProfile
- [ ] Implement DeviceRegistry
- [ ] Profile matching logic
- [ ] Test matching with real device names

**Estimated Time:** 4 hours

---

### Task 7: Integrate Profiles into Device Listing

**Update MidiService to match profiles:**

```csharp
var devices = new List<MidiDevice>();

for (int i = 0; i < outputDevices.Count; i++)
{
    var portName = outputDevices[i].Name;
    var profile = _deviceRegistry.MatchProfile(portName);
    
    var device = new MidiDevice
    {
        Index = i,
        PortName = portName,
        ProfileMatch = profile?.ProfileId,
        Manufacturer = profile?.Manufacturer,
        Type = profile?.Type ?? DeviceType.Unknown,
        Channels = profile?.MidiChannels ?? Array.Empty<int>()
    };
    
    activity?.AddEvent(new ActivityEvent($"Found device: {portName}"));
    devices.Add(device);
}
```

**Checklist:**
- [ ] Integrate DeviceRegistry into MidiService
- [ ] Match devices to profiles on scan
- [ ] Return enriched device information
- [ ] Test with Polyend Synth connected
- [ ] Verify in Aspire Dashboard

**Estimated Time:** 2 hours

---

### Task 8: Implement Send MIDI Note

**Add to MidiService:**

```csharp
public async Task SendNoteOnAsync(int deviceIndex, int channel, int note, int velocity)
{
    using var activity = _activitySource.StartActivity("SendMidiNoteOn");
    
    var device = _openDevices.GetOrAdd(deviceIndex, OpenDevice);
    
    activity?.SetTag("midi.device_index", deviceIndex);
    activity?.SetTag("midi.device_name", device.Name);
    activity?.SetTag("midi.channel", channel);
    activity?.SetTag("midi.note", note);
    activity?.SetTag("midi.velocity", velocity);
    
    var stopwatch = Stopwatch.StartNew();
    
    device.SendEvent(new NoteOnEvent((SevenBitNumber)note, (SevenBitNumber)velocity)
    {
        Channel = (FourBitNumber)(channel - 1)
    });
    
    activity?.SetTag("midi.latency_ms", stopwatch.ElapsedMilliseconds);
    
    _logger.LogInformation("MIDI NOTE_ON sent: Device={Device} CH={Channel} Note={Note} Vel={Velocity}",
        device.Name, channel, note, velocity);
}
```

**Checklist:**
- [ ] Implement SendNoteOnAsync
- [ ] Implement SendNoteOffAsync
- [ ] Add OpenTelemetry tracing
- [ ] Add structured logging
- [ ] Track latency
- [ ] Test with real hardware
- [ ] Verify traces in Aspire Dashboard

**Estimated Time:** 4 hours

---

### Task 9: Unit Tests for Theory Library

**File:** `tests/SqncR.Theory.Tests/Models/NoteTests.cs`

```csharp
public class NoteTests
{
    [Fact]
    public void Note_MiddleC_ShouldBe60()
    {
        var note = new Note("C4");
        note.MidiNumber.Should().Be(60);
    }
    
    [Fact]
    public void Note_A4_ShouldBe440Hz()
    {
        var note = new Note(69);
        note.Frequency().Should().BeApproximately(440.0, 0.01);
    }
    
    [Theory]
    [InlineData(60, "C4")]
    [InlineData(69, "A4")]
    [InlineData(48, "C3")]
    public void Note_Name_ShouldBeCorrect(int midi, string expected)
    {
        var note = new Note(midi);
        note.Name.Should().Be(expected);
    }
}
```

**Checklist:**
- [ ] NoteTests (20+ tests)
- [ ] IntervalTests (15+ tests)
- [ ] ScaleTests (all modes verified)
- [ ] ChordTests (all qualities verified)
- [ ] Test coverage > 90%

**Estimated Time:** 6 hours

---

### Task 10: Integration Test - Device Scan and Note Send

**File:** `tests/SqncR.Integration.Tests/MidiDeviceTests.cs`

```csharp
public class MidiDeviceTests
{
    [Fact]
    public async Task ListDevices_ShouldReturnConnectedDevices()
    {
        var midiService = new MidiService(/* deps */);
        var devices = await midiService.ListDevicesAsync();
        
        devices.Should().NotBeNull();
        // If hardware connected, verify specific devices
    }
    
    [Fact]
    public async Task SendNote_ToPolyendSynth_ShouldTrace()
    {
        // Requires Polyend Synth connected
        var midiService = new MidiService(/* deps */);
        
        await midiService.SendNoteOnAsync(
            deviceIndex: 0,
            channel: 1,
            note: 60,
            velocity: 80
        );
        
        // Verify trace appears in telemetry
        // Verify no exceptions
    }
}
```

**Checklist:**
- [ ] Create integration test project
- [ ] Test device scanning
- [ ] Test note sending (requires hardware)
- [ ] Verify OpenTelemetry traces
- [ ] Document hardware requirements

**Estimated Time:** 3 hours

---

## Definition of Done

- ✅ Note, Interval, Scale, Chord types implemented and tested
- ✅ MidiService lists devices with profile matching
- ✅ MidiService sends NOTE_ON/NOTE_OFF successfully
- ✅ Polyend Synth profile matches real device
- ✅ All MIDI operations traced in Aspire Dashboard
- ✅ Unit tests pass (>90% coverage on theory)
- ✅ Integration tests pass (with hardware)
- ✅ All code has XML documentation
- ✅ No compiler warnings

---

## Deliverables

1. **Music Theory Library** - Note, Scale, Chord, Interval types
2. **MIDI Service** - Device scanning, note sending
3. **Device Profiles** - Polyend Synth profile working
4. **Test Suite** - Comprehensive unit + integration tests
5. **Observability** - All operations visible in dashboard

---

## Demo Script

**Show in Sprint Review:**

1. **Run Aspire:** `cd src/SqncR.AppHost && dotnet run`
2. **Open Dashboard:** http://localhost:15888
3. **Run CLI (future sprint):** `sqncr list-devices`
4. **Show device list** with Polyend Synth matched to profile
5. **Send test note:** Call MidiService.SendNoteOnAsync
6. **Hear sound** from Polyend Synth
7. **Show trace** in Aspire Dashboard:
   - Device name
   - Channel, note, velocity
   - Latency < 10ms
8. **Run tests:** `dotnet test` - all green
9. **Show theory tests:** Scales, chords verified correct

---

## Dependencies

**Hardware Required for Full Testing:**
- At least one MIDI device (Polyend Synth preferred)
- USB MIDI interface (or device with USB MIDI)

**Software:**
- .NET 9 SDK
- MIDI drivers installed

---

## Risks & Mitigation

**Risk:** DryWetMidi doesn't work on developer's OS  
**Mitigation:** Test on Windows/Mac/Linux early, have fallback plan

**Risk:** MIDI latency too high  
**Mitigation:** Measure early, optimize if needed, document requirements

**Risk:** Music theory implementation errors  
**Mitigation:** Extensive unit tests, verify against known-correct sources

---

## Next Sprint Preview

**Sprint 02: Core Skills & Service Facade**
- Implement first 6 skills
- Create SqncRService facade
- Wire skills to MIDI and theory services
- Skill registry with DI
- First end-to-end test: "generate drone" works

---

**Sprint Status:** 🔲 Not Started  
**Updated:** January 29, 2026
