# SqncR - Generative MIDI Music Creation

## Core Vision
SqncR is a MIDI-based generative music application that empowers musicians to create organic, real-time music by describing their intentions to an AI, while maintaining complete creative freedom and control over their physical/virtual instruments.

## Key Differentiators
- **Instrument-First**: Built around what you already have (hardware synths, software instruments, MIDI controllers)
- **Description-Driven**: Tell the AI what you want in natural language, not just preset patterns
- **Generative, Not Programmatic**: Creates evolving, organic music rather than loops or rigid sequences
- **Real-Time Collaboration**: Human and AI working together in the moment
- **Genre-Agnostic**: From ambient soundscapes to jazz to experimental electronic

## Core Principles

### 1. **Setup Phase: Know Your Instruments**
- User connects MIDI devices and maps them to tones/patches
- Describe each instrument: "This is my Moog Sub 37 with a warm bass patch" or "This is Serum with a glassy pad"
- AI learns the sonic palette available
- Store instrument profiles for quick recall

### 2. **Context-Aware Generation**
User describes what they want:
- "Give me a slow-evolving ambient piece in Dorian, keep it sparse"
- "Create a rhythmic pattern that feels like rain on a roof"
- "Build tension over 2 minutes using the brass patch"
- "Improvise around what I'm playing, stay in the same key"

### 3. **Real-Time Adaptation**
- AI listens to what the user plays
- Responds to tempo, key, mood changes
- Can follow, complement, or contrast with human input
- User can adjust generation parameters on the fly

### 4. **Organic Generation Techniques**
- **Melodic Evolution**: Seeds that grow and mutate
- **Harmonic Awareness**: Understand tension/release, voice leading
- **Rhythmic Variation**: Not quantized grids, but human-feeling timing
- **Texture Building**: Layering, dynamics, space
- **Conditional Generation**: "If I play staccato, respond with legato"

## Technical Concepts

### MIDI Architecture
```
[MIDI Devices] → [SqncR Core] → [Virtual MIDI Outputs] → [DAW/Synths]
                      ↓
                 [AI Engine]
                      ↓
              [Context Manager]
```

### Components
1. **MIDI Router**: Handle multiple inputs/outputs
2. **Instrument Registry**: Store instrument descriptions and characteristics
3. **Context Engine**: Understand current musical state (key, tempo, mood, intensity)
4. **Generation Engine**: Create MIDI based on context and user descriptions
5. **Learning System**: Remember user preferences and patterns
6. **Real-Time Analyzer**: Listen and adapt to live input

### User Control Levels
- **Macro**: "Make it more intense" / "Simplify"
- **Structural**: "Add a countermelody" / "Drop out the bass"
- **Detailed**: "Play arpeggios in the upper register"
- **Manual Override**: Take full control any time, AI supports in background

## Feature Ideas

### Phase 1: Foundation
- MIDI device detection and routing
- Basic instrument profiling (name, type, character)
- Simple generative patterns from text descriptions
- Key/scale awareness
- Tempo sync

### Phase 2: Intelligence
- Natural language understanding for musical concepts
- Real-time listening and adaptation
- Harmonic analysis and generation
- Pattern evolution and variation
- Save/recall "scenes" or generative states

### Phase 3: Collaboration
- AI responds to human playing in real-time
- Multi-instrument coordination
- Dynamic arrangement (intro, build, breakdown, etc.)
- Style transfer ("play this melody but in a jazz style")
- Session memory (remember what worked)

### Phase 4: Advanced
- Machine learning on user's playing style
- Collaborative composition (human sketches, AI develops)
- Export MIDI for further editing
- Integration with popular DAWs
- Community sharing of generative "recipes"

## Example Workflows

### Workflow 1: Solo Ambient Session
1. Connect Moog (bass), Prophet (pads), Modular (textures)
2. Tell SqncR: "Create a meditative ambient piece, 60 BPM, focus on space and subtle movement"
3. SqncR generates evolving bass drones, sparse pad swells, occasional textural elements
4. User plays along, SqncR adapts to maintain cohesion
5. User: "Introduce more rhythmic elements slowly"
6. SqncR gradually adds subtle rhythmic patterns

### Workflow 2: Jazz Exploration
1. MIDI keyboard mapped to piano VST
2. "Let's explore modal jazz, something like So What, you comp chords while I solo"
3. SqncR generates jazz chord voicings with appropriate rhythm
4. User solos, AI adjusts chord density and walking bass patterns
5. User: "Take us somewhere unexpected harmonically"
6. SqncR introduces modal interchange or reharmonization

### Workflow 3: Electronic Build
1. Multiple soft synths routed
2. "Build a techno-inspired piece from minimal to complex over 5 minutes"
3. SqncR starts with simple kick pattern
4. Gradually introduces bass, hi-hats, melodic elements
5. User can steer: "More acid bass" or "Pull back on the high end"
6. Reaches peak, then SqncR can be told to "Break it down"

## Design Philosophy
- **Transparency**: Always show what the AI is doing and why
- **Interruptibility**: User can override or stop generation instantly
- **No Black Box**: Explain decisions in musical terms
- **Respect Creativity**: AI is a collaborator, not a replacement
- **Flexibility**: Works for experimental, traditional, electronic, acoustic contexts

## Technical Considerations
- Low-latency MIDI handling (< 10ms)
- Offline-capable AI (local inference for real-time)
- Scalable from laptop to studio setup
- Cross-platform (Windows, Mac, Linux)
- Plugin architecture for extensibility

## Next Steps
- Define minimum viable feature set
- Choose tech stack (Electron/Tauri, MIDI library, AI framework)
- Design UI/UX for instrument setup and live control
- Prototype basic generative engine
- Test with real musicians in various genres
