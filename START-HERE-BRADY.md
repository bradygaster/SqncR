# 🎵 Start Here, Brady

**Left off:** March 1, 2026 — you explored how SqncR works with software synths and decided to try Sonic Pi first. You've never used Sonic Pi before. That's fine — SqncR handles all the Sonic Pi code for you.

**Goal for tomorrow:** Get music playing through Sonic Pi using SqncR's MCP server.

---

## What You Need Running

1. **Sonic Pi** — just download and open it: https://sonic-pi.net/
   - It auto-listens on `localhost:4560` (OSC, not MIDI)
   - You do NOT need loopMIDI for Sonic Pi
   - You don't need to write any Ruby or know anything about Sonic Pi's UI
   - Just leave the app open in the background

2. **SqncR MCP Server** — in a terminal:
   ```
   cd C:\src\SqncR
   dotnet run --project src/SqncR.McpServer
   ```

3. **Copilot** — open this repo in VS Code or use the CLI. The MCP server auto-connects via `.vscode/mcp.json`.

---

## The Conversation (Copy-Paste Friendly)

Once everything's running, just talk to Copilot. Here's a script:

### Check the connection
> "Is Sonic Pi running?"

This calls `sonic_pi_status` — you should see it's reachable on port 4560.

### Set up a synth
> "Set up a dark ambient pad synth in Sonic Pi with reverb"

This calls `setup_software_synth` — SqncR generates Ruby code and sends it via OSC. You never touch Sonic Pi's editor.

### Start playing
> "Start generating ambient music in A minor at 90 BPM"

This calls `start_generation` — notes start flowing to Sonic Pi. You should hear sound.

### Tweak it
> "Make it bluesier and bump the tempo to 100"
> "Switch to pentatonic minor"
> "More variety"
> "Less drums"

### Save what you like
> "Save this session as late-night-coding"

### Stop
> "Stop the music"

---

## Quick Reference

| What | Command / Tool |
|------|---------------|
| List MIDI devices | `list_devices` |
| Check Sonic Pi | `sonic_pi_status` |
| Set up synth | `setup_software_synth` |
| Start music | `start_generation` |
| Change music | `modify_generation` |
| Stop music | `stop_generation` |
| Save session | `save_session` |
| Load session | `load_session` |
| Load a preset scene | `load_scene` (try "ambient-pad", "chill-lofi", "driving-techno") |

## Available Scales

Major, Minor, Harmonic Minor, Melodic Minor, Pentatonic Major, Pentatonic Minor, Blues, Dorian, Phrygian, Lydian, Mixolydian, Whole Tone, Diminished, Chromatic

## Available Drum Patterns

rock, house, hip-hop, jazz, ambient, breakbeat, half-time, shuffle, latin-clave, bossa-nova

---

## How It Works (The Part You Were Curious About)

- **Sonic Pi uses OSC (UDP on port 4560), not MIDI.** SqncR generates Ruby code strings and sends them directly.
- **SqncR picks the synth engine, writes the effects chain, generates note sequences** — all as Ruby that Sonic Pi executes.
- **You never open Sonic Pi's editor.** It's just a sound engine sitting in the background.
- **For hardware synths or VCV Rack**, you'd need loopMIDI + MIDI routing. But Sonic Pi is the zero-config option.

---

## If Something Doesn't Work

- **No sound?** Make sure Sonic Pi is open (not just installed — actually running). Check with `sonic_pi_status`.
- **Build fails?** Run `dotnet build SqncR.slnx` and check for errors. You need .NET 9+ (`dotnet --version`).
- **MCP not connecting?** Check that `.vscode/mcp.json` exists in the repo. Restart VS Code after changes.
- **Tests:** `dotnet test SqncR.slnx` runs all 586 tests if you want to verify everything's healthy.
