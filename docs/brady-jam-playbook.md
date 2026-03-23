# Brady Jam Playbook

> Presentation script for the Brady Modules AI jam session demo.
> 4 AI performer agents improvise together via SqncR MCP tools.

## The Band

| Agent | Module | Role | MIDI Ch | Sound |
|-------|--------|------|:-------:|-------|
| Tremor | Drums | Drums Performer | 10 | 4-voice drum sequencer (kick, snare, hat, perc) |
| Tidegate | Euclidean | Euclidean Rhythm Performer | 2 | Melodic rhythm via E(K,N) gate patterns |
| Driftwave | Pad | Pad/Atmosphere Performer | 3 | Prophet-style synth pad with reverb + chorus |
| Rustle | Effects | Effects/Space Performer | 4 | FM synth with delay + reverb + distortion |

## Scene Preset

Load the built-in scene before starting:

```
load_scene("brady-jam")
```

Settings: **118 BPM**, A natural minor, octave 3, weighted generator, moderate variety, house drum pattern.

---

## Performance Sequence

### 1. Opening — Tremor Solo (0:00)

> "Start the jam. Drums only."

Tremor kicks off alone with the house pattern at 118 BPM.

```
# Tremor
start_generation(pattern: "house", tempo: 118, channel: 10)
```

**What's happening:** Simple four-on-the-floor kick, off-beat hats, snare on 2 and 4. Establishes the groove.

Let it breathe for 8–16 bars.

---

### 2. Build — Tidegate Enters (0:30)

> "Bring in the melody. Start sparse."

Tidegate joins with a tresillo pattern — 3 hits over 8 steps.

```
# Tidegate
add_instrument(role: "Melody", channel: 2)
start_generation(scale: "Natural Minor", root: "A3", variety: "conservative")
# Using E(3,8) — the tresillo
```

**What's happening:** Three notes per cycle, locked to the drum grid. Sparse but musical. The melody finds the pocket.

Let it develop for 8 bars, then:

> "Get busier."

```
# Tidegate
modify_generation(variety: "moderate")
# Shift to E(5,8) — the cinquillo
```

---

### 3. Texture — Driftwave Enters (1:15)

> "Add some atmosphere. Warm pads."

Driftwave layers in long, evolving chords.

```
# Driftwave
add_instrument(role: "Pad", channel: 3)
setup_software_synth(engine: "prophet", fx: ["reverb", "chorus"])
start_generation(scale: "Natural Minor", root: "A3", smooth: true)
```

**What's happening:** Slow attack pads (2s in, 4s release), LFO on filter cutoff, wide reverb tail. The harmonic bed that everything sits on.

---

### 4. Space — Rustle Enters (2:00)

> "Add some space. Subtle delays."

Rustle adds spatial depth with synced delay taps.

```
# Rustle
add_instrument(role: "Lead", channel: 4)
setup_software_synth(engine: "fm", fx: ["delay", "reverb", "distortion"])
modify_generation(variety: "conservative")
# 2 taps, low scatter, tempo-synced
```

**What's happening:** Two delay taps at 1/8 note intervals, low feedback, narrow stereo. Ghostly echoes of the other voices. Barely there — but you'd notice if they stopped.

---

### 5. ⚡ "Make It Darker" — All 4 Respond (3:00)

> "Make it darker."

**This is the showcase moment.** All four agents respond simultaneously, each interpreting "darker" through their own lens:

```
# Tremor (responds: energy shift)
modify_generation(pattern: "half-time", variety: "conservative")
# → Half-time feel, less density, more space

# Tidegate (responds: harmonic shift)
modify_generation(scale: "Phrygian", variety: "conservative")
# → Fewer hits, darker mode — E(3,8) with phrygian intervals

# Driftwave (responds FIRST: harmonic foundation)
modify_generation(scale: "Harmonic Minor", smooth: true)
# → Lower filter cutoff, more reverb, harmonic minor darkness

# Rustle (responds: spatial shift)
modify_generation(variety: "moderate")
# → Increased scatter, longer feedback tails, wider spread
# → Dark analog tone, more wash
```

**What's happening:** The entire feel shifts. Drums go half-time, melody gets sparse and minor, pads darken the harmony, delays blur into reverb wash. Same 4 voices, completely different mood. One phrase, four interpretations.

Hold the dark section for 16 bars.

---

### 6. Climax — Full Energy (4:00)

> "Build it back up. Let's go."

All agents escalate together:

```
# Tremor
modify_generation(pattern: "house", variety: "adventurous")
# → Full pattern, fills, building energy

# Tidegate
modify_generation(scale: "Natural Minor", variety: "adventurous")
# → E(7,16) — dense, driving West African bell pattern

# Driftwave
modify_generation(scale: "Natural Minor", variety: "moderate")
# → Open filter, brighter pads, more movement

# Rustle
modify_generation(variety: "adventurous")
# → 4-6 taps, max spread, max scatter — huge space
```

**What's happening:** Maximum energy. Dense rhythms, bright pads, cascading delays. The full band at peak intensity.

---

### 7. Stop Sequence (5:00)

> "Stop."

Agents stop in reverse order — Tremor first, Rustle last:

```
# Tremor (stops FIRST)
stop_generation()

# Tidegate (stops second)
stop_generation()

# Driftwave (stops third — pads ring out)
stop_generation()

# Rustle (stops LAST — fades feedback to 0 over 4 bars)
# The echo of the last note fades into silence
stop_generation()
```

**What's happening:** The music deconstructs in layers. Drums drop, melody stops, pads fade, and the last thing you hear is Rustle's delay tail ringing out into silence.

---

## Technical Notes

### MIDI Channel Routing

| Channel | Agent | Voice Type |
|:-------:|-------|------------|
| 2 | Tidegate | Melodic rhythm (euclidean gates) |
| 3 | Driftwave | Pad/atmosphere (long chords) |
| 4 | Rustle | Lead/effects (FM + delay) |
| 10 | Tremor | Drums (General MIDI drum channel) |

### Key Parameters

- **Tempo:** 118 BPM (set by Tremor, all others sync)
- **Key:** A natural minor (A3 root, octave 3)
- **Scale progression:** Natural Minor → Phrygian (darker) → Harmonic Minor → Natural Minor (return)
- **Drum pattern:** House → Half-time (darker) → House (climax)

### Agent Response Hierarchy

When a direction like "make it darker" is given:
1. **Driftwave** responds FIRST — shifts harmonic foundation
2. **Tremor** follows — adjusts energy and density
3. **Tidegate** follows — adapts rhythmic density and scale
4. **Rustle** responds last — adjusts spatial character

### Pre-Flight Checklist

- [ ] SqncR MCP server running
- [ ] MIDI routing configured (loopMIDI or IAC)
- [ ] Software synths loaded on channels 2, 3, 4, 10
- [ ] `load_scene("brady-jam")` executed
- [ ] Audio monitoring active
- [ ] All 4 performer agents spawned
