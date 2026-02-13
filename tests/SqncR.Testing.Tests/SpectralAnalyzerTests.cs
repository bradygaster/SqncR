using SqncR.Testing.Audio;

namespace SqncR.Testing.Tests;

public class SpectralAnalyzerTests
{
    private const int SampleRate = 44100;
    private const double Duration = 1.0;

    [Fact]
    public void Detect440Hz_SineWave_DominantFrequencyIs440Hz()
    {
        var samples = ToneGenerator.GenerateSineWave(440, SampleRate, Duration);
        var result = SpectralAnalyzer.Analyze(samples, SampleRate);

        Assert.NotNull(result.DominantFrequency);
        Assert.InRange(result.DominantFrequency.Frequency, 440 * 0.95, 440 * 1.05);
    }

    [Fact]
    public void DetectC4_261Hz_SineWave_CorrectFrequency()
    {
        var c4Hz = MidiFrequency.MidiToFrequency(60); // 261.63 Hz
        var samples = ToneGenerator.GenerateSineWave(c4Hz, SampleRate, Duration);
        var result = SpectralAnalyzer.Analyze(samples, SampleRate);

        Assert.NotNull(result.DominantFrequency);
        Assert.True(result.ContainsFrequency(c4Hz));
    }

    [Fact]
    public void DetectCMajorChord_AllThreeFrequenciesPresent()
    {
        var c4 = MidiFrequency.MidiToFrequency(60);  // C4 = 261.63 Hz
        var e4 = MidiFrequency.MidiToFrequency(64);  // E4 = 329.63 Hz
        var g4 = MidiFrequency.MidiToFrequency(67);  // G4 = 392.00 Hz

        var samples = ToneGenerator.GenerateChord([c4, e4, g4], SampleRate, Duration);
        var result = SpectralAnalyzer.Analyze(samples, SampleRate);

        Assert.True(result.ContainsFrequency(c4), $"C4 ({c4:F2} Hz) not detected");
        Assert.True(result.ContainsFrequency(e4), $"E4 ({e4:F2} Hz) not detected");
        Assert.True(result.ContainsFrequency(g4), $"G4 ({g4:F2} Hz) not detected");
    }

    [Fact]
    public void Silence_NoSignificantPeaks()
    {
        var samples = ToneGenerator.GenerateSilence(SampleRate, Duration);
        var result = SpectralAnalyzer.Analyze(samples, SampleRate);

        Assert.Empty(result.Peaks);
        Assert.Null(result.DominantFrequency);
    }

    [Fact]
    public void MultipleOctaves_A3AndA4_DetectedAtCorrectFrequencies()
    {
        var a3 = MidiFrequency.MidiToFrequency(57); // 220 Hz
        var a4 = MidiFrequency.MidiToFrequency(69); // 440 Hz

        var samples = ToneGenerator.GenerateChord([a3, a4], SampleRate, Duration);
        var result = SpectralAnalyzer.Analyze(samples, SampleRate);

        Assert.True(result.ContainsFrequency(a3), $"A3 ({a3:F2} Hz) not detected");
        Assert.True(result.ContainsFrequency(a4), $"A4 ({a4:F2} Hz) not detected");
    }

    [Fact]
    public void ContainsFrequency_OutsideTolerance_ReturnsFalse()
    {
        var samples = ToneGenerator.GenerateSineWave(440, SampleRate, Duration);
        var result = SpectralAnalyzer.Analyze(samples, SampleRate);

        // 600 Hz is well outside 5% tolerance of 440 Hz
        Assert.False(result.ContainsFrequency(600));
    }

    [Fact]
    public void HighFrequency_1000Hz_DetectedAccurately()
    {
        var samples = ToneGenerator.GenerateSineWave(1000, SampleRate, Duration);
        var result = SpectralAnalyzer.Analyze(samples, SampleRate);

        Assert.NotNull(result.DominantFrequency);
        Assert.InRange(result.DominantFrequency.Frequency, 950, 1050);
    }

    [Fact]
    public void LowFrequency_100Hz_DetectedAccurately()
    {
        var samples = ToneGenerator.GenerateSineWave(100, SampleRate, Duration);
        var result = SpectralAnalyzer.Analyze(samples, SampleRate);

        Assert.NotNull(result.DominantFrequency);
        Assert.InRange(result.DominantFrequency.Frequency, 95, 105);
    }

    [Fact]
    public void EmptyInput_ReturnsEmptyResult()
    {
        var result = SpectralAnalyzer.Analyze([], SampleRate);

        Assert.Empty(result.Peaks);
        Assert.Null(result.DominantFrequency);
    }

    [Fact]
    public void PeaksSortedByMagnitudeDescending()
    {
        // Chord with different amplitudes would still sort by magnitude
        var c4 = MidiFrequency.MidiToFrequency(60);
        var a4 = MidiFrequency.MidiToFrequency(69);
        var samples = ToneGenerator.GenerateChord([c4, a4], SampleRate, Duration);
        var result = SpectralAnalyzer.Analyze(samples, SampleRate);

        for (var i = 1; i < result.Peaks.Count; i++)
        {
            Assert.True(result.Peaks[i - 1].Magnitude >= result.Peaks[i].Magnitude,
                "Peaks should be sorted by magnitude descending");
        }
    }
}
