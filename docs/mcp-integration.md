# SqncR MCP Integration Guide

Connect AI assistants to SqncR's MCP server and start generating music through natural language.

## Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
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

## Available Tools

### `ping`

Returns server status and confirms the MCP server is alive.

**Parameters:** None

**Example response:**
```
pong — SqncR MCP Server is running. Time: 2025-01-15T14:30:00.0000000+00:00
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

**Example response:**
```
Started generation: 100 BPM, A Minor (root A3), ambient pattern, octave 3
```

---

### `modify_generation`

Modifies current generation parameters without stopping playback. Only provided parameters are changed.

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `tempo` | double | No | New tempo in BPM |
| `scale` | string | No | New scale name |
| `rootNote` | string | No | New root note (e.g. `"D4"`) |
| `pattern` | string | No | New drum pattern |
| `octave` | int | No | New base octave |

**Example response:**
```
Modified generation:
  Scale → D Dorian
  Tempo → 140 BPM
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

## Available Scales

| Scale | Aliases |
|-------|---------|
| Major | ionian |
| Minor | aeolian |
| Harmonic Minor | |
| Melodic Minor | |
| Pentatonic Major | |
| Pentatonic Minor | pentatonic |
| Blues | blues-minor |
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
| `rock` | Standard rock beat |
| `house` | Four-on-the-floor electronic |
| `hip-hop` | Boom-bap style |
| `jazz` | Swing feel |
| `ambient` | Sparse, atmospheric |

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

### Change Key Mid-Play

> **User:** "Switch to D Dorian, bring the tempo up to 140"

The AI calls `modify_generation` with only the changed parameters:

**`modify_generation(scale: "dorian", rootNote: "D4", tempo: 140)`**
```
Modified generation:
  Tempo → 140 BPM
  Scale → D Dorian
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

> **User:** "Start a jazz piece in Bb, 95 BPM"

**`start_generation(tempo: 95, scale: "dorian", rootNote: "Bb3", pattern: "jazz", octave: 3)`**
```
Started generation: 95 BPM, Bb Dorian (root Bb3), jazz pattern, octave 3
```

> **User:** "Make it bluesier"

**`modify_generation(scale: "blues")`**
```
Modified generation:
  Scale → Bb Blues
```

> **User:** "Bump it up to 110"

**`modify_generation(tempo: 110)`**
```
Modified generation:
  Tempo → 110 BPM
```

> **User:** "What's playing right now?"

**`get_status()`**
```
Status: ▶ Playing
Tempo: 110 BPM
Scale: Bb Blues
Pattern: jazz
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
