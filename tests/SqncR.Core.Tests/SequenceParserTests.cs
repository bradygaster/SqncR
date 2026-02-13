using SqncR.Core;
using SqncR.Core.Models;

namespace SqncR.Core.Tests;

public class SequenceParserTests
{
    private readonly SequenceParser _parser = new();

    private static string ExamplesDir =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "examples");

    #region Example file parsing — every .sqnc.yaml must survive a round-trip

    [Theory]
    [InlineData("chill-ambient.sqnc.yaml", "Late Night Ambient", 70, "Cm")]
    [InlineData("polyend-rain.sqnc.yaml", "Rain on Glass", 62, "Am")]
    [InlineData("seven-nation-army.sqnc.yaml", "Seven Nation Army", 124, "Em")]
    public void Parse_ExampleFile_ReturnsCorrectMeta(string fileName, string expectedTitle, int expectedTempo, string expectedKey)
    {
        var path = Path.GetFullPath(Path.Combine(ExamplesDir, fileName));
        var seq = _parser.Parse(path);

        Assert.Equal(expectedTitle, seq.Meta.Title);
        Assert.Equal(expectedTempo, seq.Meta.Tempo);
        Assert.Equal(expectedKey, seq.Meta.Key);
    }

    [Theory]
    [InlineData("another-brick-in-the-wall.sqnc.yaml")]
    [InlineData("little-fluffy-clouds.sqnc.yaml")]
    public void Parse_ExampleFileWithChoiceConstructs_FailsDeserialization(string fileName)
    {
        // These files use { choice: [...] } for note values, which can't
        // deserialize into a string field. This is a known model limitation.
        var path = Path.GetFullPath(Path.Combine(ExamplesDir, fileName));
        Assert.ThrowsAny<Exception>(() => _parser.Parse(path));
    }

    [Theory]
    [InlineData("chill-ambient.sqnc.yaml")]
    [InlineData("polyend-rain.sqnc.yaml")]
    [InlineData("seven-nation-army.sqnc.yaml")]
    public void Parse_ExampleFile_HasPatternsAndSections(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(ExamplesDir, fileName));
        var seq = _parser.Parse(path);

        Assert.NotEmpty(seq.Patterns);
        Assert.NotEmpty(seq.Sections);
        Assert.NotEmpty(seq.Arrange);
    }

    #endregion

    #region Pattern structure validation

    [Fact]
    public void Parse_ChillAmbient_HasExpectedPatternCount()
    {
        var path = Path.GetFullPath(Path.Combine(ExamplesDir, "chill-ambient.sqnc.yaml"));
        var seq = _parser.Parse(path);

        // 8 patterns: bass_Cm, bass_Ab, bass_Eb, bass_Bb, pad_Cm7, pad_Abmaj7, pad_Ebmaj7, pad_Bbmaj7
        Assert.Equal(8, seq.Patterns.Count);
    }

    [Fact]
    public void Parse_ChillAmbient_PatternHasEvents()
    {
        var path = Path.GetFullPath(Path.Combine(ExamplesDir, "chill-ambient.sqnc.yaml"));
        var seq = _parser.Parse(path);

        var bassCm = seq.Patterns["bass_Cm"];
        Assert.Equal(7680, bassCm.Length);
        Assert.Equal(4, bassCm.Events.Count);
        Assert.Equal("C2", bassCm.Events[0].Note);
    }

    [Fact]
    public void Parse_ChillAmbient_PatternDefaultsAreParsed()
    {
        var path = Path.GetFullPath(Path.Combine(ExamplesDir, "chill-ambient.sqnc.yaml"));
        var seq = _parser.Parse(path);

        var bassCm = seq.Patterns["bass_Cm"];
        Assert.NotNull(bassCm.Defaults);
        Assert.NotNull(bassCm.Defaults.Vel);
        Assert.NotNull(bassCm.Defaults.TRand);
    }

    #endregion

    #region Section and arrange validation

    [Fact]
    public void Parse_ChillAmbient_SectionsHaveTracks()
    {
        var path = Path.GetFullPath(Path.Combine(ExamplesDir, "chill-ambient.sqnc.yaml"));
        var seq = _parser.Parse(path);

        Assert.Single(seq.Sections); // progression
        Assert.NotEmpty(seq.Sections["progression"].Tracks);
    }

    [Fact]
    public void Parse_ChillAmbient_ArrangeHasEntries()
    {
        var path = Path.GetFullPath(Path.Combine(ExamplesDir, "chill-ambient.sqnc.yaml"));
        var seq = _parser.Parse(path);

        Assert.Equal(2, seq.Arrange.Count);
        Assert.Equal("progression", seq.Arrange[0].Section);
        Assert.Equal(0, seq.Arrange[0].At);
    }

    [Fact]
    public void Parse_PolyendRain_SectionHasLoopableFlag()
    {
        var path = Path.GetFullPath(Path.Combine(ExamplesDir, "polyend-rain.sqnc.yaml"));
        var seq = _parser.Parse(path);

        Assert.True(seq.Sections["verse"].Loopable);
    }

    #endregion

    #region Seven Nation Army — rich structure tests

    [Fact]
    public void Parse_SevenNationArmy_HasThreeSections()
    {
        var path = Path.GetFullPath(Path.Combine(ExamplesDir, "seven-nation-army.sqnc.yaml"));
        var seq = _parser.Parse(path);

        Assert.Equal(3, seq.Sections.Count);
        Assert.Contains("intro", seq.Sections.Keys);
        Assert.Contains("bridge", seq.Sections.Keys);
        Assert.Contains("breakdown", seq.Sections.Keys);
    }

    [Fact]
    public void Parse_SevenNationArmy_SectionHasLoopableFlag()
    {
        var path = Path.GetFullPath(Path.Combine(ExamplesDir, "seven-nation-army.sqnc.yaml"));
        var seq = _parser.Parse(path);

        Assert.True(seq.Sections["intro"].Loopable);
        Assert.True(seq.Sections["breakdown"].Loopable);
    }

    [Fact]
    public void Parse_SevenNationArmy_TrackHasGroove()
    {
        var path = Path.GetFullPath(Path.Combine(ExamplesDir, "seven-nation-army.sqnc.yaml"));
        var seq = _parser.Parse(path);

        var introTracks = seq.Sections["intro"].Tracks;
        var bassTrack = introTracks.First(t => t.Ch == 1);
        Assert.Equal("mpc60", bassTrack.Groove);
    }

    [Fact]
    public void Parse_SevenNationArmy_HasDevicesWithThreeChannels()
    {
        var path = Path.GetFullPath(Path.Combine(ExamplesDir, "seven-nation-army.sqnc.yaml"));
        var seq = _parser.Parse(path);

        Assert.NotNull(seq.Devices);
        Assert.Equal(3, seq.Devices.Count);
        Assert.Equal("Moog Sub 37", seq.Devices["bass"].Name);
        Assert.Equal(10, seq.Devices["drums"].Ch);
    }

    [Fact]
    public void Parse_SevenNationArmy_ArrangeHasFiveEntries()
    {
        var path = Path.GetFullPath(Path.Combine(ExamplesDir, "seven-nation-army.sqnc.yaml"));
        var seq = _parser.Parse(path);

        Assert.Equal(5, seq.Arrange.Count);
        Assert.Equal("intro", seq.Arrange[0].Section);
        Assert.Equal("bridge", seq.Arrange[2].Section);
    }

    [Fact]
    public void Parse_SevenNationArmy_MainRiffHasCorrectNotes()
    {
        var path = Path.GetFullPath(Path.Combine(ExamplesDir, "seven-nation-army.sqnc.yaml"));
        var seq = _parser.Parse(path);

        var mainRiff = seq.Patterns["main_riff"];
        Assert.Equal(7680, mainRiff.Length);
        Assert.Equal("E2", mainRiff.Events[0].Note);
        Assert.Equal("G2", mainRiff.Events[2].Note);
    }

    #endregion

    #region Devices and intent

    [Fact]
    public void Parse_ChillAmbient_DevicesAreParsed()
    {
        var path = Path.GetFullPath(Path.Combine(ExamplesDir, "chill-ambient.sqnc.yaml"));
        var seq = _parser.Parse(path);

        Assert.NotNull(seq.Devices);
        Assert.Equal(2, seq.Devices.Count);
        Assert.Equal("Polyend Synth", seq.Devices["bass"].Name);
        Assert.Equal(1, seq.Devices["bass"].Ch);
    }

    [Fact]
    public void Parse_ChillAmbient_IntentIsParsed()
    {
        var path = Path.GetFullPath(Path.Combine(ExamplesDir, "chill-ambient.sqnc.yaml"));
        var seq = _parser.Parse(path);

        Assert.NotNull(seq.Intent);
        Assert.Equal(2, seq.Intent.Count);
        Assert.Contains("chill ambient", seq.Intent[0]);
    }

    #endregion

    #region Time signature

    [Fact]
    public void Parse_StandardTimeSignature()
    {
        var path = Path.GetFullPath(Path.Combine(ExamplesDir, "chill-ambient.sqnc.yaml"));
        var seq = _parser.Parse(path);

        Assert.Equal(4, seq.Meta.Time.Beats);
        Assert.Equal(4, seq.Meta.Time.Division);
    }

    [Fact]
    public void Parse_TpqIsParsed()
    {
        var path = Path.GetFullPath(Path.Combine(ExamplesDir, "chill-ambient.sqnc.yaml"));
        var seq = _parser.Parse(path);

        Assert.Equal(480, seq.Meta.Tpq);
    }

    #endregion

    #region ParseYaml — direct YAML string parsing

    [Fact]
    public void ParseYaml_MinimalSequence_ReturnsDefaults()
    {
        var yaml = @"
meta:
  title: Test
  tempo: 100
patterns: {}
sections: {}
arrange: []
";
        var seq = _parser.ParseYaml(yaml);

        Assert.Equal("Test", seq.Meta.Title);
        Assert.Equal(100, seq.Meta.Tempo);
        Assert.Empty(seq.Patterns);
        Assert.Empty(seq.Sections);
        Assert.Empty(seq.Arrange);
    }

    [Fact]
    public void ParseYaml_EmptyString_ReturnsDefaultSequence()
    {
        var seq = _parser.ParseYaml("");

        // YamlDotNet returns null for empty string, parser converts to default
        Assert.NotNull(seq);
    }

    [Fact]
    public void ParseYaml_InvalidYaml_ThrowsException()
    {
        var badYaml = "{{{{not: valid: yaml: [[[";

        Assert.ThrowsAny<Exception>(() => _parser.ParseYaml(badYaml));
    }

    #endregion

    #region Error cases

    [Fact]
    public void Parse_MissingFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() => _parser.Parse("nonexistent-file.sqnc.yaml"));
    }

    [Fact]
    public void Parse_NullPath_ThrowsException()
    {
        Assert.ThrowsAny<Exception>(() => _parser.Parse(null!));
    }

    #endregion

    #region Polyend Rain — minimal file (no devices, no intent)

    [Fact]
    public void Parse_PolyendRain_NullableFieldsAreNull()
    {
        var path = Path.GetFullPath(Path.Combine(ExamplesDir, "polyend-rain.sqnc.yaml"));
        var seq = _parser.Parse(path);

        Assert.Null(seq.Devices);
        Assert.Null(seq.Intent);
    }

    [Fact]
    public void Parse_PolyendRain_HasExpectedStructure()
    {
        var path = Path.GetFullPath(Path.Combine(ExamplesDir, "polyend-rain.sqnc.yaml"));
        var seq = _parser.Parse(path);

        Assert.Equal(2, seq.Patterns.Count);
        Assert.Single(seq.Sections);
        Assert.Single(seq.Arrange);
    }

    #endregion

    #region Sequence entry details

    [Fact]
    public void Parse_PolyendRain_SequenceEntryHasPattern()
    {
        var path = Path.GetFullPath(Path.Combine(ExamplesDir, "polyend-rain.sqnc.yaml"));
        var seq = _parser.Parse(path);

        var verseTracks = seq.Sections["verse"].Tracks;
        var bassTrack = verseTracks.First(t => t.Ch == 1);
        var firstEntry = bassTrack.Sequence[0];

        Assert.Equal(0, firstEntry.At);
        Assert.Equal("bass_drone", firstEntry.Pattern);
    }

    [Fact]
    public void Parse_ChillAmbient_TrackChannelIsParsed()
    {
        var path = Path.GetFullPath(Path.Combine(ExamplesDir, "chill-ambient.sqnc.yaml"));
        var seq = _parser.Parse(path);

        var tracks = seq.Sections["progression"].Tracks;
        Assert.Contains(tracks, t => t.Ch == 1);
        Assert.Contains(tracks, t => t.Ch == 2);
    }

    #endregion
}
