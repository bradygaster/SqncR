# Decision: Software Synth Telemetry Patterns (Issue #18)

**Date:** 2026-02-16  
**Agent:** Banana Guard  
**Status:** Implemented

## Context
Issue #18 requested OpenTelemetry instrumentation for Sonic Pi and VCV Rack integrations.

## Decisions

1. **Metrics classes follow GenerationMetrics pattern**: Static classes with `public static readonly` Meter and instruments. No OTel SDK dependency in library projects — only `System.Diagnostics.Metrics`.

2. **Metric naming**: `sqncr.{subsystem}.{metric_name}` (e.g., `sqncr.sonicpi.osc_latency_us`, `sqncr.vcvrack.patch_generation_time_ms`). Units in the metric name suffix match the `unit:` parameter.

3. **ObservableGauge registration**: VcvRackMetrics.IsRunning uses a `RegisterIsRunningGauge(Func<int>)` method so the composition root can wire up the callback. This avoids coupling the library to any specific launcher instance.

4. **Span tag naming**: `osc.*` for OSC-specific tags (port, endpoint, message_size_bytes), `sonicpi.*` for Sonic Pi domain tags, `vcvrack.*` for VCV Rack domain tags. Consistent with existing `midi.*` pattern.

5. **Stopwatch placement**: Latency/timing histograms use `Stopwatch` around the minimal operation being measured (UDP send for OscLatency, process launch for LaunchTime, code generation for CodeGenerationTime).
