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
}
