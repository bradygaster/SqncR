### Music Theory Foundations — SqncR.Theory library design
**By:** Simon (Music Theorist)
**Date:** Issue #8
**Status:** Implemented

**What:** Created `SqncR.Theory` as a pure .NET 9 class library containing the foundational music theory types for SqncR:

1. **Interval.cs** — Named constants for all 13 intervals (Unison through Octave) with `GetName()` lookup. Used as building blocks for scale and chord construction.

2. **Scale.cs** — Sealed record with `RootNote`, `Name`, `Intervals`. Factory methods for 10 scale types: Major, Natural Minor, Harmonic Minor, Melodic Minor, Pentatonic Major, Pentatonic Minor, Blues, Whole Tone, Diminished (half-whole), Chromatic. Query methods: `ContainsNote(midiNote)` uses pitch-class math (mod 12), `GetNearestScaleNote(midiNote)` snaps to closest scale tone (rounds down on ties), `GetNotesInOctave(octave)` returns MIDI numbers within a given octave.

3. **Mode.cs** — All 7 modes of the major scale (Ionian–Locrian) implemented as interval rotations. `FromMajorScale(root, index)` plus named convenience methods.

4. **ScaleLibrary.cs** — Case-insensitive registry with 19 entries (10 scales + 7 modes + 2 aliases). `Get(name, root)`, `Exists(name)`, `AvailableScales`.

**Design decisions:**
- All types are immutable (records, readonly). Music theory data is value data.
- Pure computation — no side effects, no MIDI, no I/O.
- Pitch-class arithmetic is mod-12, consistent with NoteParser's C4=60 convention.
- `GetNearestScaleNote` rounds down on equidistant ties — a musical choice that favors resolution downward (common in voice leading).
- Scale intervals are stored as semitone offsets from root (0-based, all < 12). This makes mode rotation a simple array shift.
- SqncR.Core references SqncR.Theory (not the reverse) so the generation engine can use theory types.

**Why:** These types are the foundation for all music generation. Scales constrain which notes the generator can choose. Modes provide tonal color. The query API (`ContainsNote`, `GetNearestScaleNote`) will be used by the generation loop to ensure output stays musically coherent. The `ScaleLibrary` enables natural-language requests like "play in Dorian" via MCP tools.
