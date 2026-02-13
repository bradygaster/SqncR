---
title: "Building SqncR: From Zero to Generative Music Engine"
date: 2026-02-13
tags: [sqncr, generative-music, dotnet, mcp, midi, aspire]
summary: "How we built a generative music engine in .NET that your AI coding assistant can talk to — from proof of life to 305 passing tests."
---

# Building SqncR: From Zero to Generative Music Engine

You have a keyboard in the corner. A synth. Maybe a drum machine. You're at your desk coding, and you think: *"I wish I could just tell an AI to play some ambient music in A minor while I work."* Not interrupting. Just... happening. Quietly, behind the scenes, like a musician jamming while you type.

That's the origin story of **SqncR**. Not a flashy idea. Just a practical one.

Today, we're shipping Milestone 1 — **The Engine Room** — with 305 tests passing, a full generation engine, three melody generators, a complete music theory foundation, and an MCP server living right in your coding environment. This post walks you through the architecture, the math, and the actual code we built to make generative music *just work*.

## The Vision: Your AI Assistant Composes Music

Brady (the project lead) is a developer with MIDI instruments sitting around. He wanted a system that:

1. Lives inside your IDE — as an MCP (Model Context Protocol) server
2. Lets your AI coding assistant control it through natural language
3. Generates music continuously on a background service
4. Works with *any* MIDI device — keyboard, synth, drum machine, hardware, software

The key insight: **Your coding assistant already exists.** Why not let it command your instruments?

That's where MCP comes in. If your AI knows how to call tools, why not make music generation one of those tools? "Play some upbeat funk in G. Use the house pattern. 120 BPM."

Done. The engine runs it while you code.

## The Stack: Why .NET 9 + Aspire + MCP

We chose:

- **.NET 9** — Modern, fast, great MIDI libraries (DryWetMidi)
- **Aspire orchestrator** — Manages services, logging, observability
- **OpenTelemetry** — Every note emits a span. Every tick is traceable.
- **MCP** — The protocol between your AI and the music system
- **System.Channels** — Lock-free command queue between MCP tools and the engine

Why this stack? Because SqncR lives where your IDE lives. It's not a separate DAW or web service. It's part of your development environment. Aspire lets us spin up multiple services (MIDI device manager, generation engine, MCP server) in one process. OpenTelemetry means you can watch the whole thing breathe in the Aspire dashboard — *real-time visibility into note generation, timing accuracy, scale selection.*

## M0: Proof of Life — The Skeleton That Breathes

Milestone 0 established the bones:

- **Aspire orchestrator** — Service bootstrapping, health checks
- **OpenTelemetry instrumentation** — ActivitySource for tracing
- **Baseline test infrastructure** — 85 deterministic tests

This wasn't feature work. It was *scaffolding*. Setting up the observability pipeline so we could see what happens inside the engine. Making sure the plumbing was solid before we started composing music.

The lesson: *invest in visibility early.* Every part of SqncR has a corresponding activity span.

## M1: The Engine Room — Where the Music Happens

M1 is where it gets interesting. We built:

### 1. MCP Server with 7 Tools

Your AI assistant talks to SqncR via these endpoints:

```csharp
[McpServerTool(Name = "start_generation"), Description("Starts music generation with specified parameters")]
public static string StartGeneration(
    GenerationEngine engine,
    GenerationState state,
    double tempo = 120,
    string scale = "Pentatonic Minor",
    string rootNote = "C4",
    string pattern = "rock",
    int octave = 4)
{
    using var activity = ActivitySource.StartActivity("mcp.start_generation");
    activity?.SetTag("mcp.tool", "start_generation");

    try
    {
        var midiRoot = NoteParser.Parse(rootNote);
        var scaleObj = ScaleLibrary.Get(scale, midiRoot);
        var patternObj = PatternLibrary.Get(pattern);

        engine.Commands.TryWrite(new GenerationCommand.SetTempo(tempo));
        engine.Commands.TryWrite(new GenerationCommand.SetScale(scaleObj));
        engine.Commands.TryWrite(new GenerationCommand.SetPattern(patternObj));
        engine.Commands.TryWrite(new GenerationCommand.SetOctave(octave));
        engine.Commands.TryWrite(new GenerationCommand.Start());

        return $"Started generation: {tempo} BPM, {scaleObj.Name} (root {rootNote}), {pattern} pattern, octave {octave}";
    }
    catch (ArgumentException ex)
    {
        return $"Error: {ex.Message}";
    }
}
```

This tool tells the generation engine to start playing. It accepts parameters, validates them against our scale and pattern libraries, writes commands to a lock-free channel, and returns status.

There are six others: `ping`, `list_devices`, `open_device`, `modify_generation`, `stop_generation`, `get_status`.

### 2. The Generation Engine — 480 TPQ Timing Accuracy

The heart of SqncR is a BackgroundService running at **480 ticks per quarter note**. That's professional DAW precision.

Here's why 480? Because MIDI Standard is 480 PPQ (parts per quarter note). One quarter note = 480 ticks. This lets us represent triplets, sixteenth notes, swing, and everything in between with integer precision.

The timing loop is deceptively tricky:

```csharp
// Calculate microseconds per tick from BPM
// BPM = quarter notes per minute
// microseconds per quarter note = 60_000_000 / BPM
// microseconds per tick = microseconds per quarter note / PPQ
double usPerTick = 60_000_000.0 / (_state.Tempo * Ppq);

// Wait for the next tick using Stopwatch + spin-wait
double targetUs = (currentTick + 1) * usPerTick;
double elapsedUs = stopwatch.Elapsed.TotalMicroseconds;

if (elapsedUs < targetUs)
{
    double remainingMs = (targetUs - elapsedUs) / 1000.0;
    if (remainingMs > 2)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(remainingMs - 1), stoppingToken)
            .ConfigureAwait(false);
    }
    // Spin-wait the remaining microseconds for sub-millisecond precision
}
```

Notice the strategy:

1. **Calculate** the exact microsecond target for the next tick
2. **Sleep** until we're 1ms away (Task.Delay is too coarse for microsecond precision)
3. **Spin-wait** the final microseconds (busy-loop — acceptable because it's very short)

This hybrid approach gives us timing accuracy within tens of microseconds while avoiding the overhead of Thread.Sleep. No external timing libraries. No magic. Just math and patience.

### 3. Three Melody Generators

We built three different note generators, each with a different personality:

**ScaleWalk** — The boring one. Just walks up and down the scale.

**WeightedNote** — The musical one. Uses probabilistic selection with musical gravity:

```csharp
public int? NextNote(GenerationState state)
{
    // Rest gate: 15% chance of silence
    if (_random.NextDouble() < _restProbability)
        return null;

    var notes = state.Scale.GetNotesInOctave(state.Octave);
    var weights = new double[notes.Count];

    for (int i = 0; i < notes.Count; i++)
    {
        int degreeIndex = i;

        // Base weight by scale degree
        if (degreeIndex == 0)
            weights[i] = 3;  // Root — gravitational center
        else if (degreeIndex == 4 && notes.Count >= 7)
            weights[i] = 2;  // Fifth — strong attractor
        else
            weights[i] = 1;

        // Stepwise motion bonus: prefer adjacent notes
        if (_lastNote >= 0)
        {
            int semitoneDistance = Math.Abs(notes[i] - _lastNote);
            if (semitoneDistance <= 2)
                weights[i] += 1;  // Adjacent = smooth
            
            // Penalize large leaps
            if (semitoneDistance > 5)
                weights[i] *= 0.1;  // Only 10% chance
        }
    }

    // Weighted random selection
    double totalWeight = 0;
    for (int i = 0; i < weights.Length; i++)
        totalWeight += weights[i];

    double roll = _random.NextDouble() * totalWeight;
    double cumulative = 0;

    for (int i = 0; i < weights.Length; i++)
    {
        cumulative += weights[i];
        if (roll <= cumulative)
        {
            int selectedNote = notes[i];
            _lastNote = selectedNote;
            return Math.Clamp(selectedNote, 0, 127);
        }
    }

    return null;
}
```

Notice the design: every note is weighted. Root and fifth have gravitational pull. Adjacent notes get bonuses (stepwise motion is beautiful). Large leaps are penalized but not impossible.

This isn't random noise. It's *musically biased randomness*. The notes feel intentional.

**Arpeggio** — The mathematical one. Cycles through chord tones: root, third, fifth, octave. Predictable, harmonic, perfect for accompaniment.

### 4. Music Theory Foundation — 19 Scales, 10 Scale Types

We built a ScaleLibrary with 19 pre-loaded scales:

```
Pentatonic Minor    Major              Minor (Natural)
Pentatonic Major    Dorian             Phrygian
Lydian              Mixolydian         Harmonic Minor
Melodic Minor       Blues              Chromatic
Ionian              Aeolian            Locrian
...and more
```

Each scale is a collection of semitone intervals. A C minor pentatonic scale has intervals `[0, 3, 5, 8, 10]` from the root. Parse "C4" as MIDI note 60. Scale degrees become actual MIDI notes: 60, 63, 65, 68, 70. Transpose to any root. Query by octave.

This is the music theory glue. Without it, the generation engine would be numerology. With it, every note *belongs* to a harmonic context.

### 5. Rhythm Engine — 5 Drum Patterns + Swing

The PatternLibrary encodes five drum grooves:

```csharp
private static LayeredPattern BuildRock()
{
    // Classic rock: kick on 1 & 3, snare on 2 & 4, closed hat on 8ths
    var kick = FromHits(16, [0, 8], 110, "rock-kick");
    var snare = FromHits(16, [4, 12], 110, "rock-snare");
    var hat = FromHits(16, [0, 2, 4, 6, 8, 10, 12, 14], 80, "rock-hat");

    return new LayeredPattern("rock", [
        (DrumVoice.Kick, kick),
        (DrumVoice.Snare, snare),
        (DrumVoice.ClosedHiHat, hat)
    ]);
}
```

Each pattern is a **LayeredPattern** — multiple drum voices (kick, snare, hi-hat) working in concert. Each voice is a **BeatPattern** — a 16-step grid with hits and rests. Each step is a **StepInfo** — velocity and probability.

Rock: kick on downbeats, snare on the backbeats, hi-hat on every 8th note.
House: four-on-the-floor kick, off-beat open hats.
Jazz: ride cymbal with swing spacing, ghost notes on the kick.
Ambient: probabilistic, sparse.

The step sequencer runs at 480 TPQ, so one step = 30 ticks (480 / 16). Hit timing is *exact*.

### 6. MIDI Test Framework — Deterministic, Observable

We needed to test music without real hardware. Enter **IMidiOutput**:

```csharp
public interface IMidiOutput
{
    Task SendNoteOn(int channel, int note, int velocity);
    Task SendNoteOff(int channel, int note, int velocity);
    Task SendControlChange(int channel, int controller, int value);
}
```

And **MockMidiOutput**:

```csharp
public class MockMidiOutput : IMidiOutput
{
    public List<MidiEvent> Events { get; } = new();

    public Task SendNoteOn(int channel, int note, int velocity)
    {
        Events.Add(new MidiEvent { Type = MidiEventType.NoteOn, Channel = channel, Note = note, Velocity = velocity });
        return Task.CompletedTask;
    }
    // ... etc
}
```

Every test can write assertions about *which notes played, when, with what velocity*. We have 12 deterministic generation tests that check melody shape, scale adherence, rhythm accuracy, and MIDI output correctness.

## The Numbers: 305 Tests, 0 Warnings, 480 TPQ Precision

- **305 unit and integration tests** — All passing
- **0 compiler warnings** — Clean code
- **480 ticks per quarter note** — Professional timing
- **19 pre-loaded scales** — Music theory coverage
- **5 drum patterns** — Rhythm foundation
- **3 melody generators** — Algorithmic variety
- **7 MCP tools** — Full control surface

## What Happens Next — M2: The Sounding Forge

M1 is the engine. M2 is the sound.

**Sonic Pi Integration** — We'll generate Ruby code that Sonic Pi executes. Your AI says "play a pad arpeggio in F Dorian." SqncR generates the Sonic Pi code. It runs. Synth sound emerges.

**VCV Rack Integration** — Programmatic patch generation. Your AI builds a modular synth patch on the fly.

**Spectral Analysis** — Analyze incoming audio. Feed it back to the generation engine. "This part needs more energy. Add reverb. Make the bass deeper."

The generation engine can *think* about music. Soon it'll actually *make* sound.

## The Developer Experience

From the outside, it's simple:

```
AI: "Play some ambient music in A minor"
↓
MCP: start_generation(scale="Pentatonic Minor", rootNote="A3", pattern="ambient", tempo=90)
↓
GenerationEngine: Start the generation loop, set commands
↓
MIDI Out: Every 480th of a quarter note, a note or drum hit
↓
Your speakers: Music.
```

From the inside, it's music theory + distributed systems + real-time precision. But that complexity is *contained*. The MCP interface is simple. The generation engine is deterministic and observable. Every part has tests.

This is what good systems design looks like: simple on top, sophisticated underneath.

## The Takeaway

SqncR isn't trying to be a DAW. It's trying to be a *companion* — an AI-driven music companion that lives in your coding environment and makes music while you work.

We built it in .NET because we needed:
- Professional MIDI timing (480 PPQ, microsecond precision)
- Observability (OpenTelemetry, Aspire)
- Scale (generation loop, background service)
- Testability (IMidiOutput mocking, 305 tests)

And we built it *right* — music theory first, code second. Every note that plays is musically valid. Every rhythm is precisely timed.

Next: sound.

---

**Want to follow along?** SqncR is open source. Check the GitHub repo for the full codebase, run the tests locally, or contribute a new rhythm pattern.

**Next post:** "Sonic Pi Meets MIDI — Bridging Generative Music and Real Synths."
