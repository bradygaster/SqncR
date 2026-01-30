# SqncR Skills

What SqncR can do. Each skill is a discrete, composable capability.

---

## MVP Skills (P3)

These ship first:

| Skill | What it does |
|-------|--------------|
| `chord-progression` | Generate progressions from key/mode |
| `bass-line-generator` | Create bass lines from chords |
| `drone-generator` | Ambient drones in any key |
| `arpeggio-generator` | Arpeggiate chords |
| `rhythm-generator` | Basic rhythmic patterns |
| `list-devices` | Show MIDI hardware |

---

## Full Catalog

The complete skill catalog (45+) lives in [../SKILLS.md](../SKILLS.md).

Categories:
- **Musical Intelligence** - vibe-to-music, voice-leading
- **Device Control** - send-midi, configure-routing
- **Analysis** - analyze-song, detect-key, detect-tempo
- **Generation** - melody, bass, drums, polyrhythms
- **Transformation** - transpose, quantize, humanize
- **Session** - save, load, export

---

## Skill Interface

```csharp
public interface ISkill
{
    string Name { get; }
    string Description { get; }
    Task<SkillResult> ExecuteAsync(SkillInput input, CancellationToken ct);
}
```

Skills are:
- **Stateless** - No internal state between calls
- **Composable** - Output of one feeds input of another
- **Observable** - Traced via OpenTelemetry

---

## Example: Chord Progression

**Input:**
```json
{
  "key": "A",
  "mode": "minor",
  "bars": 4,
  "style": "dark"
}
```

**Output:**
```json
{
  "progression": [
    { "bar": 1, "symbol": "Am" },
    { "bar": 2, "symbol": "Dm" },
    { "bar": 3, "symbol": "F" },
    { "bar": 4, "symbol": "E" }
  ]
}
```

---

## Composition

Skills chain together:

```
chord-progression → bass-line-generator → sequence player
```

1. Generate progression in Am
2. Generate bass line from those chords
3. Play result to hardware

---

## See Also

- [../SKILLS.md](../SKILLS.md) - Complete catalog with examples
- [../sprints/P3-first-generation.md](../sprints/P3-first-generation.md) - MVP skill implementation
