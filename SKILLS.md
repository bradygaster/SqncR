# SqncR Skills Reference

**Complete catalog of all skills available in SqncR**

Skills are discrete, composable, stateless capabilities that can be called by the AI assistant to perform specific tasks. Each skill is focused on a single responsibility and can be combined to create complex musical workflows.

---

## Table of Contents

- [Musical Intelligence Skills](#musical-intelligence-skills)
- [Device Control Skills](#device-control-skills)
- [Analysis Skills](#analysis-skills)
- [Generation Skills](#generation-skills)
- [Transformation Skills](#transformation-skills)
- [Session Management Skills](#session-management-skills)
- [Utility Skills](#utility-skills)

---

## Musical Intelligence Skills

### skill-vibe-to-music
**Translate abstract concepts to musical parameters**

```yaml
name: Vibe-to-Music
description: Converts artistic, emotional, or abstract concepts into concrete musical parameters
inputs:
  concept: string  # "darker", "like Rothko", "film noir", "sunrise"
  current_musical_context: object (optional)
outputs:
  mode: string  # e.g., "phrygian", "lydian"
  harmonic_rhythm: string  # "very_slow", "moderate", "fast"
  chord_extensions: boolean
  density: number  # 0-1
  brightness: number  # -1 to 1
  velocity_offset: number  # -50 to 50
  voicing: string  # "lower", "higher", "wide", "close"
  timbre_suggestions: array
  reasoning: string
```

**Examples:**
```
Input: "darker"
Output: {
  mode: "phrygian",
  brightness: -0.3,
  velocity_offset: -15,
  voicing: "lower",
  reasoning: "Phrygian mode creates darker mood through flatted 2nd"
}

Input: "make it sound like Rothko"
Output: {
  mode: "dorian",
  harmonic_rhythm: "very_slow",
  chord_extensions: true,
  density: 0.2,
  timbre: "warm_blurred",
  reasoning: "Rothko's color fields = sustained spacious chords"
}

Input: "sunrise"
Output: {
  mode: "lydian",
  brightness: 0.5,
  voicing: "higher",
  harmonic_rhythm: "moderate",
  reasoning: "Lydian's raised 4th creates bright, uplifting quality"
}
```

### skill-chord-progression
**Generate harmonically sophisticated chord progressions**

```yaml
name: Chord Progression Generator
description: Creates chord progressions based on music theory principles
inputs:
  key: string  # "C", "A", "F#"
  mode: string  # "major", "minor", "dorian", "phrygian", etc.
  vibe: string  # "dark", "bright", "tense", "resolved", "jazzy"
  bars: number  # number of bars
  complexity: number  # 0-10
  style: string (optional)  # "jazz", "classical", "ambient", "pop"
outputs:
  progression: array  # [{ chord: "Am7", function: "i", bar: 1 }]
  voice_leadings: array  # smooth voice leading between chords
  tension_curve: array  # tension values for each chord
  reasoning: string
```

**Examples:**
```
Input: { key: "A", mode: "minor", vibe: "dark", bars: 4 }
Output: {
  progression: [
    { chord: "Am7", function: "i", bar: 1, tension: 0.2 },
    { chord: "Dm7", function: "iv", bar: 2, tension: 0.4 },
    { chord: "Fmaj7", function: "VI", bar: 3, tension: 0.6 },
    { chord: "E7", function: "V", bar: 4, tension: 0.8 }
  ],
  reasoning: "i-iv-VI-V progression with tension building to dominant"
}
```

### skill-voice-leading
**Optimize chord voicings for smooth transitions**

```yaml
name: Voice Leading Optimizer
description: Creates smooth voice leading between chords
inputs:
  chords: array  # array of chord symbols
  range: [number, number]  # MIDI note range [low, high]
  instrument_type: string  # "piano", "synth", "guitar", "strings"
  previous_voicing: array (optional)  # for continuity
outputs:
  voicings: array  # array of note arrays [[60, 64, 67], ...]
  movement: array  # distance each voice moved
  common_tones: array  # which notes stayed the same
```

**Examples:**
```
Input: {
  chords: ["Cmaj7", "Fmaj7"],
  range: [48, 72],
  instrument_type: "synth"
}
Output: {
  voicings: [
    [60, 64, 67, 71],  // C E G B (Cmaj7)
    [60, 65, 69, 72]   // C F A C (Fmaj7)
  ],
  movement: [0, 1, 2, 1],  // semitones each voice moved
  common_tones: [60]  // C is common tone
}
```

### skill-scale-selector
**Select appropriate scale for given musical context**

```yaml
name: Scale Selector
description: Chooses optimal scale based on mood, genre, and context
inputs:
  key: string
  mood: string  # "happy", "sad", "mysterious", "tense"
  genre: string (optional)  # "jazz", "ambient", "rock"
  context: string (optional)  # additional context
outputs:
  scale: string  # scale name
  notes: array  # scale notes as MIDI numbers
  characteristics: object
  reasoning: string
```

### skill-harmonic-analysis
**Analyze harmonic relationships and functions**

```yaml
name: Harmonic Analyzer
description: Analyzes chords and their relationships
inputs:
  chords: array  # chord progression
  key: string
outputs:
  functions: array  # roman numeral analysis
  borrowed_chords: array  # chords from outside key
  modulations: array  # key changes detected
  tension_points: array
```

---

## Device Control Skills

### skill-list-devices
**List all available MIDI devices**

```yaml
name: List MIDI Devices
description: Scans and returns all connected MIDI devices
inputs: none
outputs:
  devices: array  # array of device objects
```

**Output Format:**
```json
{
  "devices": [
    {
      "id": "polyend-synth-001",
      "name": "Polyend Synth MIDI 1",
      "profile": "polyend-synth",
      "type": "synth",
      "channels": [1, 2, 3],
      "polyphony": 8,
      "status": "connected"
    }
  ]
}
```

### skill-device-selector
**Select optimal device for musical role**

```yaml
name: Device Selector
description: Chooses best device based on role and characteristics
inputs:
  role: string  # "bass", "chords", "pads", "lead", "drums", "texture", "fx"
  characteristics: array  # ["warm", "analog", "bright", "digital"]
  available_devices: array
  exclude_devices: array (optional)
outputs:
  selected_device: object
  channel: number
  reasoning: string
```

**Examples:**
```
Input: {
  role: "bass",
  characteristics: ["warm", "analog", "fat"]
}
Output: {
  selected_device: "moog-mother32-001",
  channel: 4,
  reasoning: "Moog Mother-32's analog warmth perfect for bass foundation"
}

Input: {
  role: "texture",
  characteristics: ["glitchy", "experimental"]
}
Output: {
  selected_device: "polyend-mess-001",
  channel: 5,
  reasoning: "MESS FX sequencer ideal for glitchy textural elements"
}
```

### skill-send-midi
**Send MIDI messages to devices**

```yaml
name: Send MIDI Message
description: Sends MIDI messages with OpenTelemetry tracing
inputs:
  device: string
  channel: number
  message_type: string  # "note_on", "note_off", "cc", "program_change"
  data: object
outputs:
  success: boolean
  latency_ms: number
```

### skill-configure-midi-routing
**Set up MIDI routing between devices**

```yaml
name: Configure MIDI Routing
description: Routes MIDI between devices for complex setups
inputs:
  source_device: string
  destination_device: string
  channel_map: object  # { 1: 2, 2: 3 } maps source ch to dest ch
  filter: object (optional)  # filter certain message types
outputs:
  routing_id: string
  configuration: object
```

---

## Analysis Skills

### skill-analyze-song
**Extract musical data from song descriptions or audio**

```yaml
name: Song Analyzer
description: Identifies songs and extracts musical parameters
inputs:
  song_description: string  # "that Cream song from The Breakfast Club"
  reference_audio: string (optional)  # URL or file path
  analysis_depth: string  # "basic", "detailed", "complete"
outputs:
  song_title: string
  artist: string
  key: string
  tempo: number
  time_signature: string
  chord_progression: array
  melody_notes: array (optional)
  vibe_tags: array
  spotify_url: string (optional)
  reasoning: string
```

**Examples:**
```
Input: "that Cream song from The Breakfast Club"
Output: {
  song_title: "I'm So Glad",
  artist: "Cream",
  key: "E",
  mode: "mixolydian/blues",
  tempo: 140,
  time_signature: "4/4",
  feel: "shuffle",
  main_riff: [64, 67, 69, 70, 71, 74],  // E blues scale
  chord_progression: ["E7"],
  vibe_tags: ["driving", "raw", "blues", "garage_rock"],
  reasoning: "Modal blues rock with shuffle feel and E7 vamp"
}
```

### skill-detect-key
**Detect key from MIDI input or audio**

```yaml
name: Key Detector
description: Analyzes notes to determine key and mode
inputs:
  notes: array  # MIDI note numbers
  duration_seconds: number (optional)
outputs:
  key: string
  mode: string
  confidence: number  # 0-1
  alternatives: array  # other possible keys
```

### skill-detect-tempo
**Detect tempo from MIDI input or audio**

```yaml
name: Tempo Detector
description: Analyzes timing to determine tempo
inputs:
  midi_events: array
  time_window_seconds: number
outputs:
  tempo: number  # BPM
  confidence: number
  time_signature: string (optional)
```

### skill-analyze-harmony
**Analyze harmonic content of MIDI input**

```yaml
name: Harmony Analyzer
description: Real-time harmonic analysis of playing
inputs:
  notes: array  # currently playing notes
  context: object  # previous harmonic context
outputs:
  current_chord: string
  chord_quality: string
  extensions: array
  function: string  # "tonic", "dominant", "subdominant"
  suggestions: array  # complementary notes
```

---

## Generation Skills

### skill-polyrhythm-generator
**Create complex polyrhythmic patterns**

```yaml
name: Polyrhythm Generator
description: Generates mathematically interesting rhythmic patterns
inputs:
  base_tempo: number
  time_signature: string
  complexity: number  # 2-7 (e.g., 3 against 4)
  instruments: array
  duration_bars: number
outputs:
  patterns: array  # rhythmic sequences per instrument
  sync_points: array  # where patterns align
  lcm: number  # least common multiple (cycle length)
```

**Examples:**
```
Input: {
  base_tempo: 87,
  time_signature: "4/4",
  complexity: 3,  // 3 against 4
  instruments: ["bass", "chords"]
}
Output: {
  patterns: {
    bass: [0, 1000, 2000, 3000],  // quarter notes
    chords: [0, 1333, 2666]  // dotted quarter notes (3 in 4 beats)
  },
  sync_points: [0, 4000],  // patterns align every 4 beats
  lcm: 12  // pattern repeats every 12 eighth notes
}
```

### skill-arpeggio-generator
**Generate arpeggiated patterns**

```yaml
name: Arpeggio Generator
description: Creates arpeggio patterns from chords
inputs:
  chord: string
  pattern: string  # "up", "down", "up-down", "random", "as-played"
  octaves: number
  note_duration: string  # "16th", "8th", "triplet"
  velocity_curve: string  # "flat", "crescendo", "diminuendo"
outputs:
  notes: array  # sequence of MIDI notes with timing
  pattern_length: number  # in beats
```

### skill-bass-line-generator
**Generate walking bass lines**

```yaml
name: Bass Line Generator
description: Creates bass lines that follow chord progressions
inputs:
  chord_progression: array
  style: string  # "walking", "pedal", "rhythmic", "sparse"
  complexity: number  # 0-10
  range: [number, number]  # MIDI note range
outputs:
  bass_line: array  # notes with timing and duration
  approach_notes: array  # chromatic approaches used
```

### skill-melody-generator
**Generate melodic lines**

```yaml
name: Melody Generator
description: Creates melodies based on scale and style
inputs:
  scale: array  # scale notes
  chord_progression: array
  style: string  # "stepwise", "leaps", "motivic", "random"
  phrase_length_bars: number
  range: [number, number]
outputs:
  melody: array  # notes with timing, duration, velocity
  motifs: array  # recurring melodic fragments
  contour: string  # "ascending", "descending", "arch", "wave"
```

### skill-rhythm-generator
**Generate rhythmic patterns**

```yaml
name: Rhythm Generator
description: Creates rhythmic patterns for drums/percussion
inputs:
  style: string  # "straight", "swing", "shuffle", "syncopated"
  density: number  # 0-1
  tempo: number
  time_signature: string
  bars: number
outputs:
  pattern: array  # hit timing and velocity
  groove: string  # groove classification
```

---

## Transformation Skills

### skill-transpose
**Transpose notes or progressions**

```yaml
name: Transposer
description: Transposes musical content to different key
inputs:
  notes: array  # MIDI notes or chord symbols
  semitones: number  # -12 to 12
  maintain_octave: boolean
outputs:
  transposed: array
  new_key: string (optional)
```

### skill-invert-chord
**Invert chord voicings**

```yaml
name: Chord Inverter
description: Creates different inversions of chords
inputs:
  chord: string
  inversion: number  # 0 (root), 1 (first), 2 (second)
  octave: number
outputs:
  notes: array
  bass_note: number
```

### skill-quantize
**Quantize timing to grid**

```yaml
name: Quantizer
description: Aligns notes to rhythmic grid
inputs:
  notes: array  # notes with timing
  grid: string  # "16th", "8th", "quarter"
  strength: number  # 0-1 (0 = no quantize, 1 = hard quantize)
  swing: number  # 0-100%
outputs:
  quantized_notes: array
```

### skill-humanize
**Add human-like timing variations**

```yaml
name: Humanizer
description: Adds natural timing and velocity variations
inputs:
  notes: array
  timing_variance: number  # milliseconds
  velocity_variance: number  # 0-127
  style: string  # "subtle", "moderate", "drunk"
outputs:
  humanized_notes: array
```

### skill-modal-interchange
**Borrow chords from parallel modes**

```yaml
name: Modal Interchange
description: Suggests chords borrowed from parallel keys/modes
inputs:
  key: string
  mode: string
  target_mood: string  # "darker", "brighter", "more_complex"
outputs:
  borrowed_chords: array
  from_mode: string
  usage_suggestions: array
```

---

## Session Management Skills

### skill-save-session
**Save current session state**

```yaml
name: Save Session
description: Persists current musical session
inputs:
  session_name: string
  include_history: boolean
outputs:
  session_id: string
  file_path: string
  timestamp: string
```

### skill-load-session
**Load previous session**

```yaml
name: Load Session
description: Restores saved musical session
inputs:
  session_id: string
outputs:
  session: object
  devices: array
  musical_context: object
```

### skill-list-sessions
**List all saved sessions**

```yaml
name: List Sessions
description: Returns all available saved sessions
inputs: none
outputs:
  sessions: array  # session metadata
```

### skill-export-midi
**Export generation as MIDI file**

```yaml
name: MIDI File Exporter
description: Exports current or generated music as MIDI file
inputs:
  session_id: string (optional)
  file_path: string
  include_devices: array (optional)
  start_bar: number (optional)
  end_bar: number (optional)
outputs:
  file_path: string
  duration_seconds: number
  track_count: number
```

---

## Utility Skills

### skill-configure-lights
**Configure MIDI-controlled lighting**

```yaml
name: Light Show Configurator
description: Maps musical events to MIDI lighting control
inputs:
  tempo: number
  intensity: number  # 0-10
  color_mapping: object  # harmonic events → colors
  sync_mode: string  # "beat", "chord_change", "dynamic"
outputs:
  light_configuration: object
  cc_mappings: array
```

### skill-calculate-interval
**Calculate interval between notes**

```yaml
name: Interval Calculator
description: Determines musical interval between two notes
inputs:
  note1: number  # MIDI note
  note2: number  # MIDI note
outputs:
  interval: string  # "perfect fifth", "minor third"
  semitones: number
  quality: string  # "perfect", "major", "minor", "augmented", "diminished"
```

### skill-note-to-frequency
**Convert MIDI note to frequency**

```yaml
name: Note to Frequency Converter
description: Converts MIDI note numbers to Hz
inputs:
  note: number  # MIDI note (0-127)
  tuning: number  # A4 frequency (default 440Hz)
outputs:
  frequency: number  # Hz
  note_name: string
```

### skill-tempo-tap
**Calculate tempo from tap input**

```yaml
name: Tempo Tap
description: Determines tempo from user tapping
inputs:
  tap_timestamps: array  # millisecond timestamps
  min_taps: number  # minimum taps to calculate (default 4)
outputs:
  tempo: number  # BPM
  confidence: number
  variance: number
```

---

## Skill Combinations

Skills are designed to be composed together for complex operations:

### Example: "Create Jazz Comp for Solo"
```
1. skill-detect-key (from user's solo input)
2. skill-chord-progression (jazz style, detected key)
3. skill-voice-leading (jazz voicings)
4. skill-rhythm-generator (swing feel)
5. skill-device-selector (piano for comping)
6. skill-send-midi (play the comp)
```

### Example: "Build Tension Over 2 Minutes"
```
1. skill-vibe-to-music ("building tension")
2. skill-chord-progression (increasing complexity)
3. skill-polyrhythm-generator (add layers)
4. skill-device-selector (orchestrate across devices)
5. skill-humanize (natural feel)
6. skill-configure-lights (visual tension)
```

### Example: "Recreate That Song's Vibe"
```
1. skill-analyze-song (identify and extract)
2. skill-scale-selector (matching mood)
3. skill-chord-progression (similar harmonic movement)
4. skill-bass-line-generator (similar style)
5. skill-rhythm-generator (matching feel)
```

---

## Implementation Notes

**Skill Structure (C#):**
```csharp
public interface ISkill
{
    string Name { get; }
    string Description { get; }
    Task<SkillResult> ExecuteAsync(SkillInput input, CancellationToken cancellationToken);
}

public abstract class SkillBase : ISkill
{
    protected readonly ActivitySource ActivitySource;
    protected readonly ILogger Logger;
    
    // OpenTelemetry tracing built-in
    public async Task<SkillResult> ExecuteAsync(SkillInput input, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity($"Skill.{Name}");
        activity?.SetTag("skill.name", Name);
        activity?.SetTag("skill.input", JsonSerializer.Serialize(input));
        
        try
        {
            var result = await ExecuteInternalAsync(input, cancellationToken);
            activity?.SetTag("skill.success", true);
            return result;
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            throw;
        }
    }
    
    protected abstract Task<SkillResult> ExecuteInternalAsync(SkillInput input, CancellationToken cancellationToken);
}
```

**Registering Skills:**
```csharp
// Startup.cs or Program.cs
services.AddSingleton<ISkill, VibeToMusicSkill>();
services.AddSingleton<ISkill, ChordProgressionSkill>();
services.AddSingleton<ISkill, DeviceSelectorSkill>();
services.AddSingleton<ISkill, AnalyzeSongSkill>();
// ... etc

services.AddSingleton<ISkillRegistry, SkillRegistry>();
```

**Calling from MCP Server:**
```csharp
var skill = skillRegistry.GetSkill("vibe-to-music");
var result = await skill.ExecuteAsync(new SkillInput
{
    Parameters = new Dictionary<string, object>
    {
        ["concept"] = "darker",
        ["current_musical_context"] = currentContext
    }
});
```

---

## Future Skills

**Planned for future development:**
- `skill-style-transfer` - Apply style of one piece to another
- `skill-auto-mastering` - Adjust MIDI velocities for balanced mix
- `skill-chord-substitution` - Suggest alternate chord choices
- `skill-reharmonization` - Reharmonize existing melody
- `skill-counterpoint-generator` - Generate contrapuntal lines
- `skill-orchestration-suggester` - Suggest device/timbre assignments
- `skill-form-analyzer` - Detect song structure (verse, chorus, bridge)
- `skill-groove-extractor` - Extract rhythmic feel from audio
- `skill-scale-degrees-to-notes` - Convert scale degree notation to notes
- `skill-lyrics-to-rhythm` - Generate rhythm from lyric syllables

---

## Contributing New Skills

See [CONTRIBUTING.md](CONTRIBUTING.md) for:
1. Skill design principles
2. Testing requirements
3. Documentation standards
4. OpenTelemetry integration
5. PR process

**Skill Naming Convention:** `skill-[action]-[target]`
- ✅ `skill-generate-melody`
- ✅ `skill-analyze-harmony`
- ✅ `skill-transpose-notes`
- ❌ `melody-skill`
- ❌ `generate_melody`

---

## See Also

- [AGENTIC_ARCHITECTURE.md](AGENTIC_ARCHITECTURE.md) - How skills fit into the architecture
- [MUSIC_THEORY.md](MUSIC_THEORY.md) - Music theory concepts used by skills
- [OBSERVABILITY.md](OBSERVABILITY.md) - How skills are traced and monitored
