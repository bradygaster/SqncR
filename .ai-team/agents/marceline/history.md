# Project Context

- **Owner:** Brady (bradyg@microsoft.com)
- **Project:** SqncR — AI-native generative music system for MIDI devices. Controls hardware/software synths through natural language conversation via MCP. Ambitions include VCV Rack 2 integration and live streaming music generation.
- **Stack:** C#/.NET, MCP (Model Context Protocol), MIDI, potential VCV Rack 2 integration
- **Created:** 2026-02-13

## Learnings

### VCV Rack 2 Integration Feasibility
- **Patch Format:** VCV Rack 2 patches (.vcv) are Zstandard-compressed tar archives containing `patch.json` (JSON format with modules and cables arrays). Patch files are fully deserializable, meaning we can generate patches programmatically in C# using System.Text.Json.
- **Programmatic Generation:** No existing .NET library for VCV Rack patch generation. Solution: model patches in C# (Module, Cable, Patch classes), serialize to JSON, compress with tar+zstd, then load in VCV Rack. All modules, parameters, and connections are JSON-encodable.
- **Built-in Modules:** VCV Rack Free includes oscillators (VCO, Wavetable VCO), filters (VCF), mixers (Mix, VCA Mix), and VCAs out of the box. Minimal viable patch: VCO → VCF → VCA → Audio output.
- **CLI Launch:** VCV Rack can be launched from command line with `Rack <patch.vcv>` and supports `-h` (headless) mode for automation. No native hot-reload, but can kill/restart with new patch via script.
- **MIDI Input:** VCV Rack accepts MIDI via MIDI-CV module. Requires virtual MIDI port (loopMIDI on Windows, IAC on macOS, ALSA/JACK on Linux). Our .NET app sends MIDI to virtual port; VCV receives and converts to CV.
- **Verdict:** Technically feasible but complex—JSON generation, tar/zstd compression, CLI management, virtual MIDI routing. Barrier to entry is medium-high.

### Alternative Synth Engines Assessment

#### SuperCollider
- **Strengths:** Purely programmatic (text-based SynthDef), OSC/MIDI control, fully headless, can be embedded in other apps. Perfect for generative music workflows.
- **Weaknesses:** Steep learning curve (not beginner-friendly). Requires sclang script management. Cross-platform but setup is complex on Windows.
- **Integration:** Send OSC messages from .NET to scsynth (port 57110 by default). Excellent for "describe music, system generates patches" scenario.

#### Surge XT
- **Strengths:** Open-source (GPL-3.0), modern hybrid synth, new CLI mode (v1.3+) for headless operation, MIDI Program Change support, XML preset format (scriptable), excellent sound quality, MPE-capable.
- **Weaknesses:** CLI is still new; GUI-first application. Preset format is proprietary XML (.fxp/.sxpreset), not as straightforward to generate.
- **Integration:** Launch with virtual MIDI port or use command line. Excellent "load preset + play" workflow via MIDI Program Change.

#### Vital
- **Strengths:** Free wavetable synth, stunning visual interface, strong MIDI Learn (any parameter mappable), MPE support. Presets are all-in-one (.vital files).
- **Weaknesses:** No headless/CLI mode. Proprietary preset format (not easily programmatically generated). GUI-required; not ideal for automation.
- **Integration:** Host via VST plugin (.NET VST host) or use MIDI CC learn. Better as a target to control rather than generate patches for.

#### Sonic Pi
- **Strengths:** Ruby-based live coding, OSC/MIDI built-in, can run headless on Linux/Raspberry Pi, excellent for generative/algorithmic music via code.
- **Weaknesses:** Not a traditional synth—it's a live-coding environment. Requires Ruby setup. OSC server stability in headless mode has known quirks.
- **Integration:** Send OSC messages to port 4560. Excellent for "write generative Ruby code, execute it" workflow.

#### FluidSynth
- **Strengths:** Extremely simple. Pure CLI tool. Takes MIDI file + SoundFont, outputs WAV. Fast, lightweight, embeddable. No setup required beyond binary.
- **Weaknesses:** Only GM (General MIDI) soundfonts. No patch generation/synthesis design. Great for playback, not for sound design or evolution.
- **Integration:** Call CLI from .NET: `fluidsynth -ni -F output.wav sound.sf2 input.mid`. Perfect for "generate MIDI, render to audio" but limited creatively.

#### CSound
- **Strengths:** Powerful, ancient (battle-tested), fully programmatic synthesis (text-based .csd files). Headless, MIDI/OSC capable. Deep synthesis control.
- **Weaknesses:** Very steep learning curve. Syntax is arcane. Not beginner-friendly. Less active community than SuperCollider.
- **Integration:** Generate .csd file (text), launch `csound -odac -M <MIDI> file.csd`. For deep synthesis experts only.

### Recommendation: Simplest Path to "Generate Patch & Play"

**Fastest MVP: Sonic Pi (Ruby live-coding approach)**
- Write Ruby code templates for ambient drones.
- Send via OSC to headless Sonic Pi server.
- Minimal infrastructure (just Sonic Pi installed).
- Brady already codes; Ruby is approachable.
- Limitation: Not traditional synth patching, but fully generative.

**Deepest Integration: SuperCollider (OSC-driven synthesis)**
- Generate SynthDef code templates in C#.
- Send to scsynth via OSC messages.
- Fully headless, embeddable.
- Superior for "AI describes music → system synthesizes it" workflow.
- Learning curve: Medium-high for SuperCollider itself, but communication via OSC is trivial.

**Best "Load & Play" with Minimal Setup: Surge XT + MIDI**
- Generate MIDI (notes, CC), send to Surge XT via virtual MIDI port.
- Surge XT has new CLI headless mode (v1.3+).
- Presets are loadable via MIDI Program Change.
- Lower barrier: Surge XT is a traditional synth; users understand it immediately.
- Limitation: Patch generation is harder; better for "compose MIDI for existing Surge patches."

**Compromise for Ambient Specificity: FluidSynth + MIDI Generation**
- Generate MIDI sequences in .NET.
- Render with FluidSynth + ambient-friendly soundfont.
- Simplest possible integration.
- Limitation: Limited sound design; SoundFont playback only.

### Dream Scenario: "Ambient Generative Music While Brady Codes"

**Best Setup:** SuperCollider for synthesis + Sonic Pi for high-level description.
- Brady: "Generate 10 minutes of evolving ambient drone, Dorian mode, sparse."
- SqncR: Uses AI to write Sonic Pi Ruby code or SuperCollider SynthDef templates.
- Both systems run headless; audio flows to system output.
- Brady hears generative music without ever opening a synthesizer GUI.
- Fully embeddable in MCP workflow (ask AI for musical ideas, AI asks synthesis engine).

**Alternative Dream: VCV Rack as the "visual interface" for when Brady wants to tweak.**
- SqncR generates patches programmatically.
- Brady can open VCV Rack GUI to see/edit the patch visually if desired.
- MIDI from SqncR controls the patch in real time.
- Combines best of both worlds: programmatic generation + visual feedback.

📌 Team update (2026-02-13): Two-Path Model Uses Unified Instrument Abstraction — decided by Mal
Hardware and software paths converge on single Instrument data model. MCP tool surface is unified (no branching logic). Generation engine is device-agnostic. MVP starts with Path A (hardware MIDI); Path B (software synth) is a later addition reusing the same generation engine.

📌 Team update (2026-02-13): Device Profile as Data-Driven Architecture — decided by Wash
Profiles should be YAML/JSON structures (not hard-coded logic). Generator queries profiles at runtime to respect hardware constraints. Profile structure includes device ID, MIDI channel, polyphony limit, velocity response curve, CC mappings, latency estimate. Profiles live at `~/.sqncr/devices/{device_id}.yaml`. This enables device-agnostic generation engine and makes adding new devices trivial—no code changes needed.

📌 Team update (2026-02-13): User directive — synth engine scope — decided by Brady
Skip SuperCollider. Support only Sonic Pi and VCV Rack as software synth targets. Inara specializes in Sonic Pi (Ruby OSC integration). River specializes in VCV Rack (patch generation + MIDI routing).

📌 Team update (2026-02-13): Full team recast from Firefly to Adventure Time universe. All 8 Firefly agents retired to _alumni. 10 new Adventure Time agents created (8 role transfers + 2 new roles). All histories transferred. Casting state updated with Adventure Time assignment. Policy updated to include Adventure Time universe (capacity 15). — decided by Brady

📌 Team update (2026-02-13): Roadmap decomposed into 36 work-item GitHub issues — decided by Finn
The v1 roadmap (issue #1) has been decomposed into 36 individual GitHub issues, one per logical work unit. M0 has 2 issues, M1 has 10 issues, M2 has 7 issues, M3 has 8 issues, M4 has 9 issues. Each issue has clear context, acceptance criteria, and agent ownership labels. You have been assigned to M2 issues on VCV Rack patch generation. Issue tracking is now granular and actionable.

📌 Team update (2026-02-14): Hardware MIDI deferred to final milestone — decided by Finn
Device profile work (your domain) moves to M4. The v1 roadmap is restructured to prove the generation engine works entirely in software (Sonic Pi, VCV Rack) before adding hardware complexity. M0–M3 focus on software validation; M4 integrates hardware (device profiles, MIDI routing, conversational setup). This is the final integration layer, not the foundation. Rationale: fastest path to validation is MCP server → music generation in pure software → hardware complexity last.
