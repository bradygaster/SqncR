### Rhythm types live in SqncR.Core, not SqncR.Theory
**By:** Rainicorn
**What:** All rhythm/beat/sequencer types are in `src/SqncR.Core/Rhythm/`. This is intentional: rhythm is engine-level (how to play), not theory-level (what to play). The boundary is: BeatPattern, StepSequencer, SwingProfile, DrumMap, PatternLibrary → Core. Scales, chords, key, harmony → Theory. Simon's Theory work and Rainicorn's rhythm work are parallel and decoupled.
**Why:** Rhythm drives the generation loop directly — it produces tick-timed events that feed into MIDI output. Theory informs *which notes* to play but not *when* or *how hard*. Keeping them separate avoids circular dependencies and makes it clear where to put new code. If it grooves, it's Core. If it resolves, it's Theory.

### Rhythm types produce SequencerEvents, not MIDI directly
**By:** Rainicorn
**What:** The rhythm subsystem outputs `SequencerEvent` records (tick, step index, drum voice, velocity, probability). It never imports or references MIDI types. The MIDI layer is responsible for mapping DrumVoice → MIDI note (via DrumMap) and sending NoteOn/NoteOff messages. This keeps rhythm logic testable without any MIDI dependency.
**Why:** Decoupling rhythm from MIDI means the same patterns work for software synths (Sonic Pi, VCV Rack) that don't use MIDI note numbers directly. It also makes unit testing trivial — no MIDI ports needed.

### PPQ=480 is the project standard for tick-based timing
**By:** Rainicorn
**What:** The StepSequencer defaults to 480 ticks per quarter note, matching `MetaData.Tpq` in Sequence.cs. All tick calculations assume this unless overridden. At 16-step resolution in 4/4, each step = 120 ticks, each measure = 1920 ticks.
**Why:** Consistency across the generation loop. If the sequencer and the sequence parser disagree on PPQ, timing will drift. 480 is the de facto MIDI standard and matches what's already in the codebase.

### FrozenDictionary needs explicit comparer for case-insensitive lookup
**By:** Rainicorn
**What:** When converting a `Dictionary<string, T>` with `StringComparer.OrdinalIgnoreCase` to `FrozenDictionary`, you must pass the comparer to `.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase)`. The comparer from the source dictionary is not automatically carried over.
**Why:** Discovered when PatternLibrary.Get("HOUSE") failed despite the source dictionary using case-insensitive comparison. This is a .NET gotcha worth remembering.
