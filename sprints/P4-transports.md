# P4: Transports - CLI Polish, MCP Server

**Priority:** 4
**Depends on:** P3 (First Generation)
**Goal:** Same core, multiple ways to access it
**Duration:** ~2 weeks

---

## The Wow Moment

Talk to Claude, and your synths start playing.

```
You (in Claude Desktop): "play an ambient drone in C minor on my Polyend Synth"
Claude calls SqncR MCP tools...
Music starts playing through your hardware.
```

---

## What We're Building

1. **CLI polish** - Help text, better error messages, shell completion
2. **MCP server** - Tools exposed via Model Context Protocol
3. **Interactive mode** - REPL for live exploration
4. **Maybe REST API** - HTTP endpoints (optional for P4)

## What We're NOT Building Yet

- Full SDK package
- Web UI
- DAW integration

---

## Architecture

```
┌─────────────────────────────────────────────────┐
│                  SqncRService                   │
│         (Core logic, skills, generation)        │
└────────────────────┬────────────────────────────┘
                     │
         ┌───────────┼───────────┐
         │           │           │
    ┌────┴────┐ ┌────┴────┐ ┌────┴────┐
    │   CLI   │ │   MCP   │ │   API   │
    │ sqncr   │ │ Server  │ │  (REST) │
    └─────────┘ └─────────┘ └─────────┘
```

All transports call the same `SqncRService`. No business logic in transports.

---

## Tasks

### Task 1: CLI Polish

**Better help text:**
```csharp
var rootCommand = new RootCommand("SqncR - AI-Native Generative Music System")
{
    Description = @"
SqncR lets you control MIDI devices through conversation with AI.

QUICK START:
  sqncr list-devices              See your MIDI hardware
  sqncr play file.sqnc.yaml -d 0  Play a sequence file
  sqncr generate ""ambient"" -d 0   Generate music

EXAMPLES:
  sqncr generate ""dark ambient in C minor, 70 BPM"" -d 0
  sqncr generate ""jazz chords in Bb"" --device ""Polyend Synth""
  sqncr sessions                  List saved sessions
  sqncr load ""my-jam""             Load and play a session
"
};
```

**Shell completion:**
```csharp
// Add --completions command for bash/zsh/fish
var completionsCommand = new Command("completions", "Generate shell completions");
var shellArg = new Argument<string>("shell", "Shell type (bash, zsh, fish, pwsh)");
completionsCommand.AddArgument(shellArg);

completionsCommand.SetHandler((shell) =>
{
    // Output completion script for the specified shell
    Console.WriteLine(GenerateCompletionScript(shell));
}, shellArg);
```

**Better error messages:**
```csharp
// Instead of stack traces
try
{
    await service.GenerateAndPlayAsync(description, deviceIndex, ct);
}
catch (ArgumentException ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Error: {ex.Message}");
    Console.ResetColor();
    Console.WriteLine("Try: sqncr --help for usage information");
    return 1;
}
```

---

### Task 2: Interactive Mode

```csharp
var interactiveCommand = new Command("interactive", "Start interactive REPL mode");
interactiveCommand.AddAlias("i");

interactiveCommand.SetHandler(async () =>
{
    var service = new SqncRService();
    var deviceIndex = 0;

    Console.WriteLine("SqncR Interactive Mode");
    Console.WriteLine("Type commands or descriptions. Type 'help' for commands, 'quit' to exit.");
    Console.WriteLine();

    while (true)
    {
        Console.Write("sqncr> ");
        var input = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(input)) continue;
        if (input == "quit" || input == "exit") break;

        if (input == "help")
        {
            Console.WriteLine("Commands:");
            Console.WriteLine("  devices        - List MIDI devices");
            Console.WriteLine("  use <n>        - Select device by index");
            Console.WriteLine("  stop           - Stop playback");
            Console.WriteLine("  save <name>    - Save current session");
            Console.WriteLine("  load <name>    - Load session");
            Console.WriteLine("  quit           - Exit");
            Console.WriteLine();
            Console.WriteLine("Or just type a description to generate music:");
            Console.WriteLine("  \"ambient in C minor, 70 BPM\"");
            continue;
        }

        if (input == "devices")
        {
            var midi = new MidiService();
            foreach (var d in midi.ListOutputDevices())
                Console.WriteLine($"  [{d.Index}] {d.Name}");
            continue;
        }

        if (input.StartsWith("use "))
        {
            deviceIndex = int.Parse(input[4..].Trim());
            Console.WriteLine($"Using device {deviceIndex}");
            continue;
        }

        // Treat as generation description
        Console.WriteLine($"Generating: {input}");
        using var cts = new CancellationTokenSource();

        // Run in background so user can type 'stop'
        var playTask = service.GenerateAndPlayAsync(input, deviceIndex, cts.Token);

        // Wait for 'stop' or completion
        Console.WriteLine("Press Enter to stop...");
        Console.ReadLine();
        cts.Cancel();

        try { await playTask; } catch (OperationCanceledException) { }
        Console.WriteLine("Stopped.");
    }
});
```

---

### Task 3: MCP Server Project

```bash
cd src
dotnet new web -n SqncR.McpServer -f net9.0
cd ..
dotnet sln add src/SqncR.McpServer

cd src/SqncR.McpServer
dotnet add reference ../SqncR.Core
dotnet add package ModelContextProtocol  # MCP.NET SDK
```

---

### Task 4: MCP Tool Definitions

**File:** `src/SqncR.McpServer/Tools/ListDevicesTool.cs`

```csharp
using ModelContextProtocol;

namespace SqncR.McpServer.Tools;

[McpTool("list-devices", "List all available MIDI output devices")]
public class ListDevicesTool
{
    private readonly MidiService _midi;

    public ListDevicesTool(MidiService midi)
    {
        _midi = midi;
    }

    [McpToolMethod]
    public McpToolResult Execute()
    {
        var devices = _midi.ListOutputDevices();

        return McpToolResult.Success(new
        {
            devices = devices.Select(d => new
            {
                index = d.Index,
                name = d.Name
            })
        });
    }
}
```

**File:** `src/SqncR.McpServer/Tools/GenerateTool.cs`

```csharp
[McpTool("generate", "Generate music from a natural language description")]
public class GenerateTool
{
    private readonly SqncRService _service;

    public GenerateTool(SqncRService service)
    {
        _service = service;
    }

    [McpToolMethod]
    public async Task<McpToolResult> Execute(
        [McpParameter("description", "What to generate (e.g., 'ambient in C minor, 70 BPM')")] string description,
        [McpParameter("device", "MIDI device index", required: false)] int device = 0)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            await _service.GenerateAndPlayAsync(description, device, cts.Token);

            return McpToolResult.Success(new
            {
                status = "playing",
                description
            });
        }
        catch (Exception ex)
        {
            return McpToolResult.Error(ex.Message);
        }
    }
}
```

---

### Task 5: MCP Server Setup

**File:** `src/SqncR.McpServer/Program.cs`

```csharp
using ModelContextProtocol;
using SqncR.Core;
using SqncR.Midi;
using SqncR.McpServer.Tools;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddSingleton<MidiService>();
builder.Services.AddSingleton<SqncRService>();

// Register MCP
builder.Services.AddMcp(options =>
{
    options.ServerName = "sqncr";
    options.ServerVersion = "0.1.0";
});

// Register tools
builder.Services.AddMcpTool<ListDevicesTool>();
builder.Services.AddMcpTool<GenerateTool>();
builder.Services.AddMcpTool<StopTool>();
builder.Services.AddMcpTool<SaveSessionTool>();
builder.Services.AddMcpTool<LoadSessionTool>();

var app = builder.Build();

// MCP endpoints
app.MapMcp();

app.Run();
```

---

### Task 6: Claude Desktop Configuration

**Config file:** `~/.config/claude/claude_desktop_config.json`

```json
{
  "mcpServers": {
    "sqncr": {
      "command": "dotnet",
      "args": ["run", "--project", "C:/src/SqncR/src/SqncR.McpServer"],
      "env": {}
    }
  }
}
```

Or with published executable:

```json
{
  "mcpServers": {
    "sqncr": {
      "command": "C:/tools/sqncr-mcp.exe",
      "args": []
    }
  }
}
```

---

### Task 7: Test MCP Integration

**Manual test flow:**

1. Start Claude Desktop with config
2. Say: "list my midi devices"
3. Claude calls `list-devices` tool
4. Returns device list

5. Say: "play an ambient drone in C minor on device 0"
6. Claude calls `generate` tool
7. Music plays

---

## Definition of Done

- [ ] CLI has polished help, error messages, completion
- [ ] Interactive REPL mode works
- [ ] MCP server exposes all skills as tools
- [ ] Claude Desktop can call SqncR tools
- [ ] "list my midi devices" works in Claude
- [ ] "play ambient in C minor" works in Claude

---

## Demo

**CLI:**
```bash
# Help
sqncr --help

# Interactive mode
sqncr interactive
sqncr> devices
  [0] Polyend Synth MIDI 1
sqncr> use 0
Using device 0
sqncr> ambient in C minor, 70 BPM
Generating: ambient in C minor, 70 BPM
Press Enter to stop...
```

**Claude Desktop:**
```
You: list my midi devices
Claude: I found these MIDI devices:
        [0] Polyend Synth MIDI 1
        [1] Moog Mother-32

You: play something ambient on the Polyend
Claude: [calls generate tool]
        Started ambient generation in C minor at 80 BPM on Polyend Synth.
```

---

## What's Next

**P5: Production** - Tests, CI/CD, packaging, docs

---

**Priority:** P4
**Status:** Waiting for P3
**Updated:** January 29, 2026
