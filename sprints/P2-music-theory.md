# P2: Music Theory Foundation

**Priority:** 2
**Depends on:** P1 (Save Sessions)
**Goal:** Strongly-typed music theory primitives that unlock generation
**Duration:** ~1-2 weeks

---

## The Wow Moment

You can now say "give me a C minor scale" and the system knows what notes that is. This unlocks everything that follows.

```csharp
var scale = new Scale("C", ScaleMode.Minor);
// scale.Notes → [C, D, Eb, F, G, Ab, Bb]

var chord = new Chord("Am7");
// chord.Notes → [A, C, E, G]
```

---

## What We're Building

1. **Note value type** - MIDI number ↔ note name, octave, frequency
2. **Interval type** - Semitones, quality (major third, perfect fifth)
3. **Scale type** - All modes, note generation, scale degrees
4. **Chord type** - Triads, 7ths, extensions, voicings
5. **Progression type** - Chord sequences with timing
6. **Unit tests** - Music theory must be correct

## What We're NOT Building Yet

- Skills framework
- Agents
- MCP/API/SDK
- Generation algorithms (just the types)

---

## Tasks

### Task 1: Create Theory Project

```bash
cd src
dotnet new classlib -n SqncR.Theory -f net9.0
cd ..
dotnet sln add src/SqncR.Theory

cd src/SqncR.Core
dotnet add reference ../SqncR.Theory

cd ../SqncR.Midi
dotnet add reference ../SqncR.Theory
```

---

### Task 2: Note Value Type

**File:** `src/SqncR.Theory/Note.cs`

```csharp
namespace SqncR.Theory;

/// <summary>
/// Represents a musical note as a MIDI number (0-127).
/// Immutable value type for performance and safety.
/// </summary>
public readonly record struct Note : IComparable<Note>
{
    public int MidiNumber { get; }

    public Note(int midiNumber)
    {
        if (midiNumber < 0 || midiNumber > 127)
            throw new ArgumentOutOfRangeException(nameof(midiNumber), "Must be 0-127");
        MidiNumber = midiNumber;
    }

    public Note(string noteName) : this(ParseNoteName(noteName)) { }

    // C4 = MIDI 60
    public string Name => NoteNames[PitchClass] + Octave;
    public int Octave => (MidiNumber / 12) - 1;
    public int PitchClass => MidiNumber % 12;

    public double Frequency(double a4Tuning = 440.0)
        => a4Tuning * Math.Pow(2, (MidiNumber - 69) / 12.0);

    public Note Transpose(int semitones)
        => new(Math.Clamp(MidiNumber + semitones, 0, 127));

    public Interval IntervalTo(Note other)
        => new(other.MidiNumber - MidiNumber);

    public int CompareTo(Note other) => MidiNumber.CompareTo(other.MidiNumber);

    public override string ToString() => Name;

    // Parsing
    private static readonly string[] NoteNames =
        ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];

    private static readonly Dictionary<string, int> NoteOffsets = new()
    {
        ["C"] = 0, ["C#"] = 1, ["Db"] = 1, ["D"] = 2, ["D#"] = 3, ["Eb"] = 3,
        ["E"] = 4, ["Fb"] = 4, ["E#"] = 5, ["F"] = 5, ["F#"] = 6, ["Gb"] = 6,
        ["G"] = 7, ["G#"] = 8, ["Ab"] = 8, ["A"] = 9, ["A#"] = 10, ["Bb"] = 10,
        ["B"] = 11, ["Cb"] = 11, ["B#"] = 0
    };

    private static int ParseNoteName(string name)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            name.Trim(), @"^([A-Ga-g][#b]?)(-?\d+)$");

        if (!match.Success)
            throw new ArgumentException($"Invalid note name: {name}");

        var notePart = match.Groups[1].Value;
        notePart = char.ToUpper(notePart[0]) + notePart[1..];

        if (!NoteOffsets.TryGetValue(notePart, out var offset))
            throw new ArgumentException($"Unknown note: {notePart}");

        var octave = int.Parse(match.Groups[2].Value);
        return offset + ((octave + 1) * 12);
    }

    // Implicit conversions
    public static implicit operator int(Note n) => n.MidiNumber;
    public static implicit operator Note(int midi) => new(midi);
}
```

---

### Task 3: Interval Value Type

**File:** `src/SqncR.Theory/Interval.cs`

```csharp
namespace SqncR.Theory;

public readonly record struct Interval(int Semitones)
{
    public string Name => Semitones switch
    {
        0 => "Unison",
        1 => "Minor 2nd",
        2 => "Major 2nd",
        3 => "Minor 3rd",
        4 => "Major 3rd",
        5 => "Perfect 4th",
        6 => "Tritone",
        7 => "Perfect 5th",
        8 => "Minor 6th",
        9 => "Major 6th",
        10 => "Minor 7th",
        11 => "Major 7th",
        12 => "Octave",
        _ => $"{Semitones} semitones"
    };

    public string ShortName => Semitones switch
    {
        0 => "P1", 1 => "m2", 2 => "M2", 3 => "m3", 4 => "M3", 5 => "P4",
        6 => "TT", 7 => "P5", 8 => "m6", 9 => "M6", 10 => "m7", 11 => "M7",
        12 => "P8", _ => Semitones.ToString()
    };

    public IntervalQuality Quality => Semitones switch
    {
        0 or 5 or 7 or 12 => IntervalQuality.Perfect,
        2 or 4 or 9 or 11 => IntervalQuality.Major,
        1 or 3 or 8 or 10 => IntervalQuality.Minor,
        6 => IntervalQuality.Augmented, // or Diminished
        _ => IntervalQuality.Other
    };

    public static Interval Between(Note a, Note b) => new(Math.Abs(b.MidiNumber - a.MidiNumber));

    // Common intervals
    public static Interval Unison => new(0);
    public static Interval MinorSecond => new(1);
    public static Interval MajorSecond => new(2);
    public static Interval MinorThird => new(3);
    public static Interval MajorThird => new(4);
    public static Interval PerfectFourth => new(5);
    public static Interval Tritone => new(6);
    public static Interval PerfectFifth => new(7);
    public static Interval MinorSixth => new(8);
    public static Interval MajorSixth => new(9);
    public static Interval MinorSeventh => new(10);
    public static Interval MajorSeventh => new(11);
    public static Interval Octave => new(12);
}

public enum IntervalQuality { Perfect, Major, Minor, Augmented, Diminished, Other }
```

---

### Task 4: Scale Type

**File:** `src/SqncR.Theory/Scale.cs`

```csharp
namespace SqncR.Theory;

public record Scale(Note Root, ScaleMode Mode)
{
    public string Name => $"{Root.Name} {Mode}";

    public IReadOnlyList<Note> Notes => GetIntervals()
        .Select(i => Root.Transpose(i))
        .ToList();

    public IReadOnlyList<int> GetIntervals() => Mode switch
    {
        ScaleMode.Major => [0, 2, 4, 5, 7, 9, 11],
        ScaleMode.Minor or ScaleMode.Aeolian => [0, 2, 3, 5, 7, 8, 10],
        ScaleMode.Dorian => [0, 2, 3, 5, 7, 9, 10],
        ScaleMode.Phrygian => [0, 1, 3, 5, 7, 8, 10],
        ScaleMode.Lydian => [0, 2, 4, 6, 7, 9, 11],
        ScaleMode.Mixolydian => [0, 2, 4, 5, 7, 9, 10],
        ScaleMode.Locrian => [0, 1, 3, 5, 6, 8, 10],
        ScaleMode.HarmonicMinor => [0, 2, 3, 5, 7, 8, 11],
        ScaleMode.MelodicMinor => [0, 2, 3, 5, 7, 9, 11],
        ScaleMode.PentatonicMajor => [0, 2, 4, 7, 9],
        ScaleMode.PentatonicMinor => [0, 3, 5, 7, 10],
        ScaleMode.Blues => [0, 3, 5, 6, 7, 10],
        ScaleMode.WholeTone => [0, 2, 4, 6, 8, 10],
        ScaleMode.Chromatic => [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11],
        _ => throw new ArgumentException($"Unknown mode: {Mode}")
    };

    public Note ScaleDegree(int degree)
    {
        var notes = Notes;
        // 1-indexed, wraps with octaves
        var idx = (degree - 1) % notes.Count;
        var octaves = (degree - 1) / notes.Count;
        return notes[idx].Transpose(12 * octaves);
    }

    public bool Contains(Note note) =>
        GetIntervals().Contains(note.PitchClass - Root.PitchClass);

    public static Scale Parse(string key)
    {
        // "Am", "C", "F#m", "Bb", "D dorian"
        var parts = key.Trim().Split(' ');
        var rootStr = parts[0].TrimEnd('m', 'M');

        var isMinor = parts[0].EndsWith('m') ||
            (parts.Length > 1 && parts[1].Equals("minor", StringComparison.OrdinalIgnoreCase));

        var mode = ScaleMode.Major;
        if (isMinor) mode = ScaleMode.Minor;
        if (parts.Length > 1)
        {
            mode = Enum.TryParse<ScaleMode>(parts[1], true, out var m) ? m : mode;
        }

        return new Scale(new Note(rootStr + "4"), mode);
    }
}

public enum ScaleMode
{
    Major, Minor, Aeolian,
    Dorian, Phrygian, Lydian, Mixolydian, Locrian,
    HarmonicMinor, MelodicMinor,
    PentatonicMajor, PentatonicMinor, Blues,
    WholeTone, Chromatic
}
```

---

### Task 5: Chord Type

**File:** `src/SqncR.Theory/Chord.cs`

```csharp
namespace SqncR.Theory;

public record Chord(Note Root, ChordQuality Quality)
{
    public string Symbol => $"{Root.Name}{GetSymbol()}";

    public IReadOnlyList<Note> Notes => GetIntervals()
        .Select(i => Root.Transpose(i))
        .ToList();

    public IReadOnlyList<int> GetIntervals() => Quality switch
    {
        ChordQuality.Major => [0, 4, 7],
        ChordQuality.Minor => [0, 3, 7],
        ChordQuality.Diminished => [0, 3, 6],
        ChordQuality.Augmented => [0, 4, 8],
        ChordQuality.Sus2 => [0, 2, 7],
        ChordQuality.Sus4 => [0, 5, 7],
        ChordQuality.Major7 => [0, 4, 7, 11],
        ChordQuality.Minor7 => [0, 3, 7, 10],
        ChordQuality.Dominant7 => [0, 4, 7, 10],
        ChordQuality.Diminished7 => [0, 3, 6, 9],
        ChordQuality.HalfDiminished7 => [0, 3, 6, 10],
        ChordQuality.MinorMajor7 => [0, 3, 7, 11],
        ChordQuality.Major9 => [0, 4, 7, 11, 14],
        ChordQuality.Minor9 => [0, 3, 7, 10, 14],
        ChordQuality.Dominant9 => [0, 4, 7, 10, 14],
        ChordQuality.Add9 => [0, 4, 7, 14],
        _ => [0, 4, 7]
    };

    private string GetSymbol() => Quality switch
    {
        ChordQuality.Major => "",
        ChordQuality.Minor => "m",
        ChordQuality.Diminished => "dim",
        ChordQuality.Augmented => "aug",
        ChordQuality.Sus2 => "sus2",
        ChordQuality.Sus4 => "sus4",
        ChordQuality.Major7 => "maj7",
        ChordQuality.Minor7 => "m7",
        ChordQuality.Dominant7 => "7",
        ChordQuality.Diminished7 => "dim7",
        ChordQuality.HalfDiminished7 => "m7b5",
        ChordQuality.MinorMajor7 => "mMaj7",
        ChordQuality.Major9 => "maj9",
        ChordQuality.Minor9 => "m9",
        ChordQuality.Dominant9 => "9",
        ChordQuality.Add9 => "add9",
        _ => ""
    };

    public Chord Invert(int inversion)
    {
        // Returns same chord, but voicing will use different bass
        // Actual voicing handled by VoiceLeading algorithms
        return this;
    }

    public static Chord Parse(string symbol)
    {
        // "Cmaj7", "Am", "F#m7", "Bb7", etc.
        var match = System.Text.RegularExpressions.Regex.Match(
            symbol.Trim(), @"^([A-Ga-g][#b]?)(.*)$");

        if (!match.Success)
            throw new ArgumentException($"Invalid chord: {symbol}");

        var rootStr = match.Groups[1].Value;
        var qualityStr = match.Groups[2].Value.ToLowerInvariant();

        var quality = qualityStr switch
        {
            "" or "maj" => ChordQuality.Major,
            "m" or "min" or "minor" => ChordQuality.Minor,
            "dim" or "o" => ChordQuality.Diminished,
            "aug" or "+" => ChordQuality.Augmented,
            "sus2" => ChordQuality.Sus2,
            "sus4" or "sus" => ChordQuality.Sus4,
            "maj7" or "M7" => ChordQuality.Major7,
            "m7" or "min7" or "-7" => ChordQuality.Minor7,
            "7" or "dom7" => ChordQuality.Dominant7,
            "dim7" or "o7" => ChordQuality.Diminished7,
            "m7b5" or "ø" or "ø7" => ChordQuality.HalfDiminished7,
            "mmaj7" or "mM7" => ChordQuality.MinorMajor7,
            "maj9" or "M9" => ChordQuality.Major9,
            "m9" or "min9" => ChordQuality.Minor9,
            "9" => ChordQuality.Dominant9,
            "add9" => ChordQuality.Add9,
            _ => ChordQuality.Major
        };

        return new Chord(new Note(rootStr + "4"), quality);
    }
}

public enum ChordQuality
{
    Major, Minor, Diminished, Augmented,
    Sus2, Sus4,
    Major7, Minor7, Dominant7, Diminished7, HalfDiminished7, MinorMajor7,
    Major9, Minor9, Dominant9, Add9
}
```

---

### Task 6: Test Project

```bash
cd tests
dotnet new xunit -n SqncR.Theory.Tests -f net9.0
cd ..
dotnet sln add tests/SqncR.Theory.Tests

cd tests/SqncR.Theory.Tests
dotnet add package FluentAssertions
dotnet add reference ../../src/SqncR.Theory
```

---

### Task 7: Music Theory Tests

**File:** `tests/SqncR.Theory.Tests/NoteTests.cs`

```csharp
using FluentAssertions;

namespace SqncR.Theory.Tests;

public class NoteTests
{
    [Theory]
    [InlineData(60, "C4")]
    [InlineData(69, "A4")]
    [InlineData(48, "C3")]
    [InlineData(72, "C5")]
    [InlineData(0, "C-1")]
    [InlineData(127, "G9")]
    public void Note_FromMidi_HasCorrectName(int midi, string expected)
    {
        var note = new Note(midi);
        note.Name.Should().Be(expected);
    }

    [Theory]
    [InlineData("C4", 60)]
    [InlineData("A4", 69)]
    [InlineData("C#4", 61)]
    [InlineData("Db4", 61)]
    [InlineData("F#2", 42)]
    [InlineData("Bb5", 82)]
    public void Note_FromString_HasCorrectMidi(string name, int expected)
    {
        var note = new Note(name);
        note.MidiNumber.Should().Be(expected);
    }

    [Fact]
    public void Note_A4_HasFrequency440()
    {
        var note = new Note("A4");
        note.Frequency().Should().BeApproximately(440.0, 0.01);
    }

    [Theory]
    [InlineData(60, 7, 67)]   // C4 + P5 = G4
    [InlineData(60, 12, 72)]  // C4 + octave = C5
    [InlineData(60, -12, 48)] // C4 - octave = C3
    public void Note_Transpose_ReturnsCorrectNote(int start, int semitones, int expected)
    {
        var note = new Note(start);
        note.Transpose(semitones).MidiNumber.Should().Be(expected);
    }
}
```

**File:** `tests/SqncR.Theory.Tests/ScaleTests.cs`

```csharp
public class ScaleTests
{
    [Fact]
    public void CMajor_HasCorrectNotes()
    {
        var scale = new Scale(new Note("C4"), ScaleMode.Major);
        var names = scale.Notes.Select(n => n.Name).ToList();

        names.Should().BeEquivalentTo(["C4", "D4", "E4", "F4", "G4", "A4", "B4"]);
    }

    [Fact]
    public void AMinor_HasCorrectNotes()
    {
        var scale = new Scale(new Note("A4"), ScaleMode.Minor);
        var names = scale.Notes.Select(n => n.Name).ToList();

        names.Should().BeEquivalentTo(["A4", "B4", "C5", "D5", "E5", "F5", "G5"]);
    }

    [Fact]
    public void DDorian_HasCorrectIntervals()
    {
        var scale = new Scale(new Note("D4"), ScaleMode.Dorian);
        var intervals = scale.GetIntervals();

        intervals.Should().BeEquivalentTo([0, 2, 3, 5, 7, 9, 10]);
    }
}
```

**File:** `tests/SqncR.Theory.Tests/ChordTests.cs`

```csharp
public class ChordTests
{
    [Fact]
    public void CMajor_HasCorrectNotes()
    {
        var chord = new Chord(new Note("C4"), ChordQuality.Major);
        var names = chord.Notes.Select(n => n.Name).ToList();

        names.Should().BeEquivalentTo(["C4", "E4", "G4"]);
    }

    [Fact]
    public void Am7_HasCorrectNotes()
    {
        var chord = Chord.Parse("Am7");
        var intervals = chord.GetIntervals();

        intervals.Should().BeEquivalentTo([0, 3, 7, 10]);
    }

    [Theory]
    [InlineData("C", ChordQuality.Major)]
    [InlineData("Cm", ChordQuality.Minor)]
    [InlineData("C7", ChordQuality.Dominant7)]
    [InlineData("Cmaj7", ChordQuality.Major7)]
    [InlineData("Cm7", ChordQuality.Minor7)]
    [InlineData("Cdim", ChordQuality.Diminished)]
    public void ChordParse_ReturnsCorrectQuality(string symbol, ChordQuality expected)
    {
        var chord = Chord.Parse(symbol);
        chord.Quality.Should().Be(expected);
    }
}
```

---

## Definition of Done

- [ ] Note type: MIDI ↔ name ↔ frequency conversion
- [ ] Interval type: semitones, quality, names
- [ ] Scale type: all modes generate correct notes
- [ ] Chord type: triads, 7ths, extensions
- [ ] All tests pass
- [ ] Test coverage > 90% on Theory project

---

## What's Next

**P3: First Generation Skills** - Use theory types to generate music

---

**Priority:** P2
**Status:** Waiting for P1
**Updated:** January 29, 2026
