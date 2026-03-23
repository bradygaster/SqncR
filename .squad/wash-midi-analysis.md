# MIDI Hardware Path Analysis — Wash

**Analysis Date:** 2026-02-13  
**For:** Brady  
**Subject:** Architecture for "tell me about your setup → play continuously" workflow

---

## Executive Summary

The hardware MIDI path needs five layers:
1. **Device discovery** (what's plugged in?)
2. **Device profiles** (what can each one do?)
3. **Setup interview** (conversational + auto-detect hybrid)
4. **Real-time playback** (clock-locked generator loop)
5. **Multi-device orchestration** (coordinated timing across devices)

The key insight: MIDI's 480 TPQ standard gives us a 10ms resolution window at typical tempos. That's your hard constraint.

---

## 1. Device Discovery: Questions for the MCP Conversation

The system needs to ask the right questions in the right order. Here's what the setup interview should cover:

### Questions MCP Server Should Ask

**Phase 1: Auto-Detection**
```
"What MIDI devices do you have connected? I found these:"
  → List all input/output devices (both hardware and virtual)
  → Ask user to confirm/correct
```

**Phase 2: Per-Device Setup**
```
For each device:
  1. "What's the primary sound character of this device?"
     Options: bass, pad, lead, texture, percussion, drums, ambient
  2. "What patches/programs are currently loaded? (describe freely)"
     Example: "warm sub-bass with LFO on filter"
  3. "What's the polyphony limit?"
     Example: "8 voices max, uses note stealing"
  4. "Any special handling for this device?"
     - Velocity sensitivity (linear, log, fixed)
     - Aftertouch capable?
     - Program change support?
     - CC-based parameter control? (which CCs?)
     - Sustain pedal expected on channel 64?
```

**Phase 3: Connectivity**
```
  1. "Which MIDI output channel(s) should SqncR use for this device?"
     Default: 1 per device, but allow multi-channel if user wants
  2. "If multiple devices, should I sync them to a single clock?"
     Answer: yes, always — but explain the tradeoff
```

**Phase 4: Context**
```
  1. "What's your typical working tempo range?" (usually 60-160 BPM)
  2. "Save this setup as..." (quick save/load)
```

### Why This Order?

- **Auto-detect first**: Respects the user's existing setup, reduces friction
- **Character before channels**: Forces user to think about sonic palette (not just wiring)
- **Polyphony & velocity explicitly**: These are the constraints the generator must respect
- **Clock sync question last**: By then user trusts you know their setup

---

## 2. Device Profiles: Data Model

A device profile describes what a device can do. This is **data, not code** — no special-casing in the generator.

### Minimal Viable Profile

```csharp
public class DeviceProfile
{
    // Identity
    public string DeviceId { get; set; }           // UUID or friendly name
    public string HardwareName { get; set; }       // "Moog Sub 37", "Serum"
    public string PatchDescription { get; set; }   // User's description of current sound
    
    // Sonic character (influences generation decisions)
    public string Role { get; set; }               // "bass" | "pad" | "lead" | "texture" | "percussion"
    public int Brightness { get; set; }            // 0-10, user estimate (affects register choices)
    
    // MIDI Capabilities (hard constraints)
    public int MidiChannel { get; set; }           // 1-16
    public int PolyphonyLimit { get; set; }        // -1 = unlimited, else explicit limit
    public bool SupportsAftertouch { get; set; }
    public bool SupportsVelocity { get; set; }
    
    // Velocity response (how to interpret velocity values)
    public VelocityProfile VelocityProfile { get; set; }
    
    // Control surface mapping (what can be modulated)
    public Dictionary<string, int> CcMappings { get; set; }  // "filter_cutoff" → CC 74, etc.
    public bool SustainPedalOnCc64 { get; set; }
    
    // Latency metadata (for timing compensation)
    public int ApproximateLatencyMs { get; set; }  // 0 = unknown, user estimate OK
    
    // Range constraints
    public int LowestNote { get; set; } = 0;       // MIDI note number
    public int HighestNote { get; set; } = 127;
}

public class VelocityProfile
{
    public enum ResponseCurve { Linear, Logarithmic, Threshold }
    public ResponseCurve Curve { get; set; } = ResponseCurve.Linear;
    public int MinVelocity { get; set; } = 1;      // Below this → silence
    public int MaxVelocity { get; set; } = 127;
    public int PreferredDynamicRange { get; set; } = 60;  // Useful velocity range in use
}
```

### Why This Design?

- **No special cases in code**: The generator queries these at runtime
- **User-estimated values are OK**: Polyphony, latency, brightness — user input is good enough
- **Velocity response is explicit**: Different devices react very differently to velocity
- **CC mappings are optional**: Only add them if user actually controls parameters
- **Extensible without modification**: Add new fields as needed

### Example Profile (Moog Sub 37)

```yaml
device_id: moog-sub-37-1
hardware_name: Moog Sub 37
patch_description: "Warm sub-bass with LFO on filter cutoff, a bit of drive"
role: bass
brightness: 3
midi_channel: 1
polyphony_limit: 1
supports_aftertouch: false
supports_velocity: true
velocity_profile:
  curve: logarithmic
  min_velocity: 10
  max_velocity: 127
  preferred_dynamic_range: 50
cc_mappings:
  filter_cutoff: 74
  filter_resonance: 71
  lfo_rate: 76
sustain_pedal_on_cc64: false
approximate_latency_ms: 2
lowest_note: 27      # A0
highest_note: 88     # E7
```

---

## 3. Setup Interview: Hybrid Auto-Detect + Conversational

### The Flow

```
1. System: "Let me scan for MIDI devices..."
   → Calls Windows MIDI API (or RtMidi)
   → Lists all OutputDevices

2. User: "Yes, that's my setup"
   System: "Great. I found:
     - Moog Sub 37 (device 0)
     - Serum in VST (device 1)
     - Wavetable in Ableton (device 2)"

3. System: "Let's describe each one.
   Starting with Moog Sub 37 on device 0.
   What's the character of the sound you have loaded?"
   
   User: "Warm sub-bass with some filter modulation"

4. System: "Nice. How many voices can it play simultaneously?"
   User: "Just one — it's monophonic"

5. [Repeat for each device]

6. System: "Should I clock all three to the same tempo?"
   User: "Yes"

7. System: "Saving your setup as 'My Studio Session'..."
   → Writes profiles to JSON/YAML
   → User can load next session instantly
```

### Best UX: Question Order

1. **Auto-list all devices** (respects what they have)
2. **Device-by-device character** (short free text)
3. **Polyphony** (dropdown: 1, 2-4, 5-8, 16+, unlimited)
4. **Velocity sensitivity** (dropdown: responsive, linear, fixed)
5. **Special notes** (free text: "has a sustain pedal", "stutters on fast runs")
6. **Confirm channels** (auto-assign 1 per device, allow reorder)
7. **Save setup name** (quick future recall)

### Implementation Considerations

- **Don't force all devices to same channel**: Some users may want stereo spread or layer multiple devices on same channel (though unusual)
- **Allow editing after initial setup**: Profiles should be editable without rebuilding
- **Store as JSON/YAML** in `~/.sqncr/devices/` for persistence
- **No firmware detection required**: User description > auto-detection of patch name (most devices don't report patch over MIDI anyway)

---

## 4. Real-Time Playback Loop: Architecture from MIDI Perspective

This is the hardest part. Here's how it should work:

### Clock Source & Timing Resolution

**The constraint:** MIDI events happen in discrete time steps.

```
Typical MIDI timing:
  TPQ (Ticks Per Quarter) = 480  (standard, set in Sequence.Meta.Tpq)
  Tempo = 120 BPM (example)
  
  Time per tick = 60,000 ms / 120 BPM / 480 TPQ
                = 1.04 ms per tick
  
  Quantization: ~1 ms precision at this tempo
  Hardware latency: typically 2-10 ms (varies wildly)
  
  Conclusion: Human can't hear 1 ms timing errors.
              Device latency (2-10 ms) is the real bottleneck.
```

### The Playback Loop

The generator should operate in **ticks**, not milliseconds. Here's the architecture:

```csharp
public class RealtimePlaybackEngine
{
    private readonly MidiService _midi;
    private readonly GenerationEngine _generator;
    private int _currentTick = 0;
    private int _tpq = 480;
    private int _tempo = 120;
    
    public async Task PlayContinuously(
        IEnumerable<DeviceProfile> devices,
        GenerationContext context,  // Key, intensity, description, etc.
        CancellationToken stop)
    {
        var tickIntervalMs = CalculateTickInterval(_tempo, _tpq);
        
        using (var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(tickIntervalMs)))
        {
            while (await timer.WaitForNextTickAsync(stop))
            {
                // 1. Ask generator for next batch of events at current tick
                var events = _generator.GenerateNext(_currentTick, context);
                
                // 2. Send to appropriate devices (respecting their profiles)
                foreach (var evt in events)
                {
                    var profile = devices.First(d => d.DeviceId == evt.DeviceId);
                    SendNoteEvent(profile, evt);
                }
                
                // 3. Increment and continue
                _currentTick++;
                
                // 4. Periodically check for user interruption/modification
                if (context.IsModified)
                {
                    context.ApplyModifications();
                    context.IsModified = false;
                }
            }
        }
    }
    
    private double CalculateTickInterval(int tempo, int tpq)
    {
        // Milliseconds per tick
        return 60_000.0 / tempo / tpq;
    }
    
    private void SendNoteEvent(DeviceProfile profile, NoteEvent evt)
    {
        if (evt.IsNoteOn)
        {
            // Respect polyphony limit: track active notes on this device
            if (profile.PolyphonyLimit > 0 && 
                ActiveNotesOn(profile.DeviceId) >= profile.PolyphonyLimit)
            {
                // Option 1: Drop the note (safe)
                // Option 2: Use note stealing (aggressive, user hears glitch)
                return;  // Drop silently
            }
            
            _midi.SendNoteOn(
                profile.MidiChannel,
                evt.Note,
                ScaleVelocity(evt.Velocity, profile.VelocityProfile)
            );
        }
        else
        {
            _midi.SendNoteOff(profile.MidiChannel, evt.Note);
        }
    }
    
    private int ScaleVelocity(int generated, VelocityProfile profile)
    {
        // Generator produces 0-127 by default
        // This method respects device's velocity response curve
        if (profile.Curve == VelocityProfile.ResponseCurve.Logarithmic)
        {
            // Map to device's preferred range with log scaling
            var ratio = Math.Pow(generated / 127.0, 0.5);  // Simplified
            return profile.MinVelocity + (int)(ratio * (profile.MaxVelocity - profile.MinVelocity));
        }
        return generated;
    }
}
```

### Key Design Decisions

1. **Tick-based, not millisecond-based**: Easier to reason about. Drift is expected and acceptable.
2. **PeriodicTimer for the metronome**: More precise than Task.Delay loops
3. **Polyphony limiting is silent**: Better than note stealing artifacts
4. **Velocity scaling per-device**: Each device has different response
5. **User modifications async**: Generator can change parameters without stopping playback
6. **No external clock**: SqncR drives the timing. If you want external sync later, that's a different layer.

### Latency Compensation

For now: **Don't attempt it**. Here's why:

- Each device has different latency (2-10 ms typical)
- You can't know exact latency without measuring
- Human hearing is forgiving at these scales
- Compensation introduces risk of sync issues

**Future improvement**: If user reports timing issues, ask them to measure their device's latency and store it in the profile. Then advance note-ons by that amount.

---

## 5. Multi-Device Orchestration

With 3-4 devices, how do you keep them in sync?

### The Architecture

**One clock, multiple channels:**

```
┌─────────────────────────────────────────────┐
│  RealtimePlaybackEngine (single tick clock)  │
│  Tick 0, Tick 1, Tick 2, ...                 │
└──────────────────┬──────────────────────────┘
                   │
        ┌──────────┼──────────┬───────────┐
        │          │          │           │
        ▼          ▼          ▼           ▼
   [Moog]    [Serum]     [Wavetable]  [Modular]
   Ch 1      Ch 2         Ch 3         Ch 4
```

Each device runs on a separate MIDI channel. They all increment the same clock tick.

### Coordination Strategies

**Strategy 1: Independent Voices (Simplest, Recommended for now)**
- Generator produces events for each device independently
- Each device respects its own polyphony/latency constraints
- Clock is shared, content is not
- **Pro:** No device interference, easy to debug
- **Con:** Less coordination (can add later)

**Strategy 2: Coordinated Harmony (Later)**
- Generator tracks which notes are playing on which devices
- Respects voice leading across devices
- Can play chords spread across devices
- **Pro:** Sophisticated arranging
- **Con:** Complex state management, harder to debug

**Strategy 3: Leader-Follower (Even Later)**
- One device is "harmonic center", others respond
- E.g., bass on device 1 leads, pads fill in around it
- **Pro:** Musically cohesive
- **Con:** Much harder implementation

### For Now: Independent Voices

```csharp
public class GenerationContext
{
    public List<DeviceProfile> Devices { get; set; }
    public string Key { get; set; }
    public int Tempo { get; set; }
    
    // Generator produces one voice per device
    public Dictionary<string, NoteEvent[]> GetNextEvents(int tick)
    {
        var results = new Dictionary<string, NoteEvent[]>();
        
        foreach (var device in Devices)
        {
            // Ask generator: "What should play on [device] at [tick]?"
            var events = _generator.GenerateForDevice(device, tick, this);
            results[device.DeviceId] = events;
        }
        
        return results;
    }
}
```

This keeps concerns separated and makes it trivial to add coordination later without breaking the playback engine.

---

## 6. .NET MIDI Libraries: Comparison & Recommendation

### Contenders

| Library | Type | Pros | Cons | License |
|---------|------|------|------|---------|
| **DryWetMidi** (used now) | Managed .NET | Full MIDI 2.0 support, excellent docs, reads/writes SMF, good API | Slightly heavier, more features than needed | MIT |
| **NAudio.Midi** | Managed .NET | Lightweight, good for WinMM, device enum works | Minimal abstraction, older API feel | Microsoft Public License |
| **RtMidi.NET** | P/Invoke wrapper | Cross-platform, industry standard (RtMidi) | Platform-specific code needed, less .NET idiomatic | MIT |
| **Sanford.Multimedia.Midi** | Managed .NET | Mature, stable, used in production music apps | Minimal recent updates, older codebase | Unclear licensing |

### Recommendation: **Stick with DryWetMidi**

**Why:**

1. **Already in use** (MidiService.cs uses it) — no migration cost
2. **Full MIDI 2.0 ready** — future-proofing if hardware evolves
3. **Device enumeration works well** — `OutputDevice.GetAll()` is exactly what you need
4. **Good API design** — `NoteOnEvent`, `ControlChangeEvent` etc are clean
5. **Active maintenance** — library is well-supported
6. **MIT licensed** — fits your open-source ethos

**What you're already doing right:**

```csharp
OutputDevice.GetAll()  // ✓ Good device enumeration
NoteOnEvent/NoteOffEvent  // ✓ Simple events
```

### What You'll Need to Add

1. **ControlChangeEvent** - For CC-based modulation (filter, volume, effects)
   ```csharp
   _outputDevice.SendEvent(new ControlChangeEvent(
       (SevenBitNumber)ccNumber,
       (SevenBitNumber)value)
   {
       Channel = (FourBitNumber)(channel - 1)
   });
   ```

2. **ProgramChangeEvent** - For patch selection
   ```csharp
   _outputDevice.SendEvent(new ProgramChangeEvent(
       (SevenBitNumber)programNumber)
   {
       Channel = (FourBitNumber)(channel - 1)
   });
   ```

3. **InputDevice listening** - To read user's MIDI (play-along mode)
   ```csharp
   var input = InputDevice.GetByName("My Keyboard");
   input.EventReceived += (_, e) => HandleIncomingEvent(e.Event);
   input.StartEventsListening();
   ```

### Avoid These Pitfalls

1. **Don't use the SMF (MIDI file) loader for realtime**
   - OK for playback of pre-composed sequences (what SequencePlayer does)
   - NOT for generating and sending in realtime
   - Two different code paths

2. **Device enumeration changes at runtime**
   - If user plugs in a device, GetAll() won't reflect it
   - For now, require user to restart the MCP server
   - (Future: hot-plug detection)

3. **No implicit syncing across devices**
   - You must manage the clock explicitly
   - DryWetMidi sends immediately; you manage timing

---

## Summary Table: Questions → Implementation

| Question | Answer | Implementation Detail |
|----------|--------|----------------------|
| **Device discovery** | Auto-list + conversational per-device | MCP tool calls `OutputDevice.GetAll()`, ask character/polyphony/velocity in interview |
| **Device profiles** | YAML/JSON data files, not code | DeviceProfile class, persisted in `~/.sqncr/devices/` |
| **Setup interview** | Hybrid (auto-detect, then describe) | 7-step conversational flow, save/load by name |
| **Playback loop** | Tick-based (480 TPQ), PeriodicTimer | RealtimePlaybackEngine, tick increment each cycle, async event handling |
| **Multi-device** | Single clock, independent voices | One PeriodicTimer drives all channels, device-independent generation |
| **.NET MIDI library** | DryWetMidi (stick with it) | Add ControlChangeEvent, ProgramChangeEvent, InputDevice for play-along |

---

## Remaining Questions for Brady

Before I spike code, I'd like to confirm:

1. **Latency tolerance**: Is "within ~2ms of target" good enough, or do you need tighter sync?
2. **Polyphony strategy**: Silent drop or note stealing on overflow?
3. **Virtual clock or external sync**: Drive clock internally (recommended), or support MIDI clock input later?
4. **VCV Rack integration**: If you sync to VCV, does it expect clock input, or can it follow?

---

## Next Steps

1. **Immediate**: Create `DeviceProfile` and `VelocityProfile` classes, add YAML serialization
2. **Soon**: Build the MCP interview flow in the server (conversational setup)
3. **Then**: Implement `RealtimePlaybackEngine` with tick-based metronome
4. **Validate**: Test with real hardware (even 1-2 devices worth of latency testing)

This is solid ground to build on. MIDI is finicky, but this design respects its constraints without over-engineering.
