# SqncR Sequence Format Specification

**Version:** 1.0  
**File Extension:** `.sqnc.yaml`  
**MIME Type:** `application/x-sqnc+yaml`

The `.sqnc.yaml` format is SqncR's native format for persisting musical sequences. It captures not just the notes, but the creative intent, randomization parameters, and song structure.

---

## Example Files

| File | Description | Demonstrates |
|------|-------------|--------------|
| `chill-ambient.sqnc.yaml` | Simple ambient piece | Basics: patterns, sections, randomization |
| `seven-nation-army.sqnc.yaml` | Classic rock riff | Advanced: automation, tempo changes, grooves, glide |
| `little-fluffy-clouds.sqnc.yaml` | Ambient house | Arpeggios, 4x4 kicks, probability-driven hats |
| `another-brick-in-the-wall.sqnc.yaml` | Iconic bass line | Drum fills, chord progressions, variations |

---

## File Structure

```yaml
meta:         # Song metadata (tempo, key, time signature)
intent:       # (optional) AI prompts that generated this
devices:      # MIDI devices and channel mappings
patterns:     # Reusable note/event patterns
automation:   # Continuous CC/pitch bend curves
grooves:      # Swing and timing templates
sections:     # Loopable song parts combining patterns
arrange:      # Timeline of sections (the song structure)
```

---

## Sections Reference

### `meta` - Song Metadata

```yaml
meta:
  title: "Song Title"
  artist: "Artist Name"
  tempo: 120                        # BPM
  key: Am                           # Root + mode (Am, C, F#m, Bb, etc.)
  time: { beats: 4, division: 4 }   # Time signature (4/4, 6/8, 7/8, etc.)
  swing: null                       # Swing template name or null
  tpq: 480                          # Ticks per quarter note (standard: 480)
  created: 2026-01-29T20:00:00Z     # ISO 8601 timestamp
```

**Note:** Time signature uses `{ beats, division }` instead of `4/4` to avoid YAML parsing issues.

### `intent` - Creative Intent (Optional)

Captures the AI prompts that generated the sequence:

```yaml
intent:
  - "ambient but rhythmic, 87bpm"
  - "add some pads"
  - "make it darker"
  - "build over 5 minutes"
```

### `devices` - MIDI Device Mappings

```yaml
devices:
  bass: { name: "Moog Sub 37", ch: 1 }
  pads: { name: "Polyend Synth", ch: 2 }
  lead: { name: "Polyend Synth", ch: 3 }
  drums: { name: "Polyend Play", ch: 10 }
```

---

## Patterns

Patterns are reusable event sequences. They are the building blocks of sections.

### Basic Pattern Structure

```yaml
patterns:
  pattern_name:
    length: 1920          # Length in ticks (1920 = 1 bar at 4/4)
    defaults:             # (optional) Default values for events
      vel: { range: [70, 90] }
      t_rand: { range: [-10, 10] }
    events:
      - { t: 0, type: note, note: C4, vel: 80, dur: 480 }
      - { t: 480, type: note, note: E4, vel: 75, dur: 480 }
```

### Event Types

#### Note Events
```yaml
- t: 0                    # Time offset in ticks
  type: note
  note: C4                # Note name (C4, F#2, Bb5, etc.)
  vel: 80                 # Velocity (0-127)
  dur: 480                # Duration in ticks
```

#### Note Events with Glide/Bend
```yaml
# Glide (portamento) to another note
- t: 0
  type: note
  note: G2
  vel: 100
  dur: 480
  glide: { to: E2, time: 240 }  # Glide to E2 over 240 ticks

# Pitch bend (in semitones)
- t: 0
  type: note
  note: D2
  vel: 95
  dur: 240
  bend: { range: [0, -2], time: 200 }  # Bend down 2 semitones over 200 ticks
```

#### CC (Control Change) Events
```yaml
- t: 0
  type: cc
  cc: 74                  # CC number (74 = filter cutoff on many synths)
  value: 127              # CC value (0-127)
```

#### Aftertouch Events
```yaml
# Channel aftertouch
- { t: 0, type: aftertouch, value: 100 }

# Polyphonic aftertouch (per-note)
- { t: 0, type: poly_aftertouch, note: E4, value: 127 }
```

---

## Randomization

Randomization is a first-class citizen. Apply it to any event property.

### Range (Continuous Random)

Pick a random value within a range:

```yaml
vel: { range: [60, 80] }           # Random velocity 60-80
dur: { range: [440, 520] }         # Random duration
t_rand: { range: [-20, 20] }       # Timing drift ±20 ticks
```

### Choice (Discrete Random)

Pick one value from a set:

```yaml
note: { choice: [C4, E4, G4] }     # Pick one note
pattern: { choice: [pat_a, pat_b] } # Pick one pattern
```

### Weighted Choice

Pick with weighted probabilities:

```yaml
pattern:
  choice: [main_riff, variation]
  weights: [3, 1]                  # 75% main, 25% variation
```

### Probability

Chance to play (0.0 - 1.0):

```yaml
- { t: 480, type: note, note: G4, vel: 80, dur: 240, prob: 0.7 }  # 70% chance
```

### Pattern-Level Defaults

Apply randomization to all events in a pattern:

```yaml
patterns:
  humanized_bass:
    length: 1920
    defaults:
      vel: { range: [85, 105] }
      t_rand: { range: [-15, 15] }
    events:
      - { t: 0, type: note, note: E2, dur: 480 }    # Inherits defaults
      - { t: 480, type: note, note: E2, dur: 480 }
```

---

## Automation

Continuous parameter changes over time (filter sweeps, volume swells, etc.).

```yaml
automation:
  filter_build:
    length: 15360           # 8 bars
    curves:
      - cc: 74              # Filter cutoff
        points:
          - { t: 0, value: 20 }
          - { t: 7680, value: 60 }
          - { t: 15360, value: 127 }
        shape: exponential  # linear, exponential, logarithmic, sine, s-curve

      - cc: 1               # Mod wheel
        points:
          - { t: 0, value: 0 }
          - { t: 15360, value: 100 }
        shape: linear

  vibrato_lfo:
    length: 1920            # 1 bar, loopable
    curves:
      - type: pitch_bend
        points:
          - { t: 0, value: 0 }
          - { t: 240, value: 0.5 }    # Semitones
          - { t: 480, value: 0 }
          - { t: 720, value: -0.5 }
          - { t: 960, value: 0 }
        shape: sine
```

---

## Grooves

Swing and timing templates for humanization.

```yaml
grooves:
  mpc60:
    grid: 16                # Applies to 16th notes
    offsets: [0, 0, 54, 0, 0, 0, 54, 0, 0, 0, 54, 0, 0, 0, 54, 0]  # Ticks

  lazy:
    grid: 8                 # Applies to 8th notes
    offsets: [0, 25, 0, 35, 0, 20, 0, 30]
    vel_scale: [1.0, 0.7, 0.9, 0.6, 1.0, 0.7, 0.85, 0.65]  # Velocity multipliers
```

---

## Sections

Sections combine patterns into loopable song parts.

```yaml
sections:
  verse:
    length: 30720           # 16 bars
    tempo: 124              # (optional) Tempo override
    time: { beats: 4, division: 4 }  # (optional) Time sig override
    loopable: true          # Can be looped in arrange
    tracks:
      - ch: 1               # MIDI channel
        groove: mpc60       # (optional) Apply groove template
        sequence:
          - { at: 0, pattern: bass_main }
          - { at: 3840, pattern: bass_main }
          - { at: 7680, pattern: bass_variation }
        automation:
          - { at: 0, curve: filter_build }

      - ch: 10
        sequence:
          - { at: 0, pattern: kick_4x4, repeat: 16 }
          - { at: 0, pattern: hats_disco, repeat: 16 }
```

### Section Tracks

Each track targets a MIDI channel and contains:
- `sequence`: List of patterns with timing
- `automation`: List of automation curves
- `groove`: Optional groove template

### Pattern Placement in Sections

```yaml
sequence:
  - { at: 0, pattern: main_riff }                    # Play once at tick 0
  - { at: 0, pattern: main_riff, repeat: 8 }        # Repeat 8 times
  - { at: 0, pattern: { choice: [a, b] }, repeat: 4 } # Random choice each repeat
  - { at: 0, pattern: main_riff, transpose: 12 }    # Transpose up octave
```

---

## Arrange

The `arrange` section is the song timeline - when each section plays.

```yaml
arrange:
  - { at: 0, section: intro }
  - { at: 15360, section: verse }
  - { at: 46080, section: chorus }
  - { at: 76800, section: verse }
  - { at: 107520, section: chorus, crossfade: 480 }  # Crossfade transition
  - { at: 138240, section: outro, tempo_ramp: { to: 100, over: 3840 } }  # Tempo change
```

### Arrange Modifiers

```yaml
crossfade: 480              # Crossfade from previous section (ticks)
tempo_ramp:                 # Gradual tempo change
  to: 100                   # Target tempo
  over: 3840                # Duration (ticks)
```

---

## Tick Reference

At `tpq: 480` (standard):

| Duration | Ticks |
|----------|-------|
| 64th note | 30 |
| 32nd note | 60 |
| 16th note | 120 |
| 8th note | 240 |
| Quarter note | 480 |
| Half note | 960 |
| Whole note | 1920 |
| 1 bar (4/4) | 1920 |
| 4 bars | 7680 |
| 8 bars | 15360 |
| 16 bars | 30720 |

---

## Complete Minimal Example

```yaml
meta:
  title: "Minimal Example"
  tempo: 120
  key: C
  time: { beats: 4, division: 4 }
  tpq: 480

devices:
  synth: { name: "Virtual MIDI", ch: 1 }

patterns:
  simple:
    length: 1920
    events:
      - { t: 0, type: note, note: C4, vel: 80, dur: 480 }
      - { t: 480, type: note, note: E4, vel: 80, dur: 480 }
      - { t: 960, type: note, note: G4, vel: 80, dur: 480 }
      - { t: 1440, type: note, note: C5, vel: 80, dur: 480 }

sections:
  main:
    length: 1920
    loopable: true
    tracks:
      - ch: 1
        sequence:
          - { at: 0, pattern: simple }

arrange:
  - { at: 0, section: main }
```

---

## Design Philosophy

1. **Persistence, not composition** - The format captures what SqncR generated, not a composition tool
2. **Randomization preserved** - Re-playing a sequence produces similar but not identical results
3. **Intent captured** - The `intent` section preserves the creative conversation
4. **YAML for tooling** - Standard format means validation, syntax highlighting, diffing work
5. **Pattern-centric** - Reusable building blocks, not a flat list of events
6. **Human-inspectable** - You can read it and understand what will play
