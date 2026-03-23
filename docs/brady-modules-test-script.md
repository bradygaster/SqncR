# 🎵 Brady Modules — Test Script

> **You don't need to build any C++ modules.** We're generating `.vcv` patch files
> that use VCV Rack's built-in Fundamental modules, wired up to simulate the
> Brady Module sounds. SqncR generates the patch, you load it, agents play it.

---

## Prerequisites Checklist

Run these one at a time. Skip anything you already have.

### 1. .NET 9 SDK
```powershell
dotnet --version
# Should show 9.x. If not: https://dotnet.microsoft.com/download
```

### 2. VCV Rack 2 (Free)
```powershell
# Check if installed
& "${env:ProgramFiles}\VCV\Rack2Free\Rack.exe" --version 2>$null
# If not installed: https://vcvrack.com/Rack — download the FREE version
# Install to default location (C:\Program Files\VCV\Rack2Free\)
```

### 3. loopMIDI (Virtual MIDI Port)
```powershell
# Check if running
Get-Process -Name loopMIDI* -ErrorAction SilentlyContinue
# If not installed: https://www.tobias-erichsen.de/software/loopmidi.html
# After install: open loopMIDI, click "+" to create a port named "loopMIDI Port"
# Leave loopMIDI running in the tray
```

### 4. SqncR built and ready
```powershell
cd C:\src\sqncr
git checkout brady-modules
git pull
dotnet build SqncR.slnx
# Should build successfully with 0 errors
```

---

## Test 1: Generate the Brady Jam Session Patch

This creates the `.vcv` file — the full 4-module rack.

```powershell
cd C:\src\sqncr

# Run the quick smoke test (no MIDI hardware needed)
dotnet test tests/SqncR.VcvRack.Tests/ --filter "FullyQualifiedName~Brady" -v minimal
# ✅ All tests should pass

# Run euclidean tests too
dotnet test tests/SqncR.Core.Tests/ --filter "FullyQualifiedName~Euclidean" -v minimal
# ✅ Should see 15+ passing tests
```

## Test 2: Load the Patch in VCV Rack

### Option A: Via SqncR MCP (the cool way)

1. Open VS Code in the SqncR repo
2. Open Copilot Chat (Ctrl+Shift+I)
3. SqncR MCP should auto-discover from `.vscode/mcp.json`
4. Say to Copilot:

```
Generate a Brady Jam Session patch and launch VCV Rack with it
```

This should call `generate_patch(template: "brady-jam")` then `launch_vcv_rack`.

### Option B: Manual (if MCP isn't cooperating)

```powershell
cd C:\src\sqncr

# Generate the patch file using the CLI
dotnet run --project src/SqncR.Cli -- generate-patch --template brady-jam --output brady-jam.vcv

# Open it in VCV Rack
& "${env:ProgramFiles}\VCV\Rack2Free\Rack.exe" .\brady-jam.vcv
```

### What You Should See in VCV Rack

A rack with modules laid out left to right:

```
┌─────────┐ ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐ ┌─────────┐
│ MIDI-   │ │ VCO    │ │ Noise  │ │ VCF    │ │ VCA    │ │ ADSR   │ │ ...more │
│ Gate    │ │        │ │        │ │        │ │        │ │        │ │ modules │
│ (Ch10)  │ │        │ │        │ │        │ │        │ │        │ │         │
└─────────┘ └────────┘ └────────┘ └────────┘ └────────┘ └────────┘ └─────────┘
                         ↑ Cables connecting everything ↑
```

**Purple cables** connecting MIDI inputs to sound modules to the mixer to audio output.

**Check these things:**
- [ ] VCV Rack opens without errors
- [ ] You see MIDI-Gate, MIDI-CV, MIDI-CC modules (the input bridges)
- [ ] You see VCO, VCF, VCA, ADSR, Noise, LFO, Delay modules  
- [ ] You see a VCMixer feeding into Audio-8
- [ ] Colored cables connect everything

---

## Test 3: Make Sound! (The Actual Jam)

### Step 1: Set up the MIDI connection

In VCV Rack, right-click the **MIDI-Gate** module:
- Set MIDI Driver to your system driver
- Set MIDI Device to **"loopMIDI Port"**

Do the same for each **MIDI-CV** and **MIDI-CC** module.

> **TIP:** Each MIDI module should be on a different channel:
> - MIDI-Gate → Channel 10 (drums)
> - First MIDI-CV → Channel 2 (Tidegate)
> - Second MIDI-CV → Channel 3 (Driftwave)
> - MIDI-CC → Channel 4 (Rustle)

### Step 2: Set up audio output

Right-click the **Audio-8** module:
- Set Audio Driver to your system audio
- Set Audio Device to your speakers/headphones

### Step 3: Start SqncR and jam!

In a NEW terminal (leave VCV Rack running):

```powershell
cd C:\src\sqncr

# Option A: Via Copilot Chat in VS Code
# Just talk to it:
#   "Open loopMIDI Port and start a house beat at 118 BPM"
#   "Add a pad instrument on channel 3, A minor"
#   "Make it darker"
#   "Switch to breakbeat"
#   "Stop"

# Option B: Via Claude Desktop  
# Add SqncR to claude_desktop_config.json (see README)
# Then chat naturally

# Option C: Direct MCP server (for testing)
dotnet run --project src/SqncR.McpServer
# Then send JSON-RPC commands via stdin
```

### Step 4: The Demo Script (what to actually say)

```
You: "List my MIDI devices"
→ Should show loopMIDI Port

You: "Open loopMIDI Port"  
→ ✅ Device opened

You: "Load the brady-jam scene"
→ ✅ 118 BPM, A minor, house pattern configured

You: "Start generation"
→ 🎵 You should hear drums through VCV Rack!

You: "Add an instrument called Driftwave as a pad on channel 3"
→ ✅ Pad registered

You: "Make it darker"
→ 🎵 Sound shifts to phrygian, filter drops

You: "Stop"
→ 🔇 Silence (with delay trails if Rustle is active)
```

---

## Test 4: Verify the Euclidean Patterns

```powershell
cd C:\src\sqncr

# Quick console test — see the patterns visually
dotnet test tests/SqncR.Core.Tests/ --filter "Euclidean" -v normal
```

You should see tests verifying:
- E(3,8) = `X . . X . . X .` (tresillo)
- E(5,8) = `X . X X . X X .` (cinquillo)  
- E(5,16) = `X . . X . . X . . X . . X . . .` (bossa nova)

---

## Troubleshooting

### "No MIDI devices found"
→ Make sure loopMIDI is running (check system tray). Create a port if none exist.

### VCV Rack opens but no modules visible
→ The patch file might need the Fundamental plugin. Go to Library → Update All in VCV Rack.

### "dotnet build fails"
```powershell
dotnet restore SqncR.slnx
dotnet build SqncR.slnx
```

### Sound comes out but wrong/garbled
→ Check that each MIDI module is on the right channel (10, 2, 3, 4).
→ Check that Audio-8 is routed to your actual audio device.

### VCV Rack is silent
→ Audio-8 module: is the output level up? Click the level meters.
→ Are the cables actually connected? (zoom in and check)
→ Is loopMIDI Port selected in the MIDI modules?

---

## File Locations

| What | Where |
|------|-------|
| SqncR repo | `C:\src\sqncr` |
| Branch | `brady-modules` |
| Generated patch | `C:\src\sqncr\brady-jam.vcv` (after generation) |
| Patch templates | `src/SqncR.VcvRack/PatchTemplates.cs` |
| Euclidean algo | `src/SqncR.Core/Rhythm/EuclideanGenerator.cs` |
| Scene preset | `src/SqncR.Core/Persistence/SceneStore.cs` (brady-jam) |
| Agent charters | `.ai-team/agents/tremor/`, `tidegate/`, `driftwave/`, `rustle/` |
| Playbook | `docs/brady-jam-playbook.md` |
| Mockup panels | `C:\src\bradyland\artifacts\research\vcv-mockups\` |

---

*Issues: SqncR #38 #39 #40 #41 #42 | bradyland #299 #300*
