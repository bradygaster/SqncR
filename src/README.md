# SqncR Source Code

**This directory will contain all .NET source code**

## Structure (Planned)

```
src/
├── SqncR.AppHost/              # .NET Aspire orchestration
├── SqncR.ServiceDefaults/      # Shared service defaults
├── SqncR.Core/                 # Core business logic
│   ├── Skills/                 # All skills
│   ├── Agents/                 # Autonomous agents
│   └── Services/               # Core services
├── SqncR.Midi/                 # MIDI I/O service
│   ├── Devices/                # Device profiles
│   └── Routing/                # MIDI routing
├── SqncR.Theory/               # Music theory engine
│   ├── Models/                 # Note, Scale, Chord, etc.
│   └── Algorithms/             # Theory algorithms
├── SqncR.State/                # State management
│   ├── Models/                 # EF Core entities
│   └── Repositories/           # Data access
├── SqncR.Cli/                  # CLI tool (sqncr.exe)
├── SqncR.McpServer/            # MCP server
├── SqncR.Api/                  # REST API
└── SqncR.Sdk/                  # .NET SDK library
```

## Status

⏳ **Not yet implemented** - Currently in planning phase (Sprint 00)

See [../docs/ROADMAP.md](../docs/ROADMAP.md) for implementation timeline.

## Getting Started (When Ready)

```powershell
# Initialize solution (Sprint 00)
dotnet new sln -n SqncR

# Create projects
dotnet new aspire-apphost -n SqncR.AppHost
dotnet new classlib -n SqncR.Core
# ... etc

# Run with Aspire
cd SqncR.AppHost
dotnet run
```

## See Also

- [../docs/ARCHITECTURE.md](../docs/ARCHITECTURE.md) - System architecture
- [../docs/ROADMAP.md](../docs/ROADMAP.md) - Implementation roadmap
- [../docs/sprints/sprint_00_foundation.md](../docs/sprints/sprint_00_foundation.md) - First sprint

---

**Current Phase:** Planning & Architecture (✅ Complete)  
**Next Phase:** Sprint 00 - Foundation & Project Setup
