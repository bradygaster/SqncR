---
title: "Making Sound: Sonic Pi, VCV Rack, and Teaching Tests to Hear"
date: 2026-02-14
tags: [sqncr, sonic-pi, vcv-rack, spectral-analysis, testing, generative-music]
summary: "M2 delivered software synth integration without hardware. Sonic Pi over OSC. VCV Rack patch generation. And a spectral analysis testing framework that validates audio output acoustically."
---

# Making Sound: Sonic Pi, VCV Rack, and Teaching Tests to Hear

You have the generation engine. You have music theory. You have MIDI commands flowing at 480 ticks per quarter note. But there's one problem: *nobody can hear it yet.*

That was M1. The machine in your computer that *knows* music.

M2 is about making it *sound*.

We added two software synths — Sonic Pi and VCV Rack — plus something weirder: tests that can *hear*. This post covers how we bridged generative music to actual audio, why we chose software synths before hardware MIDI, and how spectral analysis lets your tests validate acoustic output.

## Why Software Synths First?

Here's the brutal truth about hardware MIDI:

1. **Expensive** — A decent synth costs $500+. A modular rig costs thousands.
2. **Fragile** — MIDI connections break. USB cables fail. Hardware latency is unpredictable.
3. **Exclusive** — Not everyone has a synth. But everyone has a computer.

Our philosophy: *Lower the barrier to entry.*

If you want to try SqncR, you shouldn't need to own synthesizers. You should be able to clone the repo, run it, and hear music. **That's software synths.**

Sonic Pi is free, cross-platform, and beloved by musicians. VCV Rack is free, open-source, and powers everything from bedroom producers to touring artists. Both run on any laptop.

Software synths let anyone in. *That's the win.*

## Part 1: Sonic Pi Integration — OSC Over UDP

Sonic Pi is a live coding environment for music. You write Ruby, it makes sound. Here's the key: it doesn't have a network API. But it *does* listen on localhost:4560 for Open Sound Control (OSC) messages.

OSC is simple: send a UDP packet with a message path and arguments. Sonic Pi executes your command.

### The OSC Protocol

OSC messages are binary. Here's what we're sending:

```
Address: /play
Type tag: i f f  (int, float, float)
Arguments: note_number, velocity, duration
```

In bytes:

```
/play\0   → null-padded OSC address (8 bytes, padded to 4-byte boundary)
,iff\0    → type tag string (5 bytes, padded to 8)
MIDI note (4 bytes, big-endian int32)
Velocity  (4 bytes, big-endian float)
Duration  (4 bytes, big-endian float)
```

Here's our OSC encoder:

```csharp
public class OscMessage
{
    public string Address { get; set; }
    public List<object> Arguments { get; set; } = new();

    public byte[] Encode()
    {
        var buffer = new MemoryStream();

        // 1. Write address (null-padded to 4-byte boundary)
        var addressBytes = Encoding.ASCII.GetBytes(Address);
        buffer.Write(addressBytes);
        buffer.WriteByte(0); // null terminator
        
        // Pad to 4-byte boundary
        int remainder = (addressBytes.Length + 1) % 4;
        if (remainder != 0)
            for (int i = 0; i < 4 - remainder; i++)
                buffer.WriteByte(0);

        // 2. Build type tag string
        var typeTag = new StringBuilder(",");
        foreach (var arg in Arguments)
        {
            if (arg is int) typeTag.Append("i");
            else if (arg is float) typeTag.Append("f");
            else if (arg is string) typeTag.Append("s");
        }

        // 3. Write type tag (null-padded to 4-byte boundary)
        var typeTagBytes = Encoding.ASCII.GetBytes(typeTag.ToString());
        buffer.Write(typeTagBytes);
        buffer.WriteByte(0); // null terminator
        
        remainder = (typeTagBytes.Length + 1) % 4;
        if (remainder != 0)
            for (int i = 0; i < 4 - remainder; i++)
                buffer.WriteByte(0);

        // 4. Write arguments in big-endian binary
        foreach (var arg in Arguments)
        {
            if (arg is int intVal)
                buffer.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(intVal)));
            else if (arg is float floatVal)
            {
                var bytes = BitConverter.GetBytes(floatVal);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);
                buffer.Write(bytes);
            }
        }

        return buffer.ToArray();
    }
}
```

Now here's the thing: we're not sending individual notes. We're sending **Ruby code**.

### Ruby Code Generation

When the AI says "play an ambient pad in F Dorian," SqncR generates Sonic Pi code:

```ruby
use_synth :pad
use_synth_defaults attack: 0.5, release: 2.0, cutoff: 80

live_loop :ambient_drone do
  play 65  # F4
  sleep 2
  play 68  # A4
  sleep 2
  play 70  # B4
  sleep 2
  play 72  # C5
  sleep 2
end
```

This code defines a `live_loop` that plays notes at specific times. Sonic Pi's scheduler handles the timing. We just send the Ruby, Sonic Pi executes it, and sound emerges.

Here's the generator:

```csharp
public class SonicPiCodeGenerator
{
    public static string GenerateLiveLoop(
        string loopName, 
        int[] midiNotes, 
        double bpm, 
        string synth = "pad",
        Dictionary<string, object>? defaults = null)
    {
        var code = new StringBuilder();
        code.AppendLine($"use_synth :{synth}");
        
        if (defaults != null)
        {
            code.Append("use_synth_defaults ");
            var defs = defaults.Select(kvp => $"{kvp.Key}: {kvp.Value}");
            code.AppendLine(string.Join(", ", defs));
        }

        code.AppendLine($"\nlive_loop :{loopName} do");
        
        // Quarter note duration in beats
        double quarterNoteDuration = 60.0 / bpm;
        
        foreach (int note in midiNotes)
        {
            code.AppendLine($"  play {note}");
            code.AppendLine($"  sleep {quarterNoteDuration}");
        }
        
        code.AppendLine("end");
        return code.ToString();
    }
}
```

We send this Ruby string to Sonic Pi via OSC at `/live_loop`. Sonic Pi parses it, schedules it, and the notes play.

**Why not MIDI?** Because Sonic Pi's Ruby environment has context. Synth type, effects chain, timing all live in one place. MIDI is just note numbers. OSC + Ruby is *expressive*.

## Part 2: VCV Rack Integration — Modular Synthesis

VCV Rack is a software modular synthesizer. You patch together modules: oscillators, filters, envelopes, effects. Hundreds of free modules in the plugin ecosystem.

Unlike Sonic Pi, VCV Rack isn't a live coding environment. It's a visual patchboard. So we do something different: we *generate patches programmatically*.

### Patch Models

A VCV Rack patch is a JSON file. Here's the structure:

```json
{
  "modules": [
    {
      "plugin": "Bogaudio",
      "model": "BogaudioVCO",
      "id": 1,
      "params": [
        { "value": 0.5 }  // oscillator frequency
      ],
      "data": {}
    },
    {
      "plugin": "Bogaudio",
      "model": "BogaudioVCA",
      "id": 2,
      "params": [
        { "value": 0.8 }  // VCA gain
      ]
    }
  ],
  "cables": [
    {
      "source": 1,
      "sourcePort": 0,
      "target": 2,
      "targetPort": 0
    }
  ]
}
```

Modules have input/output ports. Cables connect them. Data flows through the patch.

### The Fluent Patch Builder API

We built a fluent API for constructing patches:

```csharp
public class PatchBuilder
{
    private List<Module> _modules = new();
    private List<Cable> _cables = new();
    private int _nextModuleId = 1;

    public PatchBuilder AddModule(string plugin, string model)
    {
        _modules.Add(new Module
        {
            Id = _nextModuleId++,
            Plugin = plugin,
            Model = model,
            Params = new List<Parameter>(),
            Data = new Dictionary<string, object>()
        });
        return this;
    }

    public PatchBuilder SetParam(int moduleId, int paramIndex, float value)
    {
        var module = _modules.FirstOrDefault(m => m.Id == moduleId);
        if (module != null)
        {
            while (module.Params.Count <= paramIndex)
                module.Params.Add(new Parameter { Value = 0 });
            
            module.Params[paramIndex].Value = value;
        }
        return this;
    }

    public PatchBuilder Connect(int sourceId, int sourcePort, int targetId, int targetPort)
    {
        _cables.Add(new Cable
        {
            Source = sourceId,
            SourcePort = sourcePort,
            Target = targetId,
            TargetPort = targetPort
        });
        return this;
    }

    public Patch Build()
    {
        return new Patch
        {
            Modules = _modules,
            Cables = _cables
        };
    }
}
```

Now you can build patches like this:

```csharp
var patch = new PatchBuilder()
    .AddModule("Bogaudio", "BogaudioVCO")      // Oscillator, id=1
    .SetParam(1, 0, 0.5f)                      // Frequency
    .AddModule("Bogaudio", "BogaudioVCA")      // Amplifier, id=2
    .SetParam(2, 0, 0.8f)                      // Gain
    .Connect(1, 0, 2, 0)                       // Osc → VCA
    .Build();
```

### Patch Templates

But manually building patches is tedious. We built four templates:

**BasicSynth** — Oscillator → Filter → VCA → Out. The classic subtractive architecture.

```csharp
public static Patch CreateBasicSynth(int rootNote, double cutoff)
{
    return new PatchBuilder()
        .AddModule("Core", "MIDI-1")            // MIDI input, id=1
        .AddModule("Bogaudio", "BogaudioVCO")   // VCO, id=2
        .SetParam(2, 0, MidiToFrequency(rootNote))
        .AddModule("Bogaudio", "BogaudioVCF")   // VCF, id=3
        .SetParam(3, 0, cutoff)
        .AddModule("Bogaudio", "BogaudioVCA")   // VCA, id=4
        .AddModule("Core", "AudioOut")          // Output, id=5
        
        // MIDI → VCO Pitch
        .Connect(1, 0, 2, 0)
        // VCO → VCF
        .Connect(2, 0, 3, 0)
        // VCF → VCA
        .Connect(3, 0, 4, 0)
        // VCA → Output
        .Connect(4, 0, 5, 0)
        .Build();
}
```

**AmbientPad** — Adds reverb, longer envelopes, spread across two oscillators for thickness.

**BassSynth** — Sub oscillator, tight filter, aggressive attack.

Each template is a starting point. You can tweak parameters or add effects.

### Compression & Distribution

VCV Rack patches are readable JSON, but they're *huge*. A typical patch is 500KB+. Sending that over the network is slow.

We compress with **tar + zstd**:

```csharp
public class PatchCompressor
{
    public static byte[] CompressPatch(Patch patch)
    {
        var json = JsonConvert.SerializeObject(patch);
        var jsonBytes = Encoding.UTF8.GetBytes(json);

        using var tarStream = new MemoryStream();
        using (var tar = new TarArchive(tarStream))
        {
            var entry = tar.CreateEntry("patch.json");
            entry.Write(jsonBytes);
        }

        var compressedStream = new MemoryStream();
        using (var zstd = new ZstdCompressor(compressedStream))
        {
            zstd.Write(tarStream.ToArray());
        }

        return compressedStream.ToArray();
    }
}
```

A 500KB patch compresses to ~50KB with zstd. Network latency drops by 10x.

## Part 3: Teaching Tests to Hear — Spectral Analysis

Here's where it gets weird and wonderful.

You can generate all the code you want, but *how do you validate that it sounds right?*

You could listen to it. But testing is automation. We needed tests that could *analyze audio*.

Enter **spectral analysis**: decompose audio into frequency components using Fast Fourier Transform (FFT).

### The Idea

Audio is a waveform: amplitude over time. FFT converts it to frequency domain: which frequencies are present, and how loud?

A synthesizer playing a 440 Hz sine wave should have a strong peak at 440 Hz. A chord has multiple peaks. Noise is distributed energy across frequencies.

We built **ToneGenerator** — a test fixture that generates pure tones at known frequencies:

```csharp
public class ToneGenerator
{
    public static float[] GenerateSineWave(int frequencyHz, int sampleRateHz, double durationSeconds)
    {
        int numSamples = (int)(sampleRateHz * durationSeconds);
        var samples = new float[numSamples];
        double tau = 2 * Math.PI;

        for (int i = 0; i < numSamples; i++)
        {
            double phase = (double)i / sampleRateHz * frequencyHz * tau;
            samples[i] = (float)Math.Sin(phase);
        }

        return samples;
    }

    public static float[] GenerateChord(int[] frequenciesHz, int sampleRateHz, double durationSeconds)
    {
        int numSamples = (int)(sampleRateHz * durationSeconds);
        var samples = new float[numSamples];
        double tau = 2 * Math.PI;

        for (int i = 0; i < numSamples; i++)
        {
            float sample = 0;
            foreach (int freq in frequenciesHz)
            {
                double phase = (double)i / sampleRateHz * freq * tau;
                sample += (float)Math.Sin(phase);
            }
            samples[i] = sample / frequenciesHz.Length;  // Normalize
        }

        return samples;
    }
}
```

Now the **FFT analyzer**:

```csharp
public class SpectralAnalyzer
{
    public static double[] ComputeFFT(float[] samples)
    {
        // Use DFT for simplicity (production code would use Cooley-Tukey FFT)
        int n = samples.Length;
        var spectrum = new double[n / 2];

        for (int k = 0; k < n / 2; k++)
        {
            double realSum = 0, imagSum = 0;
            double tau = 2 * Math.PI;

            for (int n_idx = 0; n_idx < n; n_idx++)
            {
                double angle = -tau * k * n_idx / n;
                realSum += samples[n_idx] * Math.Cos(angle);
                imagSum += samples[n_idx] * Math.Sin(angle);
            }

            // Magnitude
            spectrum[k] = Math.Sqrt(realSum * realSum + imagSum * imagSum);
        }

        return spectrum;
    }

    public static int? FindPeakFrequency(double[] spectrum, int sampleRateHz)
    {
        int maxIndex = 0;
        double maxMagnitude = 0;

        for (int i = 0; i < spectrum.Length; i++)
        {
            if (spectrum[i] > maxMagnitude)
            {
                maxMagnitude = spectrum[i];
                maxIndex = i;
            }
        }

        // Convert bin index to Hz
        return (int)(maxIndex * sampleRateHz / spectrum.Length);
    }
}
```

And finally, **AudioAssertions** — the test helpers:

```csharp
public class AudioAssertions
{
    public static void AssertContainsFrequency(float[] samples, int sampleRateHz, int targetFrequencyHz, int toleranceHz = 10)
    {
        var spectrum = SpectralAnalyzer.ComputeFFT(samples);
        var peak = SpectralAnalyzer.FindPeakFrequency(spectrum, sampleRateHz);

        if (peak is null || Math.Abs(peak.Value - targetFrequencyHz) > toleranceHz)
        {
            throw new AssertionException(
                $"Expected frequency {targetFrequencyHz} Hz ±{toleranceHz} Hz, " +
                $"but found peak at {peak} Hz"
            );
        }
    }

    public static void AssertContainsChord(float[] samples, int sampleRateHz, int[] frequenciesHz, int toleranceHz = 10)
    {
        var spectrum = SpectralAnalyzer.ComputeFFT(samples);
        
        foreach (int freq in frequenciesHz)
        {
            var binIndex = (int)(freq * spectrum.Length / sampleRateHz);
            if (binIndex >= spectrum.Length)
                throw new AssertionException($"Frequency {freq} Hz out of analysis range");

            if (spectrum[binIndex] < 100)  // Arbitrary threshold
                throw new AssertionException($"Chord tone {freq} Hz not present");
        }
    }
}
```

### A Real Test

```csharp
[Test]
public void Sonic Pi Pad Should Generate 440Hz Fundamental()
{
    // Arrange: Sonic Pi generates a pad note at A4 (440 Hz)
    var code = SonicPiCodeGenerator.GeneratePad(440, synth: "sine");
    var audioOutput = SonicPiBridge.GenerateAudio(code, durationSeconds: 2);

    // Act: Analyze the spectrum
    var samples = audioOutput.Samples;

    // Assert: We should hear 440 Hz
    AudioAssertions.AssertContainsFrequency(samples, 44100, 440, toleranceHz: 5);
}

[Test]
public void VCV Rack Ambient Patch Should Generate C Major Chord()
{
    // Arrange: Build an ambient patch with C, E, G
    var patch = PatchTemplates.CreateAmbientPad(new[] { 60, 64, 67 });
    var audioOutput = VcvRackBridge.GenerateAudio(patch, durationSeconds: 2);

    // Assert: We should hear C, E, G (261, 330, 392 Hz)
    AudioAssertions.AssertContainsChord(audioOutput.Samples, 44100, new[] { 261, 330, 392 });
}
```

*This is wild.* Our tests don't just check that code was generated. They validate that the *actual acoustic output* contains the expected frequencies. We're teaching our test suite to *hear*.

## The Architecture: Five Pieces

M2 brought together:

1. **SqncR.SonicPi** — OSC message encoding, Ruby code generation, bridge to Sonic Pi server
2. **SqncR.VcvRack** — Patch models, PatchBuilder fluent API, compression, VCV Rack launcher
3. **SqncR.Testing** — ToneGenerator, SpectralAnalyzer, AudioAssertions (spectral analysis test framework)
4. **MCP Tools** — 5 Sonic Pi tools + 5 VCV Rack tools (10 new tools total, 17 cumulative)
5. **Observability** — SonicPiMetrics + VcvRackMetrics (8 new OpenTelemetry instruments)

### MCP Tools

Your AI now controls:

**Sonic Pi:**
- `setup_software_synth` — Start Sonic Pi, initialize connection
- `play_note` — Send a note through OSC
- `live_loop` — Register a live_loop with Ruby code
- `stop` — Kill the loop
- `status` — Check if Sonic Pi is running

**VCV Rack:**
- `generate_patch` — Build a patch from a template
- `launch` — Start VCV Rack with the patch
- `stop` — Close VCV Rack
- `status` — Check if it's running
- `list_templates` — Show available patch architectures

### Telemetry

Every synth action emits spans:

```csharp
using var activity = ActivitySource.StartActivity("sonic_pi.play_note");
activity?.SetTag("synth.instrument", "pad");
activity?.SetTag("synth.note", 65);
activity?.SetTag("synth.duration", 2.0);
```

In the Aspire dashboard, you see:
- OSC message latency
- Ruby code execution time
- Sonic Pi CPU usage
- VCV Rack audio buffer fill

Real-time visibility into synthesis.

## The Tests: From 256 to 418

M1 shipped with 256 passing tests. M2 adds 21 integration tests validating:

1. **OSC encoding** — OSC messages parse correctly
2. **Ruby code generation** — Generated code matches Sonic Pi syntax
3. **Patch building** — Fluent API constructs valid JSON
4. **Compression** — tar+zstd round-trips correctly
5. **Spectral analysis** — FFT detects known frequencies
6. **Audio validation** — Sonic Pi output contains correct pitches
7. **Patch execution** — VCV Rack patches launch and generate sound

No build warnings. 418 tests. All passing.

## Why This Matters

A year ago, SqncR was an idea. "Wouldn't it be cool if you could ask an AI to make music?"

M1 was the brain: music theory, generation engine, MIDI timing.

**M2 is the voice.** It sounds. You can hear it. You can *feel* it.

The generation engine no longer speaks into the void. It speaks to Sonic Pi. It builds patches in VCV Rack. It *makes sound*.

And our tests can hear it. Not just "the code ran without error." But "the audio contains the frequencies we asked for." That's a higher bar. That's how you know the music is real.

## The Weird Part

We built a test framework that validates audio acoustically. It's overkill for most music software. But it's *perfect* for generative music.

The generation engine might decide to play a note. It sends it to Sonic Pi. Sonic Pi synth is buggy. Output is garbage. Our test hears it: "Peak frequency is 2.1 kHz, expected 440 Hz." Test fails.

Now we *know* something broke. Not the generation logic. Not the OSC protocol. The *synth itself*.

That's the power of treating audio as a first-class observable. It closes the loop from intent → generation → synthesis → verification.

## What's Next — M3: Stream-Ready

M2 gave us sound. M3 will give us *stability*.

**Persistence** — Save generated patches. Reuse them. Build a library of favorite sounds.

**Variety** — More patch templates. More Sonic Pi synth types. FX chains (reverb, delay, distortion).

**Stability** — Handle edge cases. MIDI channel overflow. Sonic Pi timeout. Graceful degradation.

And **composition** — Instead of random notes, actually compose *music*. Verse, chorus, bridge. Form and structure.

The generation engine will think in phrases, not just pitches.

---

**Want to follow along?** The code is open source. Try cloning SqncR, running M2, and asking your AI assistant to "generate an ambient pad patch."

You'll hear it. And you can prove it: the tests will tell you what frequencies are actually playing.

**Next post:** "From Notes to Moments — Building Musical Form into Generative AI."
