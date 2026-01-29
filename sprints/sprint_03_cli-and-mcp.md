# Sprint 03: CLI & MCP Transports

**Duration:** 2 weeks  
**Goal:** Build CLI tool and MCP server, both using SqncR.Core service layer

---

## Sprint Objectives

- ✅ CLI tool with list-devices, generate, modify commands
- ✅ MCP server with tools and resources
- ✅ Both transports use same SqncR.Core
- ✅ CLI works standalone
- ✅ MCP works in Claude Desktop
- ✅ All operations traced in Aspire Dashboard

---

## User Stories

### US-03-01: CLI tool
**As a user, I want to control SqncR from command line**

**Acceptance Criteria:**
- [ ] `sqncr list-devices` shows devices
- [ ] `sqncr generate "ambient drone"` starts music
- [ ] `sqncr modify "darker"` changes generation
- [ ] `sqncr stop` stops generation
- [ ] Rich console output
- [ ] Works without Aspire running (standalone mode)

### US-03-02: MCP server
**As a user, I want to control SqncR from Claude Desktop**

**Acceptance Criteria:**
- [ ] MCP server exposes list-devices tool
- [ ] MCP server exposes generate tool
- [ ] MCP server exposes modify tool
- [ ] Resources available (devices, session)
- [ ] Works in Claude Desktop
- [ ] All tool calls traced

### US-03-03: Transport independence
**As a developer, I verify Core is transport-agnostic**

**Acceptance Criteria:**
- [ ] SqncR.Core has zero CLI dependencies
- [ ] SqncR.Core has zero MCP dependencies
- [ ] Same skill execution from CLI and MCP
- [ ] Both produce identical traces in dashboard

---

## Tasks

### Task 1: Create CLI Project

```bash
cd src
dotnet new console -n SqncR.Cli -f net9.0
dotnet sln add SqncR.Cli
cd SqncR.Cli
dotnet add reference ../SqncR.Core
dotnet add package System.CommandLine --prerelease
dotnet add package Spectre.Console
```

**Checklist:**
- [ ] Create project
- [ ] Add System.CommandLine
- [ ] Add Spectre.Console for rich output
- [ ] Reference SqncR.Core

**Estimated Time:** 1 hour

---

### Task 2: Implement CLI Commands

**File:** `src/SqncR.Cli/Commands/ListDevicesCommand.cs`

```csharp
public class ListDevicesCommand : Command
{
    public ListDevicesCommand() : base("list-devices", "List all MIDI devices")
    {
        this.SetHandler(ExecuteAsync);
    }
    
    private async Task ExecuteAsync()
    {
        var service = CreateSqncRService();
        var devices = await service.ListDevicesAsync();
        
        var table = new Table();
        table.AddColumn("Index");
        table.AddColumn("Device Name");
        table.AddColumn("Type");
        table.AddColumn("Channels");
        
        foreach (var device in devices)
        {
            table.AddRow(
                device.Index.ToString(),
                device.PortName,
                device.Type.ToString(),
                string.Join(", ", device.Channels)
            );
        }
        
        AnsiConsole.Write(table);
    }
}
```

**File:** `src/SqncR.Cli/Commands/GenerateCommand.cs`

```csharp
public class GenerateCommand : Command
{
    public GenerateCommand() : base("generate", "Generate music")
    {
        var descArg = new Argument<string>("description", "Musical description");
        AddArgument(descArg);
        
        this.SetHandler(ExecuteAsync, descArg);
    }
    
    private async Task ExecuteAsync(string description)
    {
        var service = CreateSqncRService();
        
        AnsiConsole.Status()
            .Start("Generating music...", async ctx =>
            {
                var result = await service.GenerateAsync(new GenerationRequest
                {
                    Description = description
                });
                
                if (result.Success)
                {
                    AnsiConsole.MarkupLine("[green]✓[/] Music playing!");
                    AnsiConsole.WriteLine($"Key: {result.Key}, Tempo: {result.Tempo} BPM");
                }
            });
    }
}
```

**Checklist:**
- [ ] Implement list-devices command
- [ ] Implement generate command
- [ ] Implement modify command
- [ ] Implement stop command
- [ ] Rich console output with Spectre.Console
- [ ] Error handling with user-friendly messages

**Estimated Time:** 6 hours

---

### Task 3: CLI Dependency Injection Setup

**File:** `src/SqncR.Cli/Program.cs`

```csharp
var services = new ServiceCollection();

// Add SqncR services
services.AddSqncRCore();
services.AddSqncRMidi();
services.AddSqncRTheory();
services.AddSqncRState();

// Add logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Warning);  // Quiet for CLI
});

var serviceProvider = services.BuildServiceProvider();

// Build command tree
var rootCommand = new RootCommand("SqncR - AI-Native Generative Music for MIDI");
rootCommand.AddCommand(new ListDevicesCommand(serviceProvider));
rootCommand.AddCommand(new GenerateCommand(serviceProvider));
rootCommand.AddCommand(new ModifyCommand(serviceProvider));
rootCommand.AddCommand(new StopCommand(serviceProvider));

return await rootCommand.InvokeAsync(args);
```

**Checklist:**
- [ ] Set up DI container
- [ ] Register all SqncR services
- [ ] Configure logging (quiet for CLI)
- [ ] Build command tree
- [ ] Test: `dotnet run -- list-devices`

**Estimated Time:** 2 hours

---

### Task 4: Create MCP Server Project

```bash
cd src
dotnet new web -n SqncR.McpServer -f net9.0
dotnet sln add SqncR.McpServer
cd SqncR.McpServer
dotnet add reference ../SqncR.Core
dotnet add package ModelContextProtocol.NET
```

**Checklist:**
- [ ] Create web project
- [ ] Add MCP.NET SDK package
- [ ] Reference SqncR.Core
- [ ] Remove default web template code

**Estimated Time:** 1 hour

---

### Task 5: Implement MCP Tools

**File:** `src/SqncR.McpServer/Tools/ListDevicesTool.cs`

```csharp
public class ListDevicesTool : IMcpTool
{
    private readonly SqncRService _sqncr;
    
    public string Name => "sqncr.devices.list";
    public string Description => "Lists all connected MIDI devices";
    
    public async Task<McpToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var devices = await _sqncr.ListDevicesAsync();
        return new McpToolResult
        {
            Content = JsonSerializer.Serialize(devices)
        };
    }
}
```

**Checklist:**
- [ ] Implement sqncr.devices.list tool
- [ ] Implement sqncr.generate.start tool
- [ ] Implement sqncr.generate.modify tool
- [ ] Implement sqncr.generate.stop tool
- [ ] JSON serialization for responses
- [ ] Error handling per MCP spec

**Estimated Time:** 6 hours

---

### Task 6: Implement MCP Resources

**File:** `src/SqncR.McpServer/Resources/DevicesResource.cs`

```csharp
public class DevicesResource : IMcpResource
{
    public string Uri => "sqncr://devices";
    public string Description => "List of configured MIDI devices";
    
    public async Task<McpResourceContent> GetAsync()
    {
        var devices = await _sqncr.ListDevicesAsync();
        return new McpResourceContent
        {
            MimeType = "application/json",
            Content = JsonSerializer.Serialize(devices)
        };
    }
}
```

**Checklist:**
- [ ] Implement sqncr://devices resource
- [ ] Implement sqncr://session/current resource
- [ ] Resource polling/updates
- [ ] Error handling

**Estimated Time:** 3 hours

---

### Task 7: MCP Server Host Configuration

**File:** `src/SqncR.McpServer/Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();  // Aspire + OpenTelemetry

// Add SqncR services
builder.Services.AddSqncRCore();
builder.Services.AddSqncRMidi();
builder.Services.AddSqncRTheory();
builder.Services.AddSqncRState();

// Add MCP services
builder.Services.AddMcpServer();
builder.Services.AddSingleton<IMcpTool, ListDevicesTool>();
builder.Services.AddSingleton<IMcpTool, GenerateTool>();
// ... etc

var app = builder.Build();

app.MapMcpEndpoints();  // MCP protocol endpoints

app.Run();
```

**Checklist:**
- [ ] Configure Aspire ServiceDefaults
- [ ] Register SqncR services
- [ ] Register MCP tools and resources
- [ ] Configure MCP protocol endpoints
- [ ] Test server starts

**Estimated Time:** 3 hours

---

### Task 8: Test MCP Server with Claude Desktop

**Claude Desktop config:**
```json
{
  "mcpServers": {
    "sqncr": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:/src/SqncR/src/SqncR.McpServer/SqncR.McpServer.csproj"
      ]
    }
  }
}
```

**Test in Claude:**
```
You: "use sqncr to list my midi devices"
Claude: [calls sqncr.devices.list tool]
Claude: "You have 4 devices: Polyend Synth, Moog Mother-32, MAFD, Polyend MESS"

You: "start an ambient drone in A minor on the Polyend"
Claude: [calls sqncr.generate.start]
[Music plays from Polyend Synth]
```

**Checklist:**
- [ ] Add SqncR to Claude Desktop config
- [ ] Restart Claude Desktop
- [ ] Verify MCP server starts
- [ ] Test list-devices tool
- [ ] Test generate tool
- [ ] Verify traces in Aspire Dashboard

**Estimated Time:** 3 hours

---

### Task 9: Publish CLI as Executable

```bash
cd src/SqncR.Cli
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

**Checklist:**
- [ ] Configure publish settings
- [ ] Publish for Windows (win-x64)
- [ ] Publish for macOS (osx-arm64)
- [ ] Publish for Linux (linux-x64)
- [ ] Test executable runs standalone
- [ ] Document installation

**Estimated Time:** 2 hours

---

## Definition of Done

- ✅ CLI tool works: `sqncr list-devices`, `sqncr generate`
- ✅ MCP server works in Claude Desktop
- ✅ Both use same SqncR.Core (verified)
- ✅ All operations traced in Aspire Dashboard
- ✅ CLI published as executable
- ✅ MCP config documented
- ✅ Tests pass
- ✅ Documentation updated

---

## Deliverables

1. **sqncr.exe** - CLI tool (cross-platform)
2. **SqncR.McpServer** - MCP server for Claude/Copilot
3. **Working demos** - CLI and MCP both control music
4. **Documentation** - Installation and usage guides

---

## Demo Script

**CLI Demo:**
```bash
sqncr list-devices
# Shows table of devices

sqncr generate "ambient drone in A minor, 60 BPM, Polyend Synth channel 1"
# Music plays

sqncr modify "darker"
# Music shifts to Phrygian

sqncr stop
# Music stops
```

**MCP Demo in Claude:**
```
You: "list my midi devices"
You: "start an ambient drone on the Polyend"
You: "make it darker"
You: "stop"
```

**Show Aspire Dashboard** - Same traces from both interfaces!

---

**Sprint Status:** 🔲 Not Started  
**Updated:** January 29, 2026
