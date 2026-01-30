# .sqnc.yaml Format

SqncR's sequence file format. Human-readable, pattern-centric.

---

## Quick Example

```yaml
meta:
  title: "Simple Demo"
  tempo: 120
  key: C
  time: { beats: 4, division: 4 }

patterns:
  bass:
    length: 1920  # 1 bar at 480 tpq
    events:
      - { t: 0, type: note, note: C2, vel: 80, dur: 480 }
      - { t: 480, type: note, note: C2, vel: 75, dur: 480 }

sections:
  main:
    loopable: true
    tracks:
      - ch: 1
        sequence:
          - { at: 0, pattern: bass, repeat: 4 }

arrange:
  - { at: 0, section: main }
```

---

## Structure

```
meta:       # Title, tempo, key, time signature
intent:     # AI prompts that created this (optional)
devices:    # Device-to-channel mapping
patterns:   # Reusable event sequences
automation: # CC curves, pitch bend
grooves:    # Swing templates
sections:   # Loopable song parts
arrange:    # Timeline of sections
```

---

## Patterns

Building blocks of music. Contain events with timing.

```yaml
patterns:
  pattern_name:
    length: 1920          # Ticks
    defaults:             # Apply to all events
      vel: { range: [70, 90] }
    events:
      - { t: 0, type: note, note: C4, vel: 80, dur: 480 }
```

---

## Randomization

Built-in. Makes playback organic.

```yaml
# Random velocity between 60-80
vel: { range: [60, 80] }

# Pick one note randomly
note: { choice: [C4, E4, G4] }

# 70% chance to play
prob: 0.7

# Timing drift ±20 ticks
t_rand: { range: [-20, 20] }
```

---

## Tick Reference

At `tpq: 480` (default):

| Duration | Ticks |
|----------|-------|
| 16th note | 120 |
| 8th note | 240 |
| Quarter | 480 |
| Half | 960 |
| Whole | 1920 |
| 1 bar (4/4) | 1920 |

---

## Full Specification

See [../examples/README.md](../examples/README.md) for complete format documentation.
