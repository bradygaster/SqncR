# P3: First Generation Skills

**Priority:** 3
**Depends on:** P2 (Music Theory)
**Goal:** Generate actual music from natural language, not just play files
**Duration:** ~2 weeks

---

## The Wow Moment

You describe what you want, and SqncR creates it.

```bash
sqncr generate "ambient drone in C minor, 70 BPM" --device 0
```

Music starts playing that wasn't in a file. It was created.

---

## What We're Building

1. **Skill framework** - Interface for skills, registry
2. **6 MVP skills:**
   - `list-devices` - Already done, formalize as skill
   - `chord-progression` - Generate progressions
   - `bass-line-generator` - Create bass lines
   - `drone-generator` - Simple ambient drones
   - `arpeggio-generator` - Arpeggiate chords
   - `rhythm-generator` - Basic rhythmic patterns
3. **SqncRService facade** - Central entry point
4. **CLI generate command** - Text → music

## What We're NOT Building Yet

- MCP server (skill execution via CLI only)
- Full agent system
- Advanced skills (40+ remaining)
- Full observability

---

## Tasks

### Task 1: Skill Framework

**File:** `src/SqncR.Core/Skills/ISkill.cs`

```csharp
namespace SqncR.Core.Skills;

public interface ISkill
{
    string Name { get; }
    string Description { get; }
    Task<SkillResult> ExecuteAsync(SkillInput input, CancellationToken ct = default);
}

public record SkillInput(Dictionary<string, object> Parameters)
{
    public T Get<T>(string key, T defaultValue = default!)
    {
        if (Parameters.TryGetValue(key, out var value))
        {
            if (value is T typed) return typed;
            // Try conversion
            return (T)Convert.ChangeType(value, typeof(T));
        }
        return defaultValue;
    }
}

public record SkillResult(bool Success, object? Data = null, string? Error = null)
{
    public static SkillResult Ok(object data) => new(true, data);
    public static SkillResult Fail(string error) => new(false, Error: error);
}
```

---

### Task 2: Skill Registry

**File:** `src/SqncR.Core/Skills/SkillRegistry.cs`

```csharp
namespace SqncR.Core.Skills;

public class SkillRegistry
{
    private readonly Dictionary<string, ISkill> _skills = new(StringComparer.OrdinalIgnoreCase);

    public void Register(ISkill skill)
    {
        _skills[skill.Name] = skill;
    }

    public ISkill? GetSkill(string name)
    {
        return _skills.TryGetValue(name, out var skill) ? skill : null;
    }

    public IReadOnlyList<ISkill> GetAllSkills() => _skills.Values.ToList();
}
```

---

### Task 3: Chord Progression Skill

**File:** `src/SqncR.Core/Skills/ChordProgressionSkill.cs`

```csharp
using SqncR.Theory;

namespace SqncR.Core.Skills;

public class ChordProgressionSkill : ISkill
{
    public string Name => "chord-progression";
    public string Description => "Generate chord progressions based on key, mode, and style";

    public Task<SkillResult> ExecuteAsync(SkillInput input, CancellationToken ct)
    {
        var key = input.Get<string>("key", "C");
        var mode = input.Get<string>("mode", "minor");
        var bars = input.Get<int>("bars", 4);
        var style = input.Get<string>("style", "simple");

        var scaleMode = Enum.TryParse<ScaleMode>(mode, true, out var m) ? m : ScaleMode.Minor;
        var scale = new Scale(new Note(key + "4"), scaleMode);

        var progression = GenerateProgression(scale, bars, style);

        return Task.FromResult(SkillResult.Ok(new
        {
            key,
            mode = scaleMode.ToString(),
            progression,
            bars
        }));
    }

    private List<ChordInfo> GenerateProgression(Scale scale, int bars, string style)
    {
        // Common progressions by style
        var degrees = style.ToLowerInvariant() switch
        {
            "sad" or "dark" => new[] { 1, 4, 6, 5 },      // i - iv - VI - V
            "happy" or "bright" => new[] { 1, 5, 6, 4 }, // I - V - vi - IV
            "jazz" => new[] { 2, 5, 1, 1 },              // ii - V - I - I
            "ambient" => new[] { 1, 4, 1, 5 },           // i - iv - i - V
            _ => new[] { 1, 4, 5, 1 }                     // I - IV - V - I
        };

        var chords = new List<ChordInfo>();
        for (int bar = 0; bar < bars; bar++)
        {
            var degree = degrees[bar % degrees.Length];
            var root = scale.ScaleDegree(degree);
            var quality = GetChordQuality(scale.Mode, degree);

            chords.Add(new ChordInfo
            {
                Bar = bar + 1,
                Degree = degree,
                Root = root.Name[..^1], // Remove octave
                Quality = quality,
                Symbol = $"{root.Name[..^1]}{GetQualitySymbol(quality)}"
            });
        }

        return chords;
    }

    private ChordQuality GetChordQuality(ScaleMode mode, int degree)
    {
        // Diatonic chord qualities for minor scale
        if (mode == ScaleMode.Minor)
        {
            return degree switch
            {
                1 => ChordQuality.Minor,
                2 => ChordQuality.Diminished,
                3 => ChordQuality.Major,
                4 => ChordQuality.Minor,
                5 => ChordQuality.Minor, // or Major for harmonic minor
                6 => ChordQuality.Major,
                7 => ChordQuality.Major,
                _ => ChordQuality.Minor
            };
        }
        // Major scale
        return degree switch
        {
            1 => ChordQuality.Major,
            2 => ChordQuality.Minor,
            3 => ChordQuality.Minor,
            4 => ChordQuality.Major,
            5 => ChordQuality.Major,
            6 => ChordQuality.Minor,
            7 => ChordQuality.Diminished,
            _ => ChordQuality.Major
        };
    }

    private string GetQualitySymbol(ChordQuality q) => q switch
    {
        ChordQuality.Major => "",
        ChordQuality.Minor => "m",
        ChordQuality.Diminished => "dim",
        _ => ""
    };
}

public record ChordInfo
{
    public int Bar { get; init; }
    public int Degree { get; init; }
    public string Root { get; init; } = "";
    public ChordQuality Quality { get; init; }
    public string Symbol { get; init; } = "";
}
```

---

### Task 4: Bass Line Generator Skill

**File:** `src/SqncR.Core/Skills/BassLineGeneratorSkill.cs`

```csharp
namespace SqncR.Core.Skills;

public class BassLineGeneratorSkill : ISkill
{
    public string Name => "bass-line-generator";
    public string Description => "Generate bass lines from chord progressions";

    public Task<SkillResult> ExecuteAsync(SkillInput input, CancellationToken ct)
    {
        var chords = input.Get<List<ChordInfo>>("chords", new());
        var style = input.Get<string>("style", "root"); // root, walking, rhythmic
        var tempo = input.Get<int>("tempo", 120);
        var tpq = 480;

        var events = new List<NoteEvent>();
        var ticksPerBar = tpq * 4; // 4/4 time

        for (int i = 0; i < chords.Count; i++)
        {
            var chord = Chord.Parse(chords[i].Symbol);
            var root = chord.Root.Transpose(-24); // Two octaves down for bass

            var barStart = i * ticksPerBar;

            if (style == "root")
            {
                // Simple: root on 1, root on 3
                events.Add(new NoteEvent { T = barStart, Note = root.Name, Vel = 90, Dur = tpq * 2 });
                events.Add(new NoteEvent { T = barStart + tpq * 2, Note = root.Name, Vel = 75, Dur = tpq * 2 });
            }
            else if (style == "walking")
            {
                // Walking: root, third, fifth, approach note
                var notes = chord.Notes;
                events.Add(new NoteEvent { T = barStart, Note = root.Name, Vel = 90, Dur = tpq });
                events.Add(new NoteEvent { T = barStart + tpq, Note = notes[1].Transpose(-24).Name, Vel = 75, Dur = tpq });
                events.Add(new NoteEvent { T = barStart + tpq * 2, Note = notes[2].Transpose(-24).Name, Vel = 75, Dur = tpq });
                events.Add(new NoteEvent { T = barStart + tpq * 3, Note = root.Transpose(-1).Name, Vel = 70, Dur = tpq }); // Approach
            }
            else // rhythmic
            {
                // Syncopated eighth notes
                events.Add(new NoteEvent { T = barStart, Note = root.Name, Vel = 95, Dur = tpq / 2 });
                events.Add(new NoteEvent { T = barStart + tpq, Note = root.Name, Vel = 70, Dur = tpq / 2 });
                events.Add(new NoteEvent { T = barStart + tpq + tpq / 2, Note = root.Name, Vel = 80, Dur = tpq / 2 });
                events.Add(new NoteEvent { T = barStart + tpq * 3, Note = root.Name, Vel = 85, Dur = tpq / 2 });
            }
        }

        return Task.FromResult(SkillResult.Ok(new { style, events }));
    }
}
```

---

### Task 5: Drone Generator Skill

**File:** `src/SqncR.Core/Skills/DroneGeneratorSkill.cs`

```csharp
namespace SqncR.Core.Skills;

public class DroneGeneratorSkill : ISkill
{
    public string Name => "drone-generator";
    public string Description => "Generate ambient drone patterns";

    public Task<SkillResult> ExecuteAsync(SkillInput input, CancellationToken ct)
    {
        var key = input.Get<string>("key", "C");
        var mode = input.Get<string>("mode", "minor");
        var bars = input.Get<int>("bars", 8);
        var density = input.Get<string>("density", "sparse"); // sparse, moderate, dense

        var scaleMode = Enum.TryParse<ScaleMode>(mode, true, out var m) ? m : ScaleMode.Minor;
        var scale = new Scale(new Note(key + "3"), scaleMode);
        var tpq = 480;
        var ticksPerBar = tpq * 4;

        var events = new List<NoteEvent>();
        var totalTicks = bars * ticksPerBar;

        // Root drone (always present)
        events.Add(new NoteEvent
        {
            T = 0,
            Note = scale.Root.Name,
            Vel = 50,
            Dur = totalTicks
        });

        // Fifth (for stability)
        events.Add(new NoteEvent
        {
            T = ticksPerBar, // Enters after 1 bar
            Note = scale.ScaleDegree(5).Name,
            Vel = 40,
            Dur = totalTicks - ticksPerBar
        });

        if (density != "sparse")
        {
            // Add third
            events.Add(new NoteEvent
            {
                T = ticksPerBar * 2,
                Note = scale.ScaleDegree(3).Name,
                Vel = 35,
                Dur = totalTicks - ticksPerBar * 2
            });
        }

        if (density == "dense")
        {
            // Add seventh and upper octave
            events.Add(new NoteEvent
            {
                T = ticksPerBar * 3,
                Note = scale.ScaleDegree(7).Name,
                Vel = 30,
                Dur = totalTicks - ticksPerBar * 3
            });
            events.Add(new NoteEvent
            {
                T = ticksPerBar * 4,
                Note = scale.Root.Transpose(12).Name,
                Vel = 25,
                Dur = totalTicks - ticksPerBar * 4
            });
        }

        return Task.FromResult(SkillResult.Ok(new { key, mode, bars, density, events }));
    }
}
```

---

### Task 6: SqncRService Facade

**File:** `src/SqncR.Core/SqncRService.cs`

```csharp
namespace SqncR.Core;

public class SqncRService
{
    private readonly SkillRegistry _skills;
    private readonly MidiService _midi;
    private readonly SequenceParser _parser;

    public SqncRService()
    {
        _skills = new SkillRegistry();
        _midi = new MidiService();
        _parser = new SequenceParser();

        // Register skills
        _skills.Register(new ChordProgressionSkill());
        _skills.Register(new BassLineGeneratorSkill());
        _skills.Register(new DroneGeneratorSkill());
        _skills.Register(new ArpeggioGeneratorSkill());
        _skills.Register(new RhythmGeneratorSkill());
    }

    public async Task<SkillResult> ExecuteSkillAsync(string skillName, Dictionary<string, object> parameters)
    {
        var skill = _skills.GetSkill(skillName);
        if (skill == null)
            return SkillResult.Fail($"Skill not found: {skillName}");

        return await skill.ExecuteAsync(new SkillInput(parameters));
    }

    public async Task GenerateAndPlayAsync(string description, int deviceIndex, CancellationToken ct)
    {
        // Parse description
        var parsed = ParseDescription(description);

        // Generate chord progression
        var progResult = await ExecuteSkillAsync("chord-progression", new()
        {
            ["key"] = parsed.Key,
            ["mode"] = parsed.Mode,
            ["bars"] = parsed.Bars,
            ["style"] = parsed.Style
        });

        // Generate bass line from chords
        var bassResult = await ExecuteSkillAsync("bass-line-generator", new()
        {
            ["chords"] = ((dynamic)progResult.Data!).progression,
            ["style"] = "root",
            ["tempo"] = parsed.Tempo
        });

        // Build sequence
        var sequence = BuildSequence(parsed, progResult, bassResult);

        // Play it
        _midi.OpenDevice(deviceIndex);
        var player = new SequencePlayer(_midi);
        await player.PlayAsync(sequence, ct);
    }

    private ParsedDescription ParseDescription(string description)
    {
        // Simple parsing - enhance later
        var lower = description.ToLowerInvariant();

        var key = "C";
        var mode = "minor";
        var tempo = 90;
        var bars = 8;
        var style = "ambient";

        // Extract key
        foreach (var k in new[] { "C#", "Db", "D#", "Eb", "F#", "Gb", "G#", "Ab", "A#", "Bb", "C", "D", "E", "F", "G", "A", "B" })
        {
            if (lower.Contains(k.ToLowerInvariant() + " minor") || lower.Contains(k.ToLowerInvariant() + "m"))
            {
                key = k;
                mode = "minor";
                break;
            }
            if (lower.Contains(k.ToLowerInvariant() + " major") || lower.Contains(k.ToLowerInvariant()))
            {
                key = k;
                break;
            }
        }

        // Extract tempo
        var tempoMatch = System.Text.RegularExpressions.Regex.Match(description, @"(\d+)\s*bpm", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (tempoMatch.Success)
            tempo = int.Parse(tempoMatch.Groups[1].Value);

        // Extract style
        if (lower.Contains("ambient") || lower.Contains("drone")) style = "ambient";
        else if (lower.Contains("jazz")) style = "jazz";
        else if (lower.Contains("dark") || lower.Contains("sad")) style = "dark";

        return new ParsedDescription(key, mode, tempo, bars, style);
    }

    private Sequence BuildSequence(ParsedDescription desc, SkillResult prog, SkillResult bass)
    {
        var bassEvents = ((dynamic)bass.Data!).events as List<NoteEvent>;

        var pattern = new Pattern
        {
            Length = 480 * 4 * desc.Bars,
            Events = bassEvents ?? new()
        };

        return new Sequence
        {
            Meta = new MetaData
            {
                Title = $"Generated: {desc.Key} {desc.Mode}",
                Tempo = desc.Tempo,
                Key = $"{desc.Key}{(desc.Mode == "minor" ? "m" : "")}"
            },
            Patterns = new() { ["main"] = pattern },
            Sections = new()
            {
                ["main"] = new Section
                {
                    Length = pattern.Length,
                    Loopable = true,
                    Tracks = new()
                    {
                        new Track { Ch = 1, Sequence = new() { new SequenceEntry { At = 0, Pattern = "main" } } }
                    }
                }
            },
            Arrange = new() { new ArrangeEntry { At = 0, Section = "main" } }
        };
    }
}

public record ParsedDescription(string Key, string Mode, int Tempo, int Bars, string Style);
```

---

### Task 7: CLI Generate Command

```csharp
var generateCommand = new Command("generate", "Generate music from description");
var descArg = new Argument<string>("description", "What to generate");
var genDeviceOpt = new Option<int>("--device", () => 0, "MIDI device index");
genDeviceOpt.AddAlias("-d");

generateCommand.AddArgument(descArg);
generateCommand.AddOption(genDeviceOpt);

generateCommand.SetHandler(async (description, deviceIndex) =>
{
    var service = new SqncRService();

    Console.WriteLine($"Generating: {description}");

    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

    try
    {
        await service.GenerateAndPlayAsync(description, deviceIndex, cts.Token);
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Stopped.");
    }

}, descArg, genDeviceOpt);
rootCommand.AddCommand(generateCommand);
```

---

## Definition of Done

- [ ] Skill framework implemented
- [ ] 6 MVP skills working
- [ ] `sqncr generate "ambient in C minor, 70 BPM" -d 0` plays generated music
- [ ] Generated music uses correct key, mode, tempo
- [ ] Skills are composable (chord-progression feeds bass-line-generator)
- [ ] SqncRService facade working

---

## Demo

```bash
# Generate ambient drone
sqncr generate "ambient drone in C minor, 70 BPM" -d 0
# Output: Generating: ambient drone in C minor, 70 BPM
#         Playing: Generated: C minor
#         Tempo: 70 BPM, Key: Cm
#         ...

# Generate something jazzy
sqncr generate "jazz chords in Bb, 95 BPM" -d 0
```

---

## What's Next

**P4: Transports** - MCP server, CLI polish, maybe REST API

---

**Priority:** P3
**Status:** Waiting for P2
**Updated:** January 29, 2026
