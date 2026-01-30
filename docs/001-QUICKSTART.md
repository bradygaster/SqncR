# SqncR Quickstart

Get music playing in 5 minutes.

---

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- A MIDI device (hardware synth, virtual MIDI, etc.)

---

## Install

```bash
# Clone
git clone https://github.com/bradygaster/SqncR.git
cd SqncR

# Build
dotnet build
```

---

## See Your Devices

```bash
dotnet run --project src/SqncR.Cli -- list-devices
```

Output:
```
[0] Polyend Synth MIDI 1
[1] Moog Mother-32
[2] Microsoft GS Wavetable Synth
```

---

## Play a Sequence

```bash
dotnet run --project src/SqncR.Cli -- play examples/chill-ambient.sqnc.yaml -d 0
```

Replace `0` with your device index from the list above.

Output:
```
Playing: Late Night Ambient
Tempo: 70 BPM, Key: Cm
Press Ctrl+C to stop
  ON:  Ch1 C2 vel=70
  ON:  Ch1 C2 vel=68
  ...
```

Music plays through your hardware. Press Ctrl+C to stop.

---

## Generate Music (After P3)

Once generation is implemented:

```bash
dotnet run --project src/SqncR.Cli -- generate "ambient drone in C minor, 70 BPM" -d 0
```

---

## Use with Claude Desktop (After P4)

Add to `~/.config/claude/claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "sqncr": {
      "command": "dotnet",
      "args": ["run", "--project", "C:/src/SqncR/src/SqncR.McpServer"]
    }
  }
}
```

Then in Claude:
```
You: list my midi devices
Claude: [shows your devices]

You: play something ambient on device 0
Claude: [music starts]
```

---

## Next Steps

- Read [002-VISION](./002-VISION.md) to understand why SqncR exists
- Read [003-ARCHITECTURE](./003-ARCHITECTURE.md) to understand how it works
- Check [sprints/P0-play-songs.md](../sprints/P0-play-songs.md) to see what's being built

---

## Troubleshooting

**No devices found?**
- Check MIDI drivers are installed
- On Windows, virtual MIDI requires LoopMIDI or similar
- Hardware must be powered on and connected via USB/MIDI

**Latency issues?**
- Use USB MIDI when possible (faster than DIN MIDI)
- Close other applications using MIDI
- Check your audio interface buffer settings

**Build errors?**
- Ensure .NET 9 SDK is installed: `dotnet --version`
- Try: `dotnet restore` then `dotnet build`

---

*That's it. You're making music.*
