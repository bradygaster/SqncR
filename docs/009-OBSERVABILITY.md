# Observability

Tracing, logging, debugging SqncR.

---

## Console Output

By default, CLI shows what's happening:

```
Playing: Late Night Ambient
Tempo: 70 BPM, Key: Cm
Press Ctrl+C to stop
  ON:  Ch1 C2 vel=70
  ON:  Ch1 C2 vel=68
  OFF: Ch1 C2
```

---

## OpenTelemetry (Future)

Full instrumentation planned:

- Every MIDI message traced
- Skill executions logged
- Latency measured
- Device state tracked

With Aspire Dashboard at `http://localhost:15888`:
- Real-time traces
- Metrics visualization
- Log aggregation

---

## Debugging Tips

**No sound?**
1. Check `sqncr list-devices`
2. Verify device index matches
3. Check MIDI channel (1-16)
4. Hardware powered and connected?

**Wrong timing?**
1. Check tempo in .sqnc.yaml
2. Verify ticks per quarter (default 480)
3. Close other MIDI apps (resource conflicts)

**Latency issues?**
1. Use USB MIDI (faster than DIN)
2. Check audio interface buffer
3. Profile with OpenTelemetry

---

## See Also

- [../OBSERVABILITY.md](../OBSERVABILITY.md) - Full observability design
