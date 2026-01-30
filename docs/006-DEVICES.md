# Supported Devices

SqncR works with any MIDI device. These have profiles for enhanced support.

---

## Profiled Devices

| Device | Type | Channels | Notes |
|--------|------|----------|-------|
| Polyend Synth | Synth | 1-3 | 3 engines, 8 voices |
| Moog Mother-32 | Synth | 1 | Mono analog |
| Moog DFAM | Drums | via MAFD | 8-step drum machine |
| Sonoclast MAFD | Adapter | - | MIDI to DFAM |
| Polyend MESS | FX | 1 | Step sequencer FX pedal |
| Polyend Play+ | Sampler | 1-8 | Sampler/sequencer |

---

## Generic MIDI

Any MIDI device works. SqncR scans ports and lets you target by name or index.

```bash
sqncr list-devices
# [0] Polyend Synth MIDI 1
# [1] Moog Mother-32
# [2] Some Random Synth

sqncr play song.sqnc.yaml -d 2  # Plays to "Some Random Synth"
```

---

## Device Profiles

Profiles tell SqncR about device capabilities:

```yaml
device:
  name: Polyend Synth
  manufacturer: Polyend
  type: synth
  channels: [1, 2, 3]
  polyphony: 8
  roles: [bass, chords, pads, lead]
  characteristics: [versatile, modern, digital]
```

When SqncR sees "Polyend Synth MIDI 1" in the device list, it loads this profile.

---

## Virtual MIDI

For software synths, use virtual MIDI:

**Windows:** [loopMIDI](https://www.tobias-erichsen.de/software/loopmidi.html)
**macOS:** IAC Driver (built-in)
**Linux:** JACK or ALSA virtual MIDI

---

## See Also

- [../SKILLS.md](../SKILLS.md) - Device-specific skills
- [../examples/](../examples/) - Example sequences
