---
title: "Stream-Ready: Building a Music Generator That Won't Crash Live"
date: 2026-02-15
tags: [sqncr, stability, streaming, generative-music, variety-engine, persistence]
summary: "M3 delivered the stability to leave SqncR running during live coding. Session persistence, automatic variety evolution, smooth transitions, and long-running health monitoring. Plus: shedding 24K lines of dead code."
---

# Stream-Ready: Building a Music Generator That Won't Crash Live

You're coding live. Streaming on Twitch. 200 people watching. You hit "start music generation." And for the next hour, your generative music engine just... *keeps going*. No crashes. No MIDI dropouts. No stuck notes hanging in the ether.

That was the challenge of M3.

M1 gave us the engine. M2 gave us sound. M3 gave us *confidence*. The kind of confidence you need to ship something that runs unattended while you're live on camera.

This post covers what it took to make SqncR stream-ready: session persistence (save your creative state), the variety engine (automatic musical evolution), smooth transitions (graceful parameter changes), long-running stability (health monitoring + error recovery), and the massive cleanup that let us shed 24,000 lines of dead code.

## The Streaming Problem

Here's what can go wrong with a music generator running for hours:

1. **Lost state** — You spend 20 minutes crafting a beautiful ambient pad. You close the app. Poof. Gone. Start over.
2. **Musical repetition** — The generation engine plays the same motif forever. Your viewers fall asleep.
3. **Abrupt changes** — You switch from C major to F Dorian. The tempo jerks from 90 to 120 BPM. It sounds like a record skip.
4. **Polyphony disaster** — Notes don't turn off. Your synth fills with stuck notes. Audio clipping. Crash.
5. **Latency creep** — Background tasks pile up. MIDI timing drifts. Ticks get missed.

M1 and M2 were proof-of-concept. They worked for demos. But they weren't *production*.

M3 fixed all five problems.

## Part 1: Session Persistence — Save Your Sound

A session is: tempo, scale, root note, pattern, current notes playing, telemetry data.

When you stop generation, all that context dies. Next time you start, you get defaults.

M3 added **session persistence**:

```csharp
public interface ISessionManager
{
    Task SaveSessionAsync(string sessionName, GenerationState state);
    Task<GenerationState?> LoadSessionAsync(string sessionName);
    Task<List<string>> ListSessionsAsync();
    Task DeleteSessionAsync(string sessionName);
}
```

Sessions are stored at `~/.sqncr/sessions/`. Here's what gets saved:

```json
{
  "sessionName": "ambient-pad-d-minor",
  "tempo": 85,
  "scale": {
    "name": "Natural Minor",
    "root": 62,
    "intervals": [0, 2, 3, 5, 7, 8, 10]
  },
  "pattern": "ambient",
  "octave": 4,
  "midiChannel": 1,
  "timestamp": "2026-02-15T14:23:45Z",
  "metadata": {
    "streamContext": "chill-lofi",
    "duration": 1847,  // seconds
    "notesPlayed": 12847
  }
}
```

You stop generation. The `GenerationState` serializes. Session saved.

Later:

```csharp
using var activity = ActivitySource.StartActivity("mcp.load_session");

var session = await sessionManager.LoadSessionAsync("ambient-pad-d-minor");
if (session != null)
{
    engine.Commands.TryWrite(new GenerationCommand.SetTempo(session.Tempo));
    engine.Commands.TryWrite(new GenerationCommand.SetScale(session.Scale));
    engine.Commands.TryWrite(new GenerationCommand.SetPattern(session.Pattern));
    engine.Commands.TryWrite(new GenerationCommand.SetOctave(session.Octave));
    engine.Commands.TryWrite(new GenerationCommand.Start());
    
    return $"Resumed session: {session.SessionName}";
}
```

You resume. Same tempo, same scale, same vibe. No context loss.

### MCP Tools for Session Management

Three new MCP tools make this seamless:

- `save_session` — Save current generation state with a name
- `load_session` — Resume a saved session by name
- `list_sessions` — Show all saved sessions
- `delete_session` — Remove a session

Your AI assistant can now remember your music contexts:

```
AI: "I'll save this ambient pad to your session library"
→ save_session("ambient-pad-d-minor")
→ Session saved at ~/.sqncr/sessions/ambient-pad-d-minor.json

[Later...]

AI: "Let me resume that ambient pad you liked"
→ load_session("ambient-pad-d-minor")
→ Engine starts with the exact tempo, scale, and pattern
```

This is the *remembering* layer. Your music has memory.

## Part 2: The Variety Engine — Automatic Musical Evolution

Here's the insidious problem: even with a good generation algorithm, *repetition bores*.

The engine might loop the same melody shape. Same rhythm. Same everything. After 10 minutes, listeners tune out.

M3 shipped the **VarietyEngine** — an automatic system that evolves your music while you're live:

```csharp
public enum VarietyBehavior
{
    OctaveDrift,           // Melodies gradually shift octaves
    VelocityVariation,     // Notes get softer/louder over time
    RhythmicFills,         // Insert syncopated fills
    RestInsertion,         // Add strategic silences
    PatternDensity,        // Vary note density
    RegisterShift          // Alternate between low and high registers
}

public class VarietyEngine
{
    public VarietyBehavior SelectBehavior(VarietyLevel level)
    {
        var behaviors = level switch
        {
            VarietyLevel.Conservative => new[] { VarietyBehavior.RestInsertion },
            VarietyLevel.Moderate => new[] { 
                VarietyBehavior.OctaveDrift, 
                VarietyBehavior.VelocityVariation,
                VarietyBehavior.RestInsertion
            },
            VarietyLevel.Adventurous => new[] {
                VarietyBehavior.OctaveDrift,
                VarietyBehavior.VelocityVariation,
                VarietyBehavior.RhythmicFills,
                VarietyBehavior.RestInsertion,
                VarietyBehavior.PatternDensity,
                VarietyBehavior.RegisterShift
            },
            _ => throw new ArgumentException("Invalid variety level")
        };
        
        return behaviors[Random.Shared.Next(behaviors.Length)];
    }
}
```

Three variety levels:

**Conservative** — Only subtle changes. Safe for ambient.
```
Behavior: Rest Insertion
Effect: "Play a note every 8 beats instead of every 4. Keep the melody intact."
Use case: Ambient pad that needs consistency but not sameness
```

**Moderate** — Balanced evolution. Good for most contexts.
```
Behaviors: Octave Drift, Velocity Variation, Rest Insertion
Effect: "Melodies gradually move up/down octaves. Quieter notes alternate with louder. 
         Strategic silences create breathing room."
Use case: Lofi beats, chill hip-hop, anything that benefits from gentle variation
```

**Adventurous** — Full musical conversation.
```
Behaviors: All six
Effect: "Melodies shift octaves. Velocity swells and fades. Rhythmic fills inject energy.
         Rests create tension. Note density contracts and expands. High-register moves 
         alternate with low-register comping."
Use case: Techno, driving beats, anything that needs to *evolve*
```

### How Each Behavior Works

**OctaveDrift:**
```csharp
public void ApplyOctaveDrift(GenerationState state)
{
    // Every N measures, shift the octave up or down by 1
    if (_measureCount % 8 == 0)  // Every 8 measures
    {
        int drift = _random.NextDouble() < 0.5 ? -1 : 1;
        state.Octave = Math.Clamp(state.Octave + drift, 2, 7);
        
        using var activity = ActivitySource.StartActivity("variety.octave_drift");
        activity?.SetTag("variety.new_octave", state.Octave);
    }
}
```

Melodies float up and down. Not randomly. Every 8 measures. Listeners hear the journey.

**VelocityVariation:**
```csharp
public int ApplyVelocityVariation(int baseVelocity)
{
    // Sine wave LFO: oscillate velocity over time
    double phase = (_tickCount % 3840) / 3840.0 * 2 * Math.PI;  // 8 bars at 480 TPQ
    double variation = Math.Sin(phase);  // -1 to 1
    
    int velocity = (int)(baseVelocity * (1 + variation * 0.3));  // ±30%
    return Math.Clamp(velocity, 10, 127);
}
```

Velocity swells and fades on a slow sine curve. Music *breathes*.

**RhythmicFills:**
```csharp
public bool ShouldInsertFill(int currentBar)
{
    // Every 16 bars, inject a syncopated fill
    if (currentBar % 16 == 15 && _random.NextDouble() < 0.7)
    {
        return true;
    }
    return false;
}

public void InsertFill(GenerationState state)
{
    // Switch to 16th note triplets for one bar
    _fillDuration = 480;  // One bar
    state.SubdivisionOverride = Subdivision.Sixteenth;
    
    using var activity = ActivitySource.StartActivity("variety.rhythmic_fill");
    activity?.SetTag("variety.fill_start_bar", _currentBar);
}
```

Every 16 bars, a syncopated flurry of 16th notes. Energy injection.

**RestInsertion:**
```csharp
public void ApplyRestInsertion(VarietyLevel level)
{
    double restProbability = level switch
    {
        VarietyLevel.Conservative => 0.15,  // 15% chance of rest
        VarietyLevel.Moderate => 0.20,
        VarietyLevel.Adventurous => 0.25,
    };
    
    if (_random.NextDouble() < restProbability)
    {
        // Emit no NoteOn this cycle
        // Listeners hear silence, which makes subsequent notes feel heavier
    }
}
```

Strategic silence. Musical punctuation.

**PatternDensity:**
```csharp
public void ApplyPatternDensity(VarietyLevel level)
{
    // Vary how many notes play per bar
    int densityRange = level switch
    {
        VarietyLevel.Conservative => 0,     // Fixed
        VarietyLevel.Moderate => 2,         // ±2 notes per bar
        VarietyLevel.Adventurous => 4,      // ±4 notes per bar
    };
    
    if (densityRange > 0)
    {
        int variation = _random.Next(-densityRange, densityRange + 1);
        _notesPerBar = Math.Clamp(_baseNotesPerBar + variation, 2, 16);
    }
}
```

Sometimes sparse. Sometimes dense. Texture shifts.

**RegisterShift:**
```csharp
public int ApplyRegisterShift()
{
    // Alternate between low and high registers on phrase boundaries
    if (_phraseBoundary)
    {
        _currentRegister = _currentRegister == RegisterOption.Low 
            ? RegisterOption.High 
            : RegisterOption.Low;
        
        int octaveOffset = _currentRegister == RegisterOption.Low ? -2 : 2;
        return Math.Clamp(_baseOctave + octaveOffset, 2, 7);
    }
    return _baseOctave;
}
```

Alternates high/low register every phrase. Conversation between voices.

### Variety in Action

Imagine a lofi beat at 90 BPM. Conservative variety:

```
[Bar 1-4]  → Regular notes, every beat
[Bar 5-8]  → Same, but with a rest every 8 notes
[Bar 9-16] → Back to regular
[Bar 17+]  → Pattern repeats
```

Moderate:

```
[Bar 1-8]   → Regular, octave 4
[Bar 9-16]  → Octave 5 (drift up). Velocity swells (louder).
[Bar 17-24] → Back to octave 4. Velocity fades (softer).
[Bar 25+]   → Repeat
```

Adventurous:

```
[Bar 1-7]   → Regular beat
[Bar 8]     → FILL: 16th note triplets! Energy surge!
[Bar 9-15]  → Back to regular, but octave shifted up
[Bar 16]    → Strategic rest (silence)
[Bar 17-23] → Dense (8 notes per bar instead of 4)
[Bar 24]    → Sparse (2 notes per bar)
[Bar 25+]   → Register drops to low. Repeat.
```

The music evolves. Listeners stay engaged.

### Telemetry: Watching Variety Decisions

Each variety decision emits a span:

```csharp
using var activity = ActivitySource.StartActivity("variety.decision");
activity?.SetTag("variety.behavior", selectedBehavior.ToString());
activity?.SetTag("variety.level", level.ToString());
activity?.SetTag("variety.effect_magnitude", magnitude);
```

In the Aspire dashboard, you see:
- Which variety behaviors are active
- How often each one triggers
- What the actual musical effect was (octave change, velocity range, rest count)

**Streaming story:** Live on camera, you turn up variety to "Adventurous." Fills inject energy. Velocity swells. Register shifts. Your beat *evolves in real time*. Viewers watch the music transform.

## Part 3: Smooth Transitions — Gradual Parameter Changes

Here's a gotcha: what happens when you switch scales mid-generation?

Before M3? Immediate. Jarring.

```
[Tempo 120 BPM, C Major]
→ "Change to F Dorian, 90 BPM"
→ [Instantly 90 BPM, F Dorian]
```

Sounds like a record skip.

M3 shipped the **TransitionEngine**:

```csharp
public class TransitionEngine
{
    public void TransitionTempo(
        double currentTempo, 
        double targetTempo, 
        int transitionBars = 8)
    {
        // Over N bars, gradually move from current to target tempo
        // Linear interpolation per tick
        
        int transitionTicks = transitionBars * 480;
        int currentTick = 0;
        
        var timer = new Timer(_ =>
        {
            currentTick++;
            double progress = Math.Min(1.0, (double)currentTick / transitionTicks);
            double newTempo = currentTempo + (targetTempo - currentTempo) * progress;
            
            // Apply newTempo to engine
            GenerationEngine.Commands.TryWrite(new GenerationCommand.SetTempo(newTempo));
            
            if (progress >= 1.0)
            {
                timer.Dispose();  // Stop when done
            }
        }, null, TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(10));
    }
}
```

You request a transition to 90 BPM. Over 8 bars:

```
Bar 0:   120.0 BPM
Bar 1:   117.3 BPM
Bar 2:   114.5 BPM
Bar 3:   111.8 BPM
...
Bar 8:   90.0 BPM (arrived)
```

Smooth glide. Like a crossfader, but for tempo.

### Common-Tone Bridging for Scale Changes

Switching scales is trickier. You don't want *every* note to change.

M3 uses **common-tone bridging**:

```csharp
public class CommonToneBridge
{
    public int FindCommonTone(Scale currentScale, Scale targetScale, int currentNote)
    {
        var currentDegrees = currentScale.GetNotesInOctave(currentNote / 12);
        var targetDegrees = targetScale.GetNotesInOctave(currentNote / 12);
        
        // Find note that exists in both scales
        var common = currentDegrees.Intersect(targetDegrees).ToList();
        
        if (common.Any())
        {
            // Use the closest common note
            return common.OrderBy(n => Math.Abs(n - currentNote)).First();
        }
        
        // Fallback: use scale root
        return targetScale.GetRoot();
    }
}
```

Example: C Major → F Dorian

```
C Major:  [C, D, E, F, G, A, B]
F Dorian: [F, G, A, B♭, C, D, E]
Common:   [C, D, E, F, G, A]
```

If the melody is on E (in C Major), E also exists in F Dorian. No jump. Smooth connection.

### Transition Curves

Different parameter changes use different curves:

```csharp
public enum TransitionCurve
{
    Linear,      // Straight line
    EaseInOut,   // Slow start, fast middle, slow end (natural feel)
    Exponential, // Logarithmic (tempo feels natural at higher speeds)
}
```

**Linear** — Good for scale root changes, octave shifts.

**EaseInOut** — Good for velocity ranges, effect amounts.

**Exponential** — Good for tempo (musical perception of tempo is logarithmic).

## Part 4: Long-Running Stability — NoteTracker and HealthMonitor

Here's what happens if notes don't turn off: polyphony fills up. Your synth can't handle more notes. You get MIDI backup, timing slips, then crash.

M3 shipped two safety systems:

### 1. NoteTracker — 32-Note Polyphony Cap

```csharp
public class NoteTracker
{
    private readonly Dictionary<(int Channel, int Note), long> _activeNotes = new();
    private const int MaxPolyphony = 32;  // Safety limit
    
    public void NoteOn(int channel, int note, int velocity)
    {
        var key = (channel, note);
        
        // If we're at max polyphony, force-stop the oldest note
        if (_activeNotes.Count >= MaxPolyphony)
        {
            var oldest = _activeNotes.MinBy(kvp => kvp.Value);
            MidiOut.SendNoteOff(oldest.Key.Channel, oldest.Key.Note, 0);
            _activeNotes.Remove(oldest.Key);
            
            using var activity = ActivitySource.StartActivity("note_tracker.polyphony_limit_hit");
            activity?.SetTag("note_tracker.forced_release", oldest.Key.Note);
        }
        
        _activeNotes[key] = Environment.TickCount64;
    }
    
    public void NoteOff(int channel, int note, int velocity)
    {
        _activeNotes.Remove((channel, note));
    }
}
```

If notes aren't turning off (synth bug, MIDI overflow), NoteTracker force-kills the oldest note. Your synth never exceeds 32 voices. Sound quality degrades gracefully, but the system doesn't crash.

### 2. HealthMonitor — Rolling Latency, Missed Ticks

```csharp
public class HealthMonitor
{
    private readonly CircularBuffer<double> _latencies = new(1000);  // Last 1000 samples
    
    public HealthReport GetHealth()
    {
        double avgLatencyMs = _latencies.Average();
        double maxLatencyMs = _latencies.Max();
        int missedTicks = _missedTickCount;
        
        var status = (avgLatencyMs, maxLatencyMs, missedTicks) switch
        {
            (< 5, < 10, 0) => HealthStatus.Excellent,
            (< 10, < 20, < 5) => HealthStatus.Good,
            (< 20, < 50, < 20) => HealthStatus.Degraded,
            _ => HealthStatus.Critical,
        };
        
        return new HealthReport
        {
            Status = status,
            AverageLatencyMs = avgLatencyMs,
            MaxLatencyMs = maxLatencyMs,
            MissedTickCount = missedTicks,
            Timestamp = DateTime.UtcNow
        };
    }
}
```

On every tick:

```csharp
var tickStart = Stopwatch.GetTimestamp();
// ... run generation logic ...
var tickDuration = Stopwatch.GetTimestamp() - tickStart;

var latencyMs = tickDuration * 1000.0 / Stopwatch.Frequency;
_healthMonitor.RecordLatency(latencyMs);

// If this tick took too long, count it as missed
if (latencyMs > targetMs)
{
    _healthMonitor.RecordMissedTick();
}
```

The dashboard shows:
- Average latency (should be <5ms per tick)
- Max latency spikes
- Missed tick count (should be 0)

If health degrades:

```csharp
if (health.Status == HealthStatus.Degraded)
{
    // Automatically reduce complexity
    VarietyLevel = VarietyLevel.Conservative;  // No elaborate fills
    MidiOutput.AllNotesOff();  // Clear the synth
}

if (health.Status == HealthStatus.Critical)
{
    // Emergency stop
    GenerationEngine.Stop();
    MidiOutput.AllNotesOff();
}
```

**Streaming survival:** Your CPU gets overwhelmed by OBS encoding + stream chat. Latency spikes. HealthMonitor sees degradation. Variety backs off automatically. You stay live. No dropout.

### MCP Health Tool

```csharp
[McpServerTool(Name = "get_health")]
public static string GetHealth(HealthMonitor monitor)
{
    var health = monitor.GetHealth();
    return $"Status: {health.Status}\n" +
           $"Avg latency: {health.AverageLatencyMs:F2}ms\n" +
           $"Max latency: {health.MaxLatencyMs:F2}ms\n" +
           $"Missed ticks: {health.MissedTickCount}";
}
```

You can ask your AI: "How's the engine feeling?" Get real-time metrics.

## Part 5: Preset Scenes — Save Complete Configurations

Sessions save state. Scenes save *personality*.

A scene is: tempo, scale, pattern, variety level, and metadata.

M3 shipped three built-in scenes:

**ambient-pad:**
```json
{
  "name": "ambient-pad",
  "tempo": 70,
  "scale": "Pentatonic Minor",
  "root": "A2",
  "pattern": "ambient",
  "octave": 3,
  "varietyLevel": "Conservative",
  "metadata": {
    "description": "Sparse, floating pad. Perfect for focus work.",
    "tags": ["ambient", "meditation", "background"]
  }
}
```

**driving-techno:**
```json
{
  "name": "driving-techno",
  "tempo": 128,
  "scale": "Minor",
  "root": "D3",
  "pattern": "house",
  "octave": 4,
  "varietyLevel": "Moderate",
  "metadata": {
    "description": "Four-on-the-floor, driving bass. High energy.",
    "tags": ["techno", "dance", "energy"]
  }
}
```

**chill-lofi:**
```json
{
  "name": "chill-lofi",
  "tempo": 90,
  "scale": "Dorian",
  "root": "C3",
  "pattern": "lofi",
  "octave": 3,
  "varietyLevel": "Moderate",
  "metadata": {
    "description": "Hip-hop inspired. Swung rhythms, warm tones.",
    "tags": ["lofi", "hiphop", "chill"]
  }
}
```

MCP tools:

- `save_scene` — Save current config as a new scene
- `load_scene` — Activate a scene instantly
- `list_scenes` — Show all available scenes
- `delete_scene` — Remove a scene

**Streaming workflow:**

```
[Scene: ambient-pad - you're coding quietly]

[3 minutes later: chat says "Play something upbeat!"]
→ load_scene("driving-techno")
→ Tempo smoothly transitions to 128 BPM
→ Scale morphs to D minor via common-tone bridging
→ VarietyLevel jumps to Moderate (more fills, faster changes)
→ Music transforms. Energy surges.

[Chat is happy. Your stream metrics spike.]

[Later: "Go back to ambient"]
→ load_scene("ambient-pad")
→ Smooth transition back down
```

You never have to stop and reconfigure. Just load a scene. Music evolves. Stream flows.

## Part 6: Session Telemetry — Tracking Everything

M3 emits telemetry spans for every musical decision:

```csharp
using var activity = ActivitySource.StartActivity("generation.note_selection");
activity?.SetTag("generation.scale", state.Scale.Name);
activity?.SetTag("generation.octave", state.Octave);
activity?.SetTag("generation.selected_note", selectedNote);
activity?.SetTag("generation.velocity", velocity);
activity?.SetTag("variety.applied", varietyBehavior?.ToString() ?? "none");
```

In the Aspire dashboard, you see:

- **Root session span:** The entire generation session (when it started, when it ends)
- **Variety decision traces:** Which behavior was picked, at what time, with what effect
- **Health snapshots:** Latency, missed ticks, polyphony usage
- **Session metrics:** Total notes played, session duration, scale distribution

Four key metrics visible in Aspire:

1. `generation.notes_total` — Cumulative notes since session start
2. `generation.variety.decisions` — How many variety behaviors fired
3. `health.average_latency_ms` — Rolling latency average
4. `health.missed_ticks` — Tick failures

**Streaming benefit:** You can watch your music *generation in real time*. See which variety behaviors are active. Watch latency. Prove that nothing crashed.

## The Cleanup: Shedding Old Skin

M0, M1, M2 were experimental. We wrote lots of code. Some was wrong. Some was obsolete.

By M3, we had:

- **Old SequenceParser** — Deprecated by MCP tools
- **Old SequencePlayer** — Replaced by GenerationEngine
- **Old Specs architecture** — Prototype that never shipped
- **YamlDotNet dependency** — No longer needed
- **Auto-generated LLM markdown** — 24,000+ lines of bloat

We deleted it all.

**Before:**
- 300+ files
- SqncR.sln with 12 project references
- YamlDotNet, Newtonsoft.Json, speculative dependencies
- Markdown docs with outdated architecture diagrams

**After:**
- 140 files
- SqncR.slnx (simplified solution format)
- Only essential dependencies (DryWetMidi, System.Channels, OpenTelemetry)
- Clean repository root: just README, solution, src/, tests/, docs/

**Lines of code removed:**
- 24,000+ lines of dead/generated markdown
- 3,000+ lines of obsolete parser code
- 2,000+ lines of unused specs
- 500+ lines of dependency cruft

**Net effect:**
- Repository is 20% of its former size
- Build time: 8 seconds → 2.5 seconds
- Onboarding: vastly simpler
- Confidence: you're only maintaining what ships

The philosophy: *ship clean*. What's not in production doesn't belong in the repo.

## M3 By the Numbers

- **Tests:** 418 → 454 (36 new tests)
- **MCP Tools:** 13 → 22 (9 new tools)
- **Core Components:** 3 new (TransitionEngine, VarietyEngine, SessionTelemetry)
- **Variety Behaviors:** 6
- **Transition Curves:** 3
- **Built-in Scenes:** 3
- **Polyphony Limit:** 32 notes
- **Repository Size:** -74% (deleted 24K lines)
- **Build Time:** -69% (2.5 seconds)

## The Test Story: Canary Tests + Failure Recovery

We added 36 tests. Not unit tests. **Canary tests.**

Canary tests simulate hours of operation in seconds:

```csharp
[Test]
public async Task CanRun100KTicksWithoutCrashing()
{
    // Simulate 100,000 ticks (208 minutes at 120 BPM)
    var engine = new GenerationEngine(mockMidiOutput);
    await engine.StartAsync();
    
    for (int i = 0; i < 100_000; i++)
    {
        engine.ProcessTick();
        
        if (i % 10_000 == 0)
        {
            var health = healthMonitor.GetHealth();
            Assert.That(health.Status, Is.Not.EqualTo(HealthStatus.Critical));
        }
    }
    
    await engine.StopAsync();
    Assert.That(mockMidiOutput.AllNotesOff.Called);
}

[Test]
public async Task Handles10KNotePairsWithoutPolyphonyOverflow()
{
    // 10,000 rapid note pairs
    // Simulates a user hammering the keyboard or a fast arpeggio
    
    for (int i = 0; i < 10_000; i++)
    {
        await engine.SendNoteOnAsync(60 + (i % 12), 100);
        await engine.SendNoteOffAsync(60 + (i % 12), 100);
    }
    
    // NoteTracker should not exceed 32 active notes
    Assert.That(noteTracker.ActiveNoteCount, Is.LessThanOrEqualTo(32));
}
```

And **failure recovery tests** — deliberately breaking things and verifying graceful degradation:

```csharp
[Test]
public async Task RecovrsFromMidiSendFailure()
{
    mockMidiOutput.FailOnNextSend();
    
    // Should not crash; should log error
    engine.ProcessTick();
    
    Assert.That(errorLog.Contains("MIDI send failed"));
    Assert.That(engine.IsRunning);  // Still going
}

[Test]
public async Task GracefullyHandlesPolyphonyPressure()
{
    // Force 64 active notes (beyond 32-note limit)
    for (int i = 0; i < 64; i++)
    {
        noteTracker.NoteOn(0, i % 128, 100);
    }
    
    // Should have forced-released older notes
    Assert.That(noteTracker.ActiveNoteCount, Is.EqualTo(32));
    
    // Should have logged what happened
    Assert.That(activity.Tags.Contains("note_tracker.polyphony_limit_hit"));
}

[Test]
public async Task HealthMonitorDetectsDegradation()
{
    // Simulate high latency tick
    SimulateHighLatencyTick(50);  // 50ms, when target is 2ms
    
    var health = healthMonitor.GetHealth();
    Assert.That(health.Status, Is.EqualTo(HealthStatus.Degraded));
    Assert.That(health.MissedTickCount, Is.GreaterThan(0));
}
```

Seven canary tests. Seven failure recovery tests. All passing. Confidence that SqncR won't croak on a live stream.

## Why Stream-Ready Matters

Here's what changed:

**Before (M2):** You start generation, it works, you stop it. Works great for a 10-minute demo.

**After (M3):** You start generation at the beginning of your stream. It runs for 3 hours. Chat requests scene changes. You load them instantly. Music transitions smoothly. If latency spikes, it degrades gracefully. At the end, you save the session. Next week, you resume exactly where you left off.

That's *production* music software.

## What's Next — M4: Know Your Gear

M3 gave us stability. M4 will give us *flexibility*.

**Instrument abstraction** — Instead of hardcoded MIDI channels, describe what devices you have. "I have a Korg Volca Bass on channel 1, a Pocket Operator on channel 4, and Sonic Pi on localhost." SqncR routes notes appropriately.

**Multi-channel routing** — Different patterns on different devices. Bass line on the Volca. Pads on Sonic Pi. Drums on the Pocket Operator. Orchestration at the device level.

**Hardware integration** — Detect connected devices automatically. Version detection. Capability queries. "Does this synth support CC#51 (timbre)?" Route accordingly.

The generation engine will know your *gear*. Not just abstract MIDI. Your actual instruments.

## The Takeaway

SqncR went from proof-of-concept to stream-ready in three milestones.

M1: Can it *generate* music? ✓
M2: Can it *sound* good? ✓
M3: Can it *survive* live? ✓

The variety engine is musically sophisticated — six different evolution behaviors, three intensity levels, telemetry for every decision.

Session persistence is practical — save your vibe, resume it later.

Health monitoring is defensive — know when things are degrading, respond automatically.

And the cleanup was spiritual — we shed 24,000 lines of dead code. The repo is clean. The build is fast. The codebase is maintainable.

This is what shipping looks like: reliable, observable, intentional, and honest about what breaks.

---

**Want to follow along?** Clone SqncR, load a scene, and start a stream. Try the variety engine on different intensity levels. Save a session. The code is open source.

**Next post:** "Knowing Your Gear: Instrument Abstraction and Hardware Integration."
