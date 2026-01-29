# SqncR Music Theory & Conversational Design

## Music Theory Foundation

### Core Concepts SqncR Must Understand

**Scales & Modes**
```
Major Modes:
- Ionian (major) - bright, happy
- Lydian - bright, dreamy, floating
- Mixolydian - bright but grounded, bluesy

Minor Modes:
- Aeolian (natural minor) - melancholic, introspective
- Dorian - minor but hopeful, jazzy
- Phrygian - dark, exotic, Spanish
- Locrian - unstable, dissonant, tense
```

**Circle of Fifths / Harmonic Relationships**
```
Close relationships (smooth transitions):
- I - IV - V (tonic - subdominant - dominant)
- Relative major/minor (C major ↔ A minor)
- Parallel major/minor (C major ↔ C minor)

Distant relationships (dramatic shifts):
- Tritone substitution (creates tension)
- Chromatic mediants (cinematic, surprising)
- Modal interchange (borrowing from parallel modes)
```

**Chord Function & Emotional Value**
```
Stability → Tension → Release:
- I (tonic) = home, rest, resolution
- ii, iii, vi = color chords, movement
- IV = subdominant, anticipation
- V, vii° = dominant, tension, wants to resolve
- bVII, bVI = borrowed chords, darkening

Emotional Qualities:
- maj7 = sophisticated, jazzy, open
- min7 = introspective, smooth
- sus chords = ambiguous, floating
- add9/add11 = spacious, modern
- dim/aug = unstable, transitional
```

**Rhythmic Concepts**
```
Polyrhythms:
- 3 against 4 (hemiola)
- 5 against 4 
- Irregular meters (5/4, 7/8)

Density:
- Sparse = space, meditation
- Dense = energy, complexity
- Syncopation = groove, surprise
```

---

## Device Profile: Polyend Synth

**MIDI Implementation:**
- 8-voice polyphony
- 3 independent synth engines (simultaneously)
- Each engine assignable to different MIDI channel
- Supports channel & polyphonic aftertouch
- Built-in sequencer/arpeggiator per engine
- Multiple synthesis types: wavetable, granular, FM, VA, physical modeling

**Typical SqncR Usage:**
```json
{
  "device": "Polyend Synth",
  "voices": 8,
  "engines": [
    {
      "engine": 1,
      "channel": 1,
      "role": "bass",
      "synthesis": "virtual_analog",
      "range": [24, 48],
      "character": "warm, sub, foundational"
    },
    {
      "engine": 2,
      "channel": 2,
      "role": "chords",
      "synthesis": "wavetable",
      "range": [48, 72],
      "character": "evolving, harmonic, textural"
    },
    {
      "engine": 3,
      "channel": 3,
      "role": "pads",
      "synthesis": "granular",
      "range": [60, 84],
      "character": "atmospheric, sustained, spacious"
    }
  ]
}
```

---

## Conversational Workflows: From Simple to Complex

### Session 1: Initial Generation

**User:** `"ambient but rhythmic, 87bpm. polyend synth bass is on channel 1, chords on channel 2, and pads on channel 3."`

**SqncR Interprets:**
```
Genre: Ambient
Tempo: 87 BPM
Time Signature: 4/4 (assumed)
Key: Not specified → ask or default to Amin
Intensity: 3/10 (ambient default)

Instruments:
- Polyend Engine 1 (Ch 1): Bass role
- Polyend Engine 2 (Ch 2): Chords role  
- Polyend Engine 3 (Ch 3): Pads role

Generation Plan:
Bass: Quarter note pulse, octave pattern, subtle variation
Chords: Sustained whole/half notes, Amin progression (i - iv - VI - III)
Pads: Long sustained notes, high register, evolving textures
Rhythm: "Rhythmic" means not pure drone - add subtle timing variations
```

**SqncR Response:**
"Starting ambient generation at 87 BPM in A minor. Bass on channel 1 (foundational pulse), chords on channel 2 (slow harmonic movement), pads on channel 3 (atmospheric layer). Let me know if you want a different key or mode."

*Music starts playing through Polyend*

---

### Session 2: Modification Commands

**User (while music plays):** `"darker"`

**SqncR Interprets:**
```
"Darker" musical translation:
- Shift to Phrygian mode (flatten 2nd scale degree)
- Lower chord voicings
- Add more minor 2nd intervals (dissonance)
- Reduce upper harmonic content in pads
- Slower attack on chords
- Lower velocity (60-80 instead of 80-100)
```

**SqncR Response:**
"Shifting to A Phrygian, lowering voicings and adding minor 2nd tensions."

*Music shifts darker without stopping*

---

**User:** `"more polyrhythms"`

**SqncR Interprets:**
```
Add polyrhythmic layers:
- Keep bass in 4/4
- Add chord pattern in 3/4 feel (3 against 4)
- Introduce hi-register melodic figure in 5/16
- Maintain 87 BPM master tempo
- Keep overall "ambient" intensity (subtle polyrhythms, not aggressive)
```

**SqncR Response:**
"Adding 3-against-4 feel in chords and a 5-note cycle in the upper register."

*Complexity increases, polyrhythmic texture emerges*

---

**User:** `"make it sound like Rothko makes you feel"`

**SqncR Interprets (Abstract → Musical):**
```
Rothko = Color field paintings
Emotional qualities:
- Contemplative, meditative
- Large color blocks = sustained, spacious chords
- Blurred edges = slow attack/release, reverb
- Emotional depth = harmonic richness (7ths, 9ths, 11ths)
- Subtle shifts = modal interchange, slow harmonic rhythm
- Warmth vs darkness in same piece = major/minor ambiguity

Musical translation:
- Extremely slow chord changes (every 8-16 bars)
- Rich extended chords (min9, maj7#11, sus4add9)
- Very long attack/release times
- Minimal melody, focus on texture and timbre
- Dynamic swells (pp to mf, never harsh)
- Occasional chromatic voice leading (color shifts)
- Phrygian or Dorian mode for depth + warmth
```

**SqncR Response:**
"Channeling Rothko - slowing harmonic rhythm dramatically, adding extended chord voicings, focusing on color and texture with minimal movement. Shifting to Dorian for that warm/melancholic blend."

*Music becomes more spacious, contemplative, with rich sustained tones*

---

### Session 3: Interactive Layering

**User:** `"I'm going to play some chords on my keystep, listen and respond underneath"`

**SqncR:**
```
Activate MIDI input monitoring
Detect: Key, chords being played
Analyze: Voicing, rhythm, dynamics
Response strategy:
- Bass should support root movement
- Pads fill harmonic gaps (avoid doubling user's notes)
- If user plays staccato → generate legato response
- If user plays upper register → bass stays low
- Real-time key detection
```

**User plays:** `Cmaj7 - Fmaj7 - Dm7 - G7` (I - IV - ii - V in C major)

**SqncR Interprets:**
```
Key detected: C major
Progression: I - IV - ii - V (jazz/pop standard)
Tempo: Detected from user's playing (~92 BPM)

Bass generation:
- Root notes with walking approach (C → F with passing tones)
- Quarter note pulse maintaining user's tempo

Pad generation:
- Fill 5ths and 9ths that user isn't playing
- Sustained notes, fade in/out smoothly
- Stay in middle register (don't compete with user)
```

**SqncR Response:**
"Detected C major, I-IV-ii-V progression. Generating walking bass and complementary pad fills."

*Bass and pads play, supporting user's chord performance*

---

### Session 4: Structural Evolution

**User:** `"build this over 5 minutes - start minimal, peak at 3 minutes, then settle"`

**SqncR Interprets:**
```
Structural arc:
[0:00 - 1:00] Minimal (intensity 2/10)
  - Bass only, whole notes
  - Introduce pads at 0:30

[1:00 - 2:00] Building (intensity 4/10)
  - Add chord layer
  - Increase rhythmic density in bass
  - Introduce subtle melodic fragments

[2:00 - 3:00] Rising (intensity 6-8/10)
  - All layers active
  - Add polyrhythmic elements
  - Increase harmonic rhythm
  - Peak: most complex voicings, densest rhythm

[3:00 - 4:00] Settling (intensity 6→3/10)
  - Gradually remove polyrhythms
  - Simplify chord voicings
  - Return to whole/half note rhythm

[4:00 - 5:00] Resolution (intensity 2/10)
  - Pads only
  - Return to tonic
  - Fade dynamics
```

**SqncR Response:**
"Starting minimal with bass drone. Will build layers and complexity, peaking at 3:00, then settling back to spacious pads. Total duration: 5 minutes."

*Music evolves automatically over time, user can code without interruption*

---

### Session 5: Genre/Style Shift

**User:** `"shift this to something more like Jon Hopkins"`

**SqncR Interprets:**
```
Jon Hopkins characteristics:
- Intricate polyrhythms and micro-timing
- Blend of organic and electronic textures
- Builds from minimal to maximal
- Heavy use of filters and modulation
- Glitchy, granular elements
- Emotional but driving

Musical translation:
- Add rhythmic subdivision (16th note hi-hats, percussion)
- Introduce filter sweeps on sustained chords
- Stutter/glitch effects on bass (repeat notes with timing variation)
- More forward momentum (not just ambient)
- Keep harmonic sophistication but add rhythmic drive
- Tempo might need to increase slightly (90-95 BPM)
```

**SqncR Response:**
"Transitioning to Jon Hopkins style - adding intricate rhythmic layers, filter modulation, and more forward drive. Increasing tempo to 92 BPM."

*Music shifts from pure ambient to intricate electronic with more momentum*

---

## Key SqncR Intelligence Features

### 1. Music Theory Engine
- Scale/mode knowledge
- Chord progression generation
- Voice leading rules
- Tension/release curves
- Harmonic function understanding

### 2. Natural Language → Music Translation
- Map adjectives to parameters:
  - "darker" → mode shift, lower voicings, less brightness
  - "spacious" → lower density, longer notes, more reverb
  - "urgent" → faster tempo, shorter notes, more dissonance
  
- Understand artistic references:
  - "Like Rothko" → analyze artistic qualities → musical equivalents
  - "Jon Hopkins" → recall musical characteristics
  - "Film noir" → harmonic minor, chromatic lines, tension

### 3. Context Awareness
- Remember what's currently playing
- Understand user's intent (modify vs restart vs layer)
- Track harmonic position (where we are in progression)
- Maintain musical coherence through changes

### 4. Device Intelligence
- Know capabilities of each instrument
- Respect MIDI channel routing
- Understand voice allocation (8 voices across 3 engines)
- Optimize note distribution to avoid voice stealing

### 5. Real-Time Adaptation
- Listen to user input
- Detect key/tempo/style
- Respond complementarily
- Adjust when user changes direction

---

## Technical Implementation Notes

### Music Theory Library Structure
```typescript
const scales = {
  major: [0, 2, 4, 5, 7, 9, 11],
  minor: [0, 2, 3, 5, 7, 8, 10],
  dorian: [0, 2, 3, 5, 7, 9, 10],
  phrygian: [0, 1, 3, 5, 7, 8, 10],
  // ... all modes
}

const chordQualities = {
  maj7: [0, 4, 7, 11],
  min7: [0, 3, 7, 10],
  dom7: [0, 4, 7, 10],
  sus4: [0, 5, 7],
  // ... extended chords
}

function getProgression(key, mode, vibe) {
  // Generate chord progression based on:
  // - Functional harmony
  // - Emotional target
  // - Complexity level
}

function voiceChord(chord, previousVoicing, range) {
  // Apply voice leading rules
  // Keep common tones
  // Minimize movement
  // Respect range constraints
}
```

### Parameter Mapping
```typescript
const vibeToParams = {
  darker: {
    mode: 'shift_to_minor_or_phrygian',
    brightness: -0.3,
    voicing: 'lower',
    velocity: -15,
    intervals: 'favor_minor_2nd'
  },
  brighter: {
    mode: 'shift_to_major_or_lydian',
    brightness: +0.3,
    voicing: 'higher',
    velocity: +15,
    intervals: 'favor_major_3rd'
  },
  spacious: {
    density: -0.5,
    note_duration: +0.4,
    reverb: +0.3,
    attack: +0.2
  },
  rothko: {
    harmonic_rhythm: 'very_slow',
    chord_extensions: true,
    dynamic_swells: true,
    timbre: 'warm_blurred'
  }
}
```

### Polyend-Specific Optimizations
```typescript
const polyendVoiceAllocation = {
  totalVoices: 8,
  engines: [
    { id: 1, channel: 1, maxVoices: 3, role: 'bass' },
    { id: 2, channel: 2, maxVoices: 3, role: 'chords' },
    { id: 3, channel: 3, maxVoices: 2, role: 'pads' }
  ]
}

function allocateNotes(notes, engine) {
  // Respect voice limits per engine
  // Prioritize lower notes for bass
  // Distribute chord notes efficiently
  // Avoid voice stealing when possible
}
```

---

## Example MCP Tool Call Flow

**User says:** "make it darker"

**Claude/Copilot calls:**
```json
{
  "tool": "modify_generation",
  "arguments": {
    "instruction": "make it darker",
    "interpretation": {
      "mode_shift": "phrygian",
      "voicing_adjustment": "lower",
      "brightness": -0.3,
      "velocity_offset": -15
    }
  }
}
```

**SqncR executes:**
1. Analyzes current musical state
2. Applies theory-based transformations
3. Updates MIDI generation parameters
4. Transitions smoothly (no sudden stops)
5. Returns: "Shifted to Phrygian, lowered voicings"

---

## Next Steps for Implementation

1. **Build music theory engine** (scales, chords, progressions)
2. **Create parameter mapping system** (adjectives → musical changes)
3. **Implement Polyend device profile** (channel routing, voice management)
4. **Design smooth transition logic** (don't just stop/start)
5. **Add real-time MIDI input analysis** (detect key/chords from user)
6. **Build reference database** (artists, genres, vibes → musical characteristics)
