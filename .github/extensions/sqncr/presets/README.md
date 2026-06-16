# SqncR Presets

Community-contributed patches for the SqncR modular synth canvas.

## Adding a Preset

1. Create a new `.json` file in this directory
2. Name it with a kebab-case slug (e.g. `my-cool-track.json`)
3. Fill in the metadata schema below
4. Open a PR!

## Preset Schema

```json
{
  "id": "my-preset-id",
  "name": "Human Readable Name",
  "description": "A brief description of the sound and vibe.",
  "author": "your-github-username",
  "tags": ["house", "dark", "minimal"],
  "genre": "Deep House",
  "mood": "dark | euphoric | calm | energetic | passionate | chill",
  "energy": "low | medium | high",
  "bpm": 128,
  "key": "A",
  "scale": "minor | major | harmonicMinor | melodicMinor | pentatonicMajor | pentatonicMinor | blues | dorian | phrygian | lydian | mixolydian",
  "style": "house | breakbeat | ambient | latin | euclidean",
  "bass_style": "pumping | walking | octave",
  "lead_style": "arpeggiated | scalar | call-response",
  "version": "1.0.0",
  "created": "2026-06-13"
}
```

## Metadata Fields

| Field | Required | Description |
|-------|----------|-------------|
| `id` | ✅ | Unique kebab-case identifier |
| `name` | ✅ | Display name |
| `description` | ✅ | What it sounds like, what inspired it |
| `author` | ✅ | Your GitHub username |
| `tags` | ✅ | Array of searchable keywords (genre, mood, artist refs, instruments) |
| `genre` | ✅ | Primary genre label |
| `mood` | ✅ | Emotional quality |
| `energy` | ✅ | Intensity level |
| `bpm` | ✅ | Tempo (20-300) |
| `key` | ✅ | Root note (C, C#, D, Eb, E, F, F#, G, Ab, A, Bb, B) |
| `scale` | ✅ | Scale/mode name |
| `style` | ✅ | Drum pattern style |
| `bass_style` | ✅ | Bass line algorithm |
| `lead_style` | ✅ | Lead melody algorithm |
| `version` | ✅ | Semver for the preset |
| `created` | ✅ | ISO date |

## Discovery

Presets are searchable by any metadata field. Agents and humans can use:

- `list_presets` — shows all presets with metadata
- `list_presets` with `filter: "euphoric"` — filters by mood, genre, tags, etc.
- `load_preset` with `query: "kaskade"` — fuzzy-matches against name, tags, description

## Tips for Good Presets

- **Tags are king** — add artist references, sub-genres, instruments, vibes
- **Description matters** — paint a picture of when/where you'd hear this
- **Be specific** — "2am warehouse techno" > "dance music"
- **Test it** — load your preset and make sure it actually sounds like what you described!
