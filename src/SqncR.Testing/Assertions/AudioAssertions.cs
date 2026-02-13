using SqncR.Testing.Audio;
using Xunit;

namespace SqncR.Testing.Assertions;

/// <summary>
/// xUnit-friendly assertion helpers for audio spectral analysis.
/// </summary>
public static class AudioAssertions
{
    /// <summary>
    /// Assert that the given frequency is present in the audio signal.
    /// </summary>
    public static void AssertFrequencyPresent(float[] samples, int sampleRate, double expectedHz, double tolerancePercent = 5.0)
    {
        var result = SpectralAnalyzer.Analyze(samples, sampleRate);
        Assert.True(
            result.ContainsFrequency(expectedHz, tolerancePercent),
            $"Expected frequency {expectedHz:F2} Hz (±{tolerancePercent}%) to be present in the signal. " +
            $"Detected peaks: [{string.Join(", ", result.Peaks.Select(p => $"{p.Frequency:F2} Hz"))}]");
    }

    /// <summary>
    /// Assert that the given frequency is NOT present in the audio signal.
    /// </summary>
    public static void AssertFrequencyAbsent(float[] samples, int sampleRate, double expectedHz, double tolerancePercent = 5.0)
    {
        var result = SpectralAnalyzer.Analyze(samples, sampleRate);
        Assert.False(
            result.ContainsFrequency(expectedHz, tolerancePercent),
            $"Expected frequency {expectedHz:F2} Hz (±{tolerancePercent}%) to be ABSENT from the signal. " +
            $"Detected peaks: [{string.Join(", ", result.Peaks.Select(p => $"{p.Frequency:F2} Hz"))}]");
    }

    /// <summary>
    /// Assert that the dominant (loudest) frequency matches the expected frequency.
    /// </summary>
    public static void AssertDominantFrequency(float[] samples, int sampleRate, double expectedHz, double tolerancePercent = 5.0)
    {
        var result = SpectralAnalyzer.Analyze(samples, sampleRate);

        Assert.NotNull(result.DominantFrequency);

        var lower = expectedHz * (1 - tolerancePercent / 100.0);
        var upper = expectedHz * (1 + tolerancePercent / 100.0);
        var actual = result.DominantFrequency.Frequency;

        Assert.True(
            actual >= lower && actual <= upper,
            $"Expected dominant frequency to be {expectedHz:F2} Hz (±{tolerancePercent}%), but was {actual:F2} Hz. " +
            $"All peaks: [{string.Join(", ", result.Peaks.Select(p => $"{p.Frequency:F2} Hz"))}]");
    }

    /// <summary>
    /// Assert that the audio signal is effectively silent (RMS below threshold).
    /// </summary>
    public static void AssertSilence(float[] samples, double thresholdDb = -60)
    {
        if (samples.Length == 0)
            return;

        var sumOfSquares = 0.0;
        foreach (var sample in samples)
        {
            sumOfSquares += sample * sample;
        }

        var rms = Math.Sqrt(sumOfSquares / samples.Length);
        var rmsDb = rms > 0 ? 20 * Math.Log10(rms) : double.NegativeInfinity;

        Assert.True(
            rmsDb <= thresholdDb,
            $"Expected silence (RMS ≤ {thresholdDb:F1} dB), but signal RMS was {rmsDb:F1} dB.");
    }
}
