# Music Theory in SqncR

SqncR understands music, not just MIDI numbers.

---

## Core Types

### Note
A pitch with MIDI number, name, octave.

```csharp
var note = new Note("C4");  // MIDI 60
var freq = note.Frequency(); // 261.63 Hz
var higher = note.Transpose(7); // G4
```

### Scale
A key with mode, generates all notes.

```csharp
var scale = new Scale(new Note("A4"), ScaleMode.Minor);
// Notes: A, B, C, D, E, F, G
```

Supported modes:
- Major, Minor (Aeolian)
- Dorian, Phrygian, Lydian, Mixolydian, Locrian
- Harmonic Minor, Melodic Minor
- Pentatonic Major/Minor, Blues
- Whole Tone, Chromatic

### Chord
Root + quality, generates notes.

```csharp
var chord = Chord.Parse("Am7");
// Notes: A, C, E, G
```

Supported qualities:
- Major, Minor, Diminished, Augmented
- Sus2, Sus4
- Major7, Minor7, Dominant7
- All 9th, 11th, 13th extensions

### Interval
Distance between notes.

```csharp
var interval = Interval.Between(C4, G4);
// "Perfect Fifth", 7 semitones
```

---

## Why This Matters

Instead of:
```
"send MIDI note 60"
```

You say:
```
"play a C minor chord"
```

SqncR translates musical intent to MIDI.

---

## Music Theory Decisions

**Modes over scales:** A mode is a scale starting from a different degree. Dorian isn't just "D to D on white keys" - it's a specific set of intervals that creates a mood.

**Chords have function:** Am7 in the key of C is the vi chord, not just "notes A C E G."

**Voice leading matters:** Moving from Cmaj7 to Fmaj7, the C stays (common tone), other voices move minimally.

---

## See Also

- [../MUSIC_THEORY.md](../MUSIC_THEORY.md) - Full theory documentation
- [../sprints/P2-music-theory.md](../sprints/P2-music-theory.md) - Implementation sprint
