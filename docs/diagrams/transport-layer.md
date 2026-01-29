# Transport Layer Architecture

**All Transports Use Same Core**

```mermaid
flowchart LR
    subgraph "Transport Layer"
        direction TB
        CLI["CLI<br/>System.CommandLine"]
        MCP["MCP Server<br/>MCP.NET SDK"]
        API["REST API<br/>ASP.NET Core"]
        SDK["SDK<br/>Fluent API"]
    end
    
    subgraph "Service Layer (Transport-Agnostic)"
        direction TB
        Core["SqncR.Core<br/>Business Logic"]
        Skills["Skills<br/>Composable"]
        Agents["Agents<br/>Autonomous"]
    end
    
    subgraph "Domain Layer"
        direction TB
        MIDI["MIDI Service"]
        Theory["Theory Service"]
        State["State Service"]
    end
    
    CLI --> Core
    MCP --> Core
    API --> Core
    SDK --> Core
    
    Core --> Skills
    Core --> Agents
    
    Skills --> MIDI
    Skills --> Theory
    Agents --> State
    
    style Core fill:#f0e1ff
    style CLI fill:#ffe1e1
    style MCP fill:#ffe1e1
    style API fill:#ffe1e1
    style SDK fill:#ffe1e1
```

## Key Insight

**SqncR.Core has ZERO transport dependencies.** 

This means:
- ✅ Add new transports (SSH, gRPC, WebSocket) without changing Core
- ✅ All transports get same functionality automatically
- ✅ Business logic is testable without transport concerns
- ✅ Consistent behavior across all interfaces

## Transport Implementations

### CLI (System.CommandLine)
- Command-line tool for direct shell usage
- Uses `System.CommandLine` library
- Executable: `sqncr.exe`

### MCP Server (MCP.NET SDK)
- Model Context Protocol for AI assistants
- Works with Claude Desktop, GitHub Copilot
- Uses `MCP.NET` SDK

### REST API (ASP.NET Core)
- HTTP/JSON interface for web clients
- OpenAPI/Swagger documentation
- Standard REST conventions

### SDK (.NET Library)
- Fluent API for .NET applications
- Strongly-typed interfaces
- Published as NuGet package

---

**See Also:**
- [System Overview](system-overview.md)
- [Service Layer Details](../ARCHITECTURE.md)
