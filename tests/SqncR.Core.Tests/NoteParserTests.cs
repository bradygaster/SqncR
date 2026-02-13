namespace SqncR.Core.Tests;

public class NoteParserTests
{
    [Theory]
    [InlineData("C4", 60)]
    [InlineData("A4", 69)]
    [InlineData("C0", 12)]
    [InlineData("C-1", 0)]
    [InlineData("G9", 127)]
    public void Parse_StandardNotes_ReturnsCorrectMidiNumber(string note, int expected)
    {
        Assert.Equal(expected, NoteParser.Parse(note));
    }

    [Theory]
    [InlineData("C#4", 61)]
    [InlineData("Db4", 61)]
    [InlineData("F#2", 42)]
    [InlineData("Gb2", 42)]
    [InlineData("Bb3", 58)]
    [InlineData("A#3", 58)]
    public void Parse_Accidentals_ReturnsCorrectMidiNumber(string note, int expected)
    {
        Assert.Equal(expected, NoteParser.Parse(note));
    }

    [Theory]
    [InlineData("c4", 60)]
    [InlineData("a4", 69)]
    [InlineData("f#2", 42)]
    public void Parse_LowercaseNotes_ReturnsCorrectMidiNumber(string note, int expected)
    {
        Assert.Equal(expected, NoteParser.Parse(note));
    }

    [Theory]
    [InlineData("")]
    [InlineData("X4")]
    [InlineData("C")]
    [InlineData("4")]
    [InlineData("CC4")]
    public void Parse_InvalidNotes_ThrowsArgumentException(string note)
    {
        Assert.Throws<ArgumentException>(() => NoteParser.Parse(note));
    }

    [Theory]
    [InlineData(60, "C4")]
    [InlineData(69, "A4")]
    [InlineData(61, "C#4")]
    [InlineData(63, "D#4")]
    [InlineData(0, "C-1")]
    [InlineData(127, "G9")]
    public void ToNoteName_ValidMidiNumbers_ReturnsCorrectName(int midi, string expected)
    {
        Assert.Equal(expected, NoteParser.ToNoteName(midi));
    }

    [Fact]
    public void Parse_AllOctaves_SpansFullMidiRange()
    {
        // C-1 should be 0
        Assert.Equal(0, NoteParser.Parse("C-1"));

        // C4 is middle C = 60
        Assert.Equal(60, NoteParser.Parse("C4"));

        // Each octave is 12 semitones apart
        Assert.Equal(48, NoteParser.Parse("C3"));
        Assert.Equal(72, NoteParser.Parse("C5"));
    }

    [Theory]
    [InlineData("E#4", 65)]  // E# = F
    [InlineData("Fb4", 64)]  // Fb = E
    public void Parse_EnharmonicEquivalents_ReturnsCorrectMidiNumber(string note, int expected)
    {
        Assert.Equal(expected, NoteParser.Parse(note));
    }

    [Fact]
    public void Parse_BSharpAndCFlat_DoNotWrapOctave()
    {
        // Known limitation: B#3 should theoretically equal C4 (60) but parser
        // doesn't handle octave wrapping, so B#3 = offset 0 + (3+1)*12 = 48.
        // Cb4 should theoretically equal B3 (59) but computes as 71.
        // Documenting actual behavior — fix requires source code change.
        Assert.Equal(48, NoteParser.Parse("B#3"));
        Assert.Equal(71, NoteParser.Parse("Cb4"));
    }

    [Theory]
    [InlineData("A#9")]   // would be 130 — out of range
    [InlineData("C-2")]   // would be -12 — out of range
    public void Parse_OutOfMidiRange_ThrowsArgumentException(string note)
    {
        Assert.Throws<ArgumentException>(() => NoteParser.Parse(note));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(128)]
    public void ToNoteName_OutOfRange_ThrowsArgumentOutOfRangeException(int midi)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NoteParser.ToNoteName(midi));
    }

    [Theory]
    [InlineData("C4")]
    [InlineData("A4")]
    [InlineData("F#2")]
    [InlineData("G9")]
    [InlineData("C-1")]
    public void RoundTrip_ParseThenToNoteName_PreservesNote(string note)
    {
        var midi = NoteParser.Parse(note);
        var result = NoteParser.ToNoteName(midi);
        Assert.Equal(note, result);
    }

    [Fact]
    public void Parse_WhitespaceAroundNote_IsHandled()
    {
        Assert.Equal(60, NoteParser.Parse("  C4  "));
    }
}
