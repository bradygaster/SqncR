namespace SqncR.Theory.Tests;

public class ScaleTests
{
    // --- C Major: C D E F G A B ---
    // MIDI (octave 4): 60, 62, 64, 65, 67, 69, 71

    [Fact]
    public void Major_CMajor_HasCorrectIntervals()
    {
        var scale = Scale.Major(60); // C4
        Assert.Equal([0, 2, 4, 5, 7, 9, 11], scale.Intervals);
    }

    [Fact]
    public void Major_CMajor_GetNotesInOctave4()
    {
        var scale = Scale.Major(60);
        var notes = scale.GetNotesInOctave(4);
        // C4=60, D4=62, E4=64, F4=65, G4=67, A4=69, B4=71
        Assert.Equal([60, 62, 64, 65, 67, 69, 71], notes);
    }

    [Fact]
    public void Major_GMajor_GetNotesInOctave4()
    {
        // G Major: G A B C D E F#
        // Root = G4 (67), pitch class = 7
        var scale = Scale.Major(67);
        var notes = scale.GetNotesInOctave(4);
        // Starting from octaveBase (60) + rootPitchClass (7) + intervals
        // G4=67, A4=69, B4=71, C5=72, D5=74, E5=76, F#5=78
        Assert.Equal([67, 69, 71, 72, 74, 76, 78], notes);
    }

    // --- A Natural Minor: A B C D E F G ---
    [Fact]
    public void Minor_AMinor_HasCorrectIntervals()
    {
        var scale = Scale.Minor(69); // A4
        Assert.Equal([0, 2, 3, 5, 7, 8, 10], scale.Intervals);
    }

    [Fact]
    public void Minor_AMinor_GetNotesInOctave4()
    {
        var scale = Scale.Minor(69);
        // A pitch class = 9. Octave 4 base = 60. Start = 60 + 9 = 69.
        // A4=69, B4=71, C5=72, D5=74, E5=76, F5=77, G5=79
        var notes = scale.GetNotesInOctave(4);
        Assert.Equal([69, 71, 72, 74, 76, 77, 79], notes);
    }

    // --- Harmonic Minor: raised 7th ---
    [Fact]
    public void HarmonicMinor_AHarmonicMinor_HasCorrectIntervals()
    {
        var scale = Scale.HarmonicMinor(69);
        Assert.Equal([0, 2, 3, 5, 7, 8, 11], scale.Intervals);
    }

    // --- Melodic Minor: raised 6th and 7th ---
    [Fact]
    public void MelodicMinor_HasCorrectIntervals()
    {
        var scale = Scale.MelodicMinor(60);
        Assert.Equal([0, 2, 3, 5, 7, 9, 11], scale.Intervals);
    }

    // --- Pentatonic scales ---
    [Fact]
    public void PentatonicMajor_CPentatonic_HasCorrectIntervals()
    {
        var scale = Scale.PentatonicMajor(60);
        // C D E G A → 0 2 4 7 9
        Assert.Equal([0, 2, 4, 7, 9], scale.Intervals);
    }

    [Fact]
    public void PentatonicMinor_AMinorPentatonic_HasCorrectIntervals()
    {
        var scale = Scale.PentatonicMinor(69);
        // A C D E G → 0 3 5 7 10
        Assert.Equal([0, 3, 5, 7, 10], scale.Intervals);
    }

    [Fact]
    public void PentatonicMajor_GetNotesInOctave4()
    {
        var scale = Scale.PentatonicMajor(60);
        // C4=60, D4=62, E4=64, G4=67, A4=69
        Assert.Equal([60, 62, 64, 67, 69], scale.GetNotesInOctave(4));
    }

    // --- Blues scale ---
    [Fact]
    public void Blues_CBlues_HasCorrectIntervals()
    {
        var scale = Scale.Blues(60);
        // C Eb F F# G Bb → 0 3 5 6 7 10
        Assert.Equal([0, 3, 5, 6, 7, 10], scale.Intervals);
    }

    [Fact]
    public void Blues_GetNotesInOctave4()
    {
        var scale = Scale.Blues(60);
        // C4=60, Eb4=63, F4=65, F#4=66, G4=67, Bb4=70
        Assert.Equal([60, 63, 65, 66, 67, 70], scale.GetNotesInOctave(4));
    }

    // --- Whole Tone ---
    [Fact]
    public void WholeTone_HasCorrectIntervals()
    {
        var scale = Scale.WholeTone(60);
        Assert.Equal([0, 2, 4, 6, 8, 10], scale.Intervals);
    }

    // --- Diminished (half-whole) ---
    [Fact]
    public void Diminished_HasCorrectIntervals()
    {
        var scale = Scale.DiminishedHalfWhole(60);
        // H-W: 0 1 3 4 6 7 9 10
        Assert.Equal([0, 1, 3, 4, 6, 7, 9, 10], scale.Intervals);
    }

    // --- Chromatic ---
    [Fact]
    public void Chromatic_HasAllTwelveNotes()
    {
        var scale = Scale.Chromatic(60);
        Assert.Equal(12, scale.Intervals.Count);
        Assert.Equal([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11], scale.Intervals);
    }

    // --- ContainsNote ---
    [Theory]
    [InlineData(60, true)]   // C4 — root
    [InlineData(62, true)]   // D4
    [InlineData(64, true)]   // E4
    [InlineData(65, true)]   // F4
    [InlineData(67, true)]   // G4
    [InlineData(69, true)]   // A4
    [InlineData(71, true)]   // B4
    [InlineData(61, false)]  // C#4 — not in C major
    [InlineData(63, false)]  // Eb4 — not in C major
    [InlineData(66, false)]  // F#4 — not in C major
    public void ContainsNote_CMajor_Octave4(int midiNote, bool expected)
    {
        var scale = Scale.Major(60);
        Assert.Equal(expected, scale.ContainsNote(midiNote));
    }

    [Theory]
    [InlineData(48, true)]   // C3 — different octave
    [InlineData(72, true)]   // C5 — different octave
    [InlineData(24, true)]   // C1
    [InlineData(36, true)]   // C2
    public void ContainsNote_CMajor_AcrossOctaves(int midiNote, bool expected)
    {
        var scale = Scale.Major(60);
        Assert.Equal(expected, scale.ContainsNote(midiNote));
    }

    [Fact]
    public void ContainsNote_FSharpMajor_VerifiesCorrectPitchClasses()
    {
        // F# Major: F# G# A# B C# D# E#(F)
        // Pitch classes: 6, 8, 10, 11, 1, 3, 5
        var scale = Scale.Major(66); // F#4 = 66
        Assert.True(scale.ContainsNote(66));   // F#
        Assert.True(scale.ContainsNote(68));   // G#
        Assert.True(scale.ContainsNote(70));   // A#
        Assert.True(scale.ContainsNote(71));   // B
        Assert.True(scale.ContainsNote(61));   // C#
        Assert.True(scale.ContainsNote(63));   // D#
        Assert.True(scale.ContainsNote(65));   // E#/F
        Assert.False(scale.ContainsNote(60));  // C natural — not in F# Major
    }

    // --- GetNearestScaleNote ---
    [Fact]
    public void GetNearestScaleNote_NoteAlreadyInScale_ReturnsSame()
    {
        var scale = Scale.Major(60);
        Assert.Equal(60, scale.GetNearestScaleNote(60));
        Assert.Equal(64, scale.GetNearestScaleNote(64));
    }

    [Fact]
    public void GetNearestScaleNote_SnapsToNearest_CMajor()
    {
        var scale = Scale.Major(60);
        // C#4 (61) — between C4 (60) and D4 (62), equidistant → rounds down to C4
        Assert.Equal(60, scale.GetNearestScaleNote(61));
        // F#4 (66) — between F4 (65) and G4 (67), equidistant → rounds down to F4
        Assert.Equal(65, scale.GetNearestScaleNote(66));
    }

    [Fact]
    public void GetNearestScaleNote_Eb_SnapsToE_InCMajor()
    {
        var scale = Scale.Major(60);
        // Eb4 (63) — between D4 (62) and E4 (64), equidistant → rounds down to D4
        Assert.Equal(62, scale.GetNearestScaleNote(63));
    }

    [Fact]
    public void GetNearestScaleNote_Ab_InCMajor()
    {
        var scale = Scale.Major(60);
        // Ab4 (68) — between G4 (67) and A4 (69), equidistant → rounds down to G4
        Assert.Equal(67, scale.GetNearestScaleNote(68));
    }

    // --- Record equality ---
    [Fact]
    public void Scale_RecordEquality_WorksCorrectly()
    {
        var a = Scale.Major(60);
        var b = Scale.Major(60);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Scale_DifferentRoots_AreNotEqual()
    {
        var cMajor = Scale.Major(60);
        var dMajor = Scale.Major(62);
        Assert.NotEqual(cMajor, dMajor);
    }

    // --- Edge cases ---
    [Fact]
    public void GetNotesInOctave_ClipsToMidiRange()
    {
        // A scale rooted very high should not exceed MIDI 127
        var scale = Scale.Major(127); // G9 root, pitch class 7
        var notes = scale.GetNotesInOctave(9);
        Assert.All(notes, n => Assert.InRange(n, 0, 127));
    }

    [Fact]
    public void GetNotesInOctave_LowOctave_ClipsToMidiRange()
    {
        var scale = Scale.Major(0); // C-1 root
        var notes = scale.GetNotesInOctave(-1);
        Assert.All(notes, n => Assert.InRange(n, 0, 127));
    }

    [Fact]
    public void RootPitchClass_CalculatesCorrectly()
    {
        Assert.Equal(0, Scale.Major(60).RootPitchClass);   // C
        Assert.Equal(9, Scale.Major(69).RootPitchClass);   // A
        Assert.Equal(2, Scale.Major(62).RootPitchClass);   // D
        Assert.Equal(7, Scale.Major(67).RootPitchClass);   // G
    }
}
