# SqncR Architecture Diagrams

**Visual documentation of SqncR system architecture**

All diagrams use Mermaid and are viewable directly in GitHub or any Markdown viewer that supports Mermaid.

## System Architecture

### [System Overview](system-overview.md)
High-level architecture showing all major components: user interfaces, core services, domain services, infrastructure, and hardware.

**Key Concepts:** Transport layers, service facade, skill registry, agents, observability

---

### [Transport Layer Architecture](transport-layer.md)  
Demonstrates how all transports (CLI, MCP, API, SDK) use the same transport-agnostic core.

**Key Concepts:** Service layer, transport independence, extensibility

---

## Execution Flows

### [MIDI Message Flow](midi-message-flow.md)
Sequence diagram showing the complete path from user request to MIDI hardware, including observability tracing.

**Key Concepts:** Request handling, MIDI I/O, latency measurement, telemetry

---

### [Skill Execution Flow](skill-execution-flow.md)
Flowchart of skill discovery, orchestration, and execution with error handling.

**Key Concepts:** Skill registry, composition, observability, error handling

---

## Agent Systems

### [Agent State Machines](agent-state-machines.md)
State machine diagrams for CompositionAgent and ListenerAgent showing autonomous behavior.

**Key Concepts:** State transitions, composition phases, real-time analysis

---

### [Device Orchestration](device-orchestration.md)
Multi-device coordination, role assignment, and voice allocation strategies.

**Key Concepts:** Device roles, layer management, timing coordination, polyphony

---

## Infrastructure

### [Telemetry & Observability](telemetry-observability.md)
OpenTelemetry span hierarchies, metrics map, and Aspire Dashboard integration.

**Key Concepts:** Distributed tracing, metrics, structured logging, performance monitoring

---

## Quick Reference

| Diagram | Focus | Use When |
|---------|-------|----------|
| [System Overview](system-overview.md) | Big picture | Understanding overall architecture |
| [Transport Layer](transport-layer.md) | Extensibility | Adding new interfaces |
| [MIDI Flow](midi-message-flow.md) | Performance | Debugging latency issues |
| [Skill Execution](skill-execution-flow.md) | Composition | Creating new skills |
| [Agent States](agent-state-machines.md) | Behavior | Understanding agents |
| [Device Orchestration](device-orchestration.md) | Multi-device | Complex setups |
| [Telemetry](telemetry-observability.md) | Monitoring | Production operations |

---

## Diagram Conventions

### Colors
- **Purple** - Core services and business logic
- **Green** - Skills and domain logic
- **Red** - Agents and autonomous systems
- **Blue** - Infrastructure and telemetry
- **Light variants** - Supporting components

### Shapes
- **Boxes** - Services, components, systems
- **Diamonds** - Decision points
- **Circles** - Start/end points
- **Dotted lines** - Data flow or optional paths
- **Solid lines** - Control flow or required paths

---

**See Also:**
- [../ARCHITECTURE.md](../ARCHITECTURE.md) - Detailed architecture documentation
- [../AGENTIC_ARCHITECTURE.md](../AGENTIC_ARCHITECTURE.md) - Skills and agents deep dive
- [../OBSERVABILITY.md](../OBSERVABILITY.md) - OpenTelemetry implementation
