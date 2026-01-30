# Agentic Architecture

Skills, Agents, and MCP integration.

---

## Three Layers

### Skills (Implemented in P3)
Discrete, stateless tasks.
- `chord-progression`
- `bass-line-generator`
- `drone-generator`

### Agents (Future)
Autonomous, stateful entities.
- SessionManager - musical coherence
- Composition - high-level structure
- Listener - real-time adaptation
- DeviceOrchestrator - multi-device coordination

### MCP Server (Implemented in P4)
Exposes skills as tools for AI assistants.

---

## MCP Integration

Claude Desktop calls SqncR via MCP protocol:

```
User: "play ambient in C minor"
Claude: [calls generate tool]
SqncR: [executes skills, plays MIDI]
```

Configuration:
```json
{
  "mcpServers": {
    "sqncr": {
      "command": "sqncr-mcp"
    }
  }
}
```

---

## Skill Composition

Skills chain together:

```
vibe-to-music → chord-progression → bass-line-generator → play
     ↓                  ↓                    ↓
 "darker"        Am → Dm → F → E        bass notes
```

The AI orchestrates this automatically.

---

## See Also

- [../AGENTIC_ARCHITECTURE.md](../AGENTIC_ARCHITECTURE.md) - Full design
- [../SKILLS.md](../SKILLS.md) - Complete skill catalog
- [../sprints/P4-transports.md](../sprints/P4-transports.md) - MCP implementation
