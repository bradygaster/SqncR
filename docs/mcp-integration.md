# SqncR MCP Integration Guide

Complete reference for SqncR's 30+ MCP tools. Connect any MCP-compatible AI assistant and start generating music through natural language.

## Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download) (or later)
- [loopMIDI](https://www.tobias-erichsen.de/software/loopmidi.html) (or any virtual MIDI driver)
- A software synth — [Sonic Pi](https://sonic-pi.net/), [VCV Rack](https://vcvrack.com/), or any MIDI-compatible DAW

### Running the MCP Server

```bash
dotnet run --project src/SqncR.McpServer
```

The server communicates via **stdio** (stdin/stdout JSON-RPC). All log output is directed to stderr so it does not interfere with the MCP protocol.

### MCP Client Configuration

#### GitHub Copilot (VS Code)

If you cloned the repo, the `.vscode/mcp.json` is already included — Copilot will discover the SqncR server automatically.

To configure manually, add to your `.vscode/mcp.json`:

```json
{
  "servers": {
    "sqncr": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "path/to/SqncR/src/SqncR.McpServer"]
    }
  }
}
```

#### Claude Desktop

Add to your Claude Desktop config (`~/.config/claude/claude_desktop_config.json` on macOS/Linux or `%APPDATA%\Claude\claude_desktop_config.json` on Windows):

```json
{
  "mcpServers": {
    "sqncr": {
      "command": "dotnet",
      "args": ["run", "--project", "path/to/SqncR/src/SqncR.McpServer"]
    }
  }
}
```

#### Generic MCP Client (stdio)

The server uses stdin/stdout for JSON-RPC communication following the [Model Context Protocol](https://modelcontextprotocol.io/) specification. Configure your MCP client to spawn the process and communicate via stdio.

---

## Core Tools

### `ping`

Returns server status and confirms the MCP server is alive.

**Parameters:** None

**Example response:**
```
pong — SqncR MCP Server is running. Time: 2026-02-16T14:30:00.0000000+00:00
```

---

### `list_devices`

Lists available MIDI output devices on the system.

**Parameters:** None

**Example response:**
```
Found 2 MIDI output device(s):
  [0] loopMIDI Port
  [1] Microsoft GS Wavetable Synth
```

If no devices are found:
```
No MIDI output devices found. Ensure a MIDI device is connected or a virtual MIDI driver (e.g. loopMIDI) is installed.
```

---

### `open_device`

Opens a MIDI output device by index or name. Use `list_devices` first to see available devices.

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `deviceIndex` | int | No* | Device index from `list_devices` |
| `deviceName` | string | No* | Device name (partial match, case-insensitive) |

*One of `deviceIndex` or `deviceName` must be provided.

**Example response:**
```
Opened MIDI device: loopMIDI Port
```

---

### `start_generation`

Starts music generation with the specified parameters. Configures tempo, scale, pattern, and octave then begins playback.

**Parameters:**

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `tempo` | double | 120 | Tempo in BPM |
| `scale` | string | `"Pentatonic Minor"` | Scale name (see [Available Scales](#available-scales)) |
| `rootNote` | string | `"C4"` | Root note (e.g. `"C4"`, `"F#3"`, `"Bb2"`) |
| `pattern` | string | `"rock"` | Drum pattern (see [Available Patterns](#available-patterns)) |
| `octave` | int | 4 | Base octave for melody (0–8) |
| `variety` | string | `"off"` | Variety level: `conservative`, `moderate`, `adventurous`, `off` |

**Example response:**
```
Started generation: 100 BPM, A Minor (root A3), ambient pattern, octave 3, variety moderate
```

---

### `modify_generation`

Modifies current generation parameters without stopping playback. Only provided parameters are changed. Supports **smooth transitions** that glide over bars instead of snapping instantly.

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `tempo` | double | No | New tempo in BPM |
| `scale` | string | No | New scale name |
| `rootNote` | string | No | New root note (e.g. `"D4"`) |
| `pattern` | string | No | New drum pattern |
| `octave` | int | No | New base octave |
| `smooth` | bool | No | When `true`, tempo and scale transitions happen smoothly over bars instead of instantly. Default: `false` |
| `variety` | string | No | Variety level: `conservative`, `moderate`, `adventurous`, `off` |

**Example response:**
```
Modified generation:
  Scale → D Dorian (smooth)
  Tempo → 140 BPM (smooth)
```

---

### `stop_generation`

Stops music generation and silences all notes.

**Parameters:** None

**Example response:**
```
Generation stopped.
```

---

### `get_status`

Returns the current state of the music generation engine including playback status, tempo, scale, pattern, and channels.

**Parameters:** None

**Example response:**
```
Status: ▶ Playing
Tempo: 100 BPM
Scale: A Minor
Pattern: ambient
Octave: 3
Melodic Channel: 0
Drum Channel: 9
```

---

## Session Tools

### `save_session`

Saves the current generation state as a named session for later recall. Sessions are stored as JSON at `~/.sqncr/sessions/`.

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `name` | string | Yes | Name for this session |

**Example response:**
```
Session 'late-night-coding' saved.
```

---

### `load_session`

Loads a previously saved session, restoring all generation parameters.

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `name` | string | Yes | Name of session to load |

**Example response:**
```
Session 'late-night-coding' loaded (saved 2026-02-16T22:15:00Z).
```

---

### `list_sessions`

Lists all saved generation sessions.

**Parameters:** None

**Example response:**
```
Saved sessions (2):
  • late-night-coding
  • morning-ambient
```

---

## Scene Tools

Scenes are lightweight presets (named musical "recipes") that capture the core generation parameters. SqncR includes 3 built-in presets and supports user-created scenes. Scenes are stored as JSON at `~/.sqncr/scenes/`.

### Built-In Scene Presets

| Name | Description |
|------|-------------|
| `ambient-pad` | Slow, atmospheric, sparse percussion |
| `driving-techno` | Four-on-the-floor, high energy |
| `chill-lofi` | Relaxed hip-hop feel |

### `save_scene`

Saves the current generation state as a named scene preset for instant recall.

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `name` | string | Yes | Name for this scene preset |
| `description` | string | No | Optional description of this scene |

**Example response:**
```
Scene 'dark-ambient' saved.
```

---

### `load_scene`

Loads a scene preset, applying its settings to the generation engine.

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `name` | string | Yes | Name of scene to load |

**Example response:**
```
Scene 'ambient-pad' loaded — 72 BPM, Pentatonic Minor, C3.
```

---

### `list_scenes`

Lists all available scene presets (user-saved and built-in).

**Parameters:** None

**Example response:**
```
Scenes (4):
  • ambient-pad [built-in] — Slow, atmospheric, sparse percussion
  • driving-techno [built-in] — Four-on-the-floor, high energy
  • chill-lofi [built-in] — Relaxed hip-hop feel
  • dark-ambient — My custom dark ambient scene
```

---

### `delete_scene`

Deletes a user-saved scene preset. Built-in scenes cannot be deleted.

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `name` | string | Yes | Name of scene to delete |

**Example response:**
```
Scene 'dark-ambient' deleted.
```

---

## Instrument Setup Tools

These tools manage the full instrument lifecycle — discovery, registration, profile persistence, and teardown. Instrument profiles are stored as JSON at `~/.sqncr/devices/`.

### `setup_instrument`

Conversational instrument setup — discovers MIDI devices for hardware, applies software synth defaults for Sonic Pi / VCV Rack, creates a device profile, and registers the instrument. Auto-assigns MIDI channel if not specified.

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `name` | string | Yes | Human-readable name for the instrument |
| `type` | string | Yes | Instrument type: `Hardware`, `SonicPi`, `VcvRack` |
| `role` | string | No | Musical role: `Bass`, `Pad`, `Lead`, `Drums`, `Melody` (default: `Melody`) |
| `channel` | int | No | MIDI channel (1-16). Auto-assigned if omitted |
| `description` | string | No | Optional description of the instrument |

**Software synth defaults:**

| Type | Range | Polyphony | Timbre |
|------|-------|-----------|--------|
| SonicPi | 24-108 | 8 | digital synthesis |
| VcvRack | 0-127 | 16 | modular synthesis |

**Example response:**
```
Available MIDI devices:
  [0] loopMIDI Port

✅ Instrument 'Moog Bass' set up successfully!
  ID:      moog-bass
  Type:    Hardware
  Role:    Bass
  Channel: 1
  Range:   0-127
  Poly:    1
```

---

### `describe_instrument`

Returns a detailed human-readable description of a registered instrument including name, type, role, channel, capabilities, and CC mappings.

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `id` | string | Yes | Instrument ID to describe |

**Example response:**
```
🎹 Moog Bass
  ID:      moog-bass
  Type:    Hardware
  Role:    Bass
  Channel: 1
  Range:   24-72
  Poly:    1
  Timbre:  analog subtractive
  CC Mappings:
    filter: CC 74
    resonance: CC 71
```

---

### `list_setup_instruments`

Lists all registered instruments grouped by musical role with channel assignments.

**Parameters:** None

**Example response:**
```
🎛️ Registered instruments (3):

  [Bass]
    moog-bass: Moog Bass | Hardware | Ch 1 | Notes 24-72

  [Pad]
    juno-pad: Roland Juno | Hardware | Ch 2 | Notes 36-96

  [Melody]
    sp-lead: Sonic Pi Lead | SonicPi | Ch 3 | Notes 24-108
```

---

### `remove_setup_instrument`

Removes an instrument from the registry, optionally deletes its device profile, and sends AllNotesOff on its channel.

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `id` | string | Yes | Instrument ID to remove |
| `deleteProfile` | bool | No | Whether to also delete the device profile from disk. Default: `false` |

**Example response:**
```
✅ Removed instrument 'Moog Bass' (ch 1). Device profile deleted. AllNotesOff sent.
```

---

## Instrument (Engine) Tools

These tools interact directly with the generation engine's instrument registry for adding/removing instruments during active generation.

### `add_instrument`

Adds an instrument to the generation engine with a specified role and MIDI channel.

**Parameters:**

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `name` | string | *(required)* | Unique name for the instrument |
| `type` | string | `"Hardware"` | Instrument type: `Hardware`, `SonicPi`, `VcvRack` |
| `role` | string | `"Melody"` | Musical role: `Bass`, `Pad`, `Lead`, `Drums`, `Melody` |
| `channel` | int | 1 | MIDI channel (1-16) |

**Example response:**
```
Added instrument 'Lead Synth' (id: lead-synth) as Lead on channel 3.
```

---

### `remove_instrument`

Removes an instrument from the generation engine by its ID.

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `id` | string | Yes | Instrument ID to remove |

**Example response:**
```
Removed instrument 'lead-synth'.
```

---

### `list_instruments`

Lists all registered instruments and their roles, channels, and capabilities.

**Parameters:** None

**Example response:**
```
Registered instruments (2):
  lead-synth: Lead Synth | Hardware | Lead | Ch 3 | Notes 0-127
  bass-synth: Bass Synth | Hardware | Bass | Ch 1 | Notes 0-127
```

---

## Sonic Pi Tools

Control [Sonic Pi](https://sonic-pi.net/) via OSC. Sonic Pi must be running and listening on port 4560 (default).

### `setup_software_synth`

Sets up a Sonic Pi software synthesizer. Creates an instrument with the specified synth engine and optional effects chain, then activates it.

**Parameters:**

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `synthName` | string | `"prophet"` | Sonic Pi synth name (e.g. `prophet`, `tb303`, `blade`, `piano`, `dark_ambience`) |
| `effects` | string | None | Comma-separated FX chain (e.g. `reverb,echo`) |
| `name` | string | None | Display name for this instrument |

**Example response:**
```
✓ Synth 'warm-pad' activated (engine: dark_ambience)
  FX: reverb, echo
  Available synths: prophet, tb303, blade, piano, dark_ambience, ...
```

---

### `play_sonic_pi_note`

Plays a single note on Sonic Pi. Accepts note names (C4, F#3) or MIDI numbers.

**Parameters:**

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `note` | string | *(required)* | Note name (e.g. `C4`, `F#3`) or MIDI number (e.g. `60`) |
| `duration` | double | 1.0 | Duration in seconds |
| `velocity` | int | 80 | Velocity (0-127) |

**Example response:**
```
♪ Playing C4 (MIDI 60) for 1s at velocity 80
```

---

### `sonic_pi_live_loop`

Creates and sends a `live_loop` to Sonic Pi that cycles through a sequence of notes at the specified BPM.

**Parameters:**

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `loopName` | string | *(required)* | Name for the live_loop |
| `notes` | string | *(required)* | Comma-separated note names (e.g. `C4,E4,G4,C5`) |
| `synthName` | string | `"prophet"` | Sonic Pi synth name |
| `bpm` | double | 120 | Tempo in BPM |
| `effects` | string | None | Comma-separated FX (e.g. `reverb,echo`) |

**Example response:**
```
🔁 Live loop 'arpeggio' started: 4 notes at 120 BPM using prophet with FX: reverb
```

---

### `stop_sonic_pi`

Stops all running Sonic Pi code and silences all sound.

**Parameters:** None

**Example response:**
```
⏹ Sonic Pi stopped — all jobs halted.
```

---

### `sonic_pi_status`

Checks whether Sonic Pi appears to be running and reachable via OSC.

**Parameters:** None

**Example response:**
```
✓ Sonic Pi reachable at 127.0.0.1:4560
```

---

## VCV Rack Tools

Control [VCV Rack 2](https://vcvrack.com/) — generate patches from templates, launch, and manage the process.

### `generate_patch`

Generates a VCV Rack patch from a template and saves it as a `.vcv` file. Returns the file path and module list.

**Parameters:**

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `template` | string | `"basic"` | Template name: `basic`, `ambient`, `bass` |
| `name` | string | None | Display name for the patch |
| `outputPath` | string | None | Output file path for the `.vcv` file (defaults to temp directory) |

**Template descriptions:**

| Template | Description |
|----------|-------------|
| `basic` | Minimal synth: MIDI-CV → VCO → VCF → ADSR → VCA → Audio |
| `ambient` | Ambient pad with slow attack/release and LFO-modulated filter |
| `bass` | Punchy bass synth with resonant filter and square wave |

**Example response:**
```
Patch saved: C:\Users\you\AppData\Local\Temp\SqncR-ambient.vcv
Template: ambient
Modules (6):
  - MIDI-CV (Core)
  - VCO (Fundamental)
  - VCF (Fundamental)
  - ADSR (Fundamental)
  - VCA (Fundamental)
  - Audio (Core)
Cables: 8
```

---

### `launch_vcv_rack`

Launches VCV Rack 2 with a patch file. Requires VCV Rack to be installed.

**Parameters:**

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `patchPath` | string | *(required)* | Path to the `.vcv` patch file to load |
| `headless` | bool | `true` | Run headless (no GUI) |

**Example response:**
```
VCV Rack launched with patch: C:\temp\SqncR-ambient.vcv (headless: true)
```

---

### `stop_vcv_rack`

Stops the running VCV Rack process.

**Parameters:** None

**Example response:**
```
VCV Rack stopped.
```

---

### `vcv_rack_status`

Returns whether VCV Rack is currently running and its configuration.

**Parameters:** None

**Example response:**
```
VCV Rack: ▶ Running
MIDI Port: loopMIDI Port
```

---

### `list_templates`

Lists available VCV Rack patch templates with descriptions.

**Parameters:** None

**Example response:**
```
Available VCV Rack patch templates:

  basic   — Minimal synth: MIDI-CV → VCO → VCF → ADSR → VCA → Audio
  ambient — Ambient pad with slow attack/release and LFO-modulated filter
  bass    — Punchy bass synth with resonant filter and square wave
```

---

## Health Tools

### `get_health`

Returns a health snapshot of the generation engine including tick latency, active notes, memory usage, uptime, and missed ticks.

**Parameters:** None

**Example response:**
```json
{
  "tickLatencyMs": 1.2,
  "activeNotes": 4,
  "memoryMb": 42.5,
  "uptimeSeconds": 3600,
  "missedTicks": 0
}
```

---

### `all_notes_off`

Panic button — immediately sends note-off for all active notes. Use if notes are stuck.

**Parameters:** None

**Example response:**
```
All notes off command sent.
```

---

## Available Scales

| Scale | Aliases |
|-------|---------|
| Major | Ionian |
| Minor | Aeolian, Natural Minor |
| Harmonic Minor | |
| Melodic Minor | |
| Pentatonic Major | |
| Pentatonic Minor | |
| Blues | |
| Whole Tone | |
| Diminished | |
| Chromatic | |
| Dorian | |
| Phrygian | |
| Lydian | |
| Mixolydian | |
| Locrian | |

## Available Patterns

| Pattern | Style |
|---------|-------|
| `rock` | Standard rock beat — kick on 1 & 3, snare on 2 & 4 |
| `house` | Four-on-the-floor electronic |
| `hip-hop` | Boom-bap style with accented hats |
| `jazz` | Swing feel with ride and ghost kicks |
| `ambient` | Sparse, probabilistic hits |
| `breakbeat` | Syncopated kick with busy hat work |
| `half-time` | Kick on 1, snare on 3, spacious feel |
| `shuffle` | Triplet-feel hats with alternating velocities |
| `latin-clave` | Son clave 3-2 pattern |
| `bossa-nova` | Cross-stick pattern with gentle hat |

---

## Example Conversations

### Start Playing in A Minor at 100 BPM

> **User:** "Play some ambient music in A minor at 100 BPM"

The AI assistant calls the tools in sequence:

1. **`list_devices`** → finds available MIDI devices
   ```
   Found 1 MIDI output device(s):
     [0] loopMIDI Port
   ```

2. **`open_device(deviceName: "loopMIDI")`** → opens the MIDI output
   ```
   Opened MIDI device: loopMIDI Port
   ```

3. **`start_generation(tempo: 100, scale: "minor", rootNote: "A3", pattern: "ambient", octave: 3)`** → starts playback
   ```
   Started generation: 100 BPM, A Minor (root A3), ambient pattern, octave 3
   ```

4. **`get_status()`** → confirms current state
   ```
   Status: ▶ Playing
   Tempo: 100 BPM
   Scale: A Minor
   Pattern: ambient
   Octave: 3
   Melodic Channel: 0
   Drum Channel: 9
   ```

---

### Smooth Transition with Variety

> **User:** "Switch to D Dorian, bring the tempo up to 140, make it smooth and add some variety"

**`modify_generation(scale: "dorian", rootNote: "D4", tempo: 140, smooth: true, variety: "moderate")`**
```
Modified generation:
  Tempo → 140 BPM (smooth)
  Scale → D Dorian (smooth)
  Variety → moderate
```

---

### Scene Workflow

> **User:** "Load the ambient pad preset"

**`load_scene(name: "ambient-pad")`**
```
Scene 'ambient-pad' loaded — 72 BPM, Pentatonic Minor, C3.
```

> **User:** "Make it darker, save as my-ambient"

**`modify_generation(scale: "phrygian", octave: 2)`**
```
Modified generation:
  Scale → D Phrygian
  Octave → 2
```

**`save_scene(name: "my-ambient", description: "Dark Phrygian ambient")`**
```
Scene 'my-ambient' saved.
```

---

### Multi-Instrument Setup

> **User:** "Set up a bass on hardware MIDI and a pad on Sonic Pi"

1. **`setup_instrument(name: "Moog Bass", type: "Hardware", role: "Bass")`**
   ```
   ✅ Instrument 'Moog Bass' set up successfully!
     ID: moog-bass, Type: Hardware, Role: Bass, Channel: 1
   ```

2. **`setup_instrument(name: "SP Pad", type: "SonicPi", role: "Pad")`**
   ```
   ✅ Instrument 'SP Pad' set up successfully!
     ID: sp-pad, Type: SonicPi, Role: Pad, Channel: 2
   ```

3. **`list_setup_instruments()`**
   ```
   🎛️ Registered instruments (2):
     [Bass]
       moog-bass: Moog Bass | Hardware | Ch 1 | Notes 0-127
     [Pad]
       sp-pad: SP Pad | SonicPi | Ch 2 | Notes 24-108
   ```

---

### Full Session Flow

A complete session from setup to shutdown:

> **User:** "What MIDI devices do I have?"

**`list_devices()`**
```
Found 2 MIDI output device(s):
  [0] loopMIDI Port
  [1] Microsoft GS Wavetable Synth
```

> **User:** "Open the loopMIDI port"

**`open_device(deviceName: "loopMIDI")`**
```
Opened MIDI device: loopMIDI Port
```

> **User:** "Start a jazz piece in Bb, 95 BPM with moderate variety"

**`start_generation(tempo: 95, scale: "dorian", rootNote: "Bb3", pattern: "jazz", octave: 3, variety: "moderate")`**
```
Started generation: 95 BPM, Bb Dorian (root Bb3), jazz pattern, octave 3, variety moderate
```

> **User:** "Switch to a bossa nova feel"

**`modify_generation(pattern: "bossa-nova", smooth: true)`**
```
Modified generation:
  Pattern → bossa-nova
```

> **User:** "Make it bluesier"

**`modify_generation(scale: "blues", smooth: true)`**
```
Modified generation:
  Scale → Bb Blues (smooth)
```

> **User:** "Save this session"

**`save_session(name: "bossa-blues")`**
```
Session 'bossa-blues' saved.
```

> **User:** "What's playing right now?"

**`get_status()`**
```
Status: ▶ Playing
Tempo: 95 BPM
Scale: Bb Blues
Pattern: bossa-nova
Octave: 3
Melodic Channel: 0
Drum Channel: 9
```

> **User:** "Stop the music"

**`stop_generation()`**
```
Generation stopped.
```

---

## Troubleshooting

### No MIDI devices found

Install a virtual MIDI driver like [loopMIDI](https://www.tobias-erichsen.de/software/loopmidi.html), then create a virtual port. The device will appear in `list_devices`.

### No sound output

The MCP server sends MIDI messages — you need a synth to receive them. Route your virtual MIDI port to a DAW, Sonic Pi, VCV Rack, or any MIDI-capable synthesizer.

### Server not connecting

- Verify .NET 9 SDK is installed: `dotnet --version`
- Ensure the project builds: `dotnet build src/SqncR.McpServer`
- Check that your MCP client configuration points to the correct project path
- The server uses stdio transport — logs go to stderr, protocol messages to stdout

### OpenTelemetry (optional)

If you're running the full Aspire stack, set the `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable to send traces and metrics to the Aspire Dashboard for real-time monitoring of MIDI messages, generation decisions, and engine state.
