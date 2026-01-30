using YamlDotNet.Serialization;

namespace SqncR.Core.Models;

public class Sequence
{
    [YamlMember(Alias = "meta")]
    public MetaData Meta { get; set; } = new();

    [YamlMember(Alias = "intent")]
    public List<string>? Intent { get; set; }

    [YamlMember(Alias = "devices")]
    public Dictionary<string, DeviceMapping>? Devices { get; set; }

    [YamlMember(Alias = "patterns")]
    public Dictionary<string, Pattern> Patterns { get; set; } = new();

    [YamlMember(Alias = "sections")]
    public Dictionary<string, Section> Sections { get; set; } = new();

    [YamlMember(Alias = "arrange")]
    public List<ArrangeEntry> Arrange { get; set; } = new();
}

public class MetaData
{
    [YamlMember(Alias = "title")]
    public string Title { get; set; } = "Untitled";

    [YamlMember(Alias = "artist")]
    public string? Artist { get; set; }

    [YamlMember(Alias = "tempo")]
    public int Tempo { get; set; } = 120;

    [YamlMember(Alias = "key")]
    public string Key { get; set; } = "C";

    [YamlMember(Alias = "time")]
    public TimeSignature Time { get; set; } = new();

    [YamlMember(Alias = "tpq")]
    public int Tpq { get; set; } = 480;
}

public class TimeSignature
{
    [YamlMember(Alias = "beats")]
    public int Beats { get; set; } = 4;

    [YamlMember(Alias = "division")]
    public int Division { get; set; } = 4;
}

public class DeviceMapping
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = "";

    [YamlMember(Alias = "ch")]
    public int Ch { get; set; } = 1;
}

public class Pattern
{
    [YamlMember(Alias = "length")]
    public int Length { get; set; }

    [YamlMember(Alias = "defaults")]
    public PatternDefaults? Defaults { get; set; }

    [YamlMember(Alias = "events")]
    public List<NoteEvent> Events { get; set; } = new();
}

public class PatternDefaults
{
    [YamlMember(Alias = "vel")]
    public object? Vel { get; set; }

    [YamlMember(Alias = "t_rand")]
    public object? TRand { get; set; }
}

public class NoteEvent
{
    [YamlMember(Alias = "t")]
    public int T { get; set; }

    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "note";

    [YamlMember(Alias = "note")]
    public string Note { get; set; } = "C4";

    [YamlMember(Alias = "vel")]
    public object? Vel { get; set; } = 80;

    [YamlMember(Alias = "dur")]
    public int Dur { get; set; } = 480;

    [YamlMember(Alias = "prob")]
    public double? Prob { get; set; }
}

public class Section
{
    [YamlMember(Alias = "length")]
    public int Length { get; set; }

    [YamlMember(Alias = "loopable")]
    public bool Loopable { get; set; }

    [YamlMember(Alias = "tracks")]
    public List<Track> Tracks { get; set; } = new();
}

public class Track
{
    [YamlMember(Alias = "ch")]
    public int Ch { get; set; } = 1;

    [YamlMember(Alias = "groove")]
    public string? Groove { get; set; }

    [YamlMember(Alias = "sequence")]
    public List<SequenceEntry> Sequence { get; set; } = new();
}

public class SequenceEntry
{
    [YamlMember(Alias = "at")]
    public int At { get; set; }

    [YamlMember(Alias = "pattern")]
    public string Pattern { get; set; } = "";

    [YamlMember(Alias = "repeat")]
    public int Repeat { get; set; } = 1;

    [YamlMember(Alias = "transpose")]
    public int Transpose { get; set; } = 0;
}

public class ArrangeEntry
{
    [YamlMember(Alias = "at")]
    public int At { get; set; }

    [YamlMember(Alias = "section")]
    public string Section { get; set; } = "";
}
