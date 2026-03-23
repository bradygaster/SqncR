# 2026-02-13: Observability and Hardware Reorder

**Requested by:** Brady

## Summary

Finn restructured milestones and added OpenTelemetry observability as a core requirement. Brady directives clarify priorities: prove music works in software first, hardware integration comes last.

## Key Decisions

- **Milestone Restructure:** Hardware MIDI deferred to M4. Software synths moved to M2.
- **Observability Requirement:** OpenTelemetry observability is a core requirement across all milestones.
- **Telemetry Directive:** Every musical event must emit telemetry spans visible in Aspire dashboard.
- **Priority Order:** Hardware integration comes last. Prove music works in software first.

## Participants

- Brady (Direction)
- Finn (Milestone restructure)
