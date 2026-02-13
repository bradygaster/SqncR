using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace SqncR.Testing.Audio;

/// <summary>
/// A detected frequency peak in the spectrum.
/// </summary>
/// <param name="Frequency">The frequency in Hz.</param>
/// <param name="Magnitude">The magnitude of the peak.</param>
public record FrequencyPeak(double Frequency, double Magnitude);

/// <summary>
/// Result of spectral analysis containing detected frequency peaks.
/// </summary>
public record SpectralResult
{
    public IReadOnlyList<FrequencyPeak> Peaks { get; init; } = [];

    /// <summary>
    /// The peak with the highest magnitude, or null if no significant peaks.
    /// </summary>
    public FrequencyPeak? DominantFrequency => Peaks.Count > 0 ? Peaks[0] : null;

    /// <summary>
    /// Returns true if any peak is within the given tolerance of the target frequency.
    /// </summary>
    public bool ContainsFrequency(double hz, double tolerancePercent = 5.0)
    {
        var lower = hz * (1 - tolerancePercent / 100.0);
        var upper = hz * (1 + tolerancePercent / 100.0);
        return Peaks.Any(p => p.Frequency >= lower && p.Frequency <= upper);
    }
}

/// <summary>
/// FFT-based spectral analyzer for audio test verification.
/// </summary>
public static class SpectralAnalyzer
{
    /// <summary>
    /// Analyze audio samples and return detected frequency peaks.
    /// </summary>
    /// <param name="samples">Audio samples (mono, float).</param>
    /// <param name="sampleRate">Sample rate in Hz.</param>
    /// <param name="maxPeaks">Maximum number of peaks to return.</param>
    /// <param name="magnitudeThresholdDb">Minimum magnitude in dB relative to max to be considered a peak.</param>
    public static SpectralResult Analyze(float[] samples, int sampleRate, int maxPeaks = 10, double magnitudeThresholdDb = -40)
    {
        if (samples.Length == 0)
            return new SpectralResult();

        // Round up to next power of 2 for FFT
        var fftSize = NextPowerOfTwo(samples.Length);

        // Apply Hanning window and copy to complex array
        var complex = new System.Numerics.Complex[fftSize];
        for (var i = 0; i < samples.Length; i++)
        {
            var window = 0.5 * (1 - Math.Cos(2 * Math.PI * i / (samples.Length - 1)));
            complex[i] = new System.Numerics.Complex(samples[i] * window, 0);
        }

        // Run FFT
        Fourier.Forward(complex, FourierOptions.Matlab);

        // Compute magnitude spectrum (only first half is meaningful)
        var halfSize = fftSize / 2;
        var magnitudes = new double[halfSize];
        for (var i = 0; i < halfSize; i++)
        {
            magnitudes[i] = complex[i].Magnitude;
        }

        // Find the max magnitude for thresholding
        var maxMagnitude = magnitudes.Max();
        if (maxMagnitude < 1e-10)
            return new SpectralResult();

        var thresholdLinear = maxMagnitude * Math.Pow(10, magnitudeThresholdDb / 20.0);

        // Find peaks: local maxima above threshold
        var peaks = new List<FrequencyPeak>();
        var freqResolution = (double)sampleRate / fftSize;

        for (var i = 1; i < halfSize - 1; i++)
        {
            if (magnitudes[i] > magnitudes[i - 1] &&
                magnitudes[i] > magnitudes[i + 1] &&
                magnitudes[i] > thresholdLinear)
            {
                // Parabolic interpolation for better frequency accuracy
                var alpha = magnitudes[i - 1];
                var beta = magnitudes[i];
                var gamma = magnitudes[i + 1];
                var p = 0.5 * (alpha - gamma) / (alpha - 2 * beta + gamma);
                var interpolatedBin = i + p;
                var frequency = interpolatedBin * freqResolution;
                peaks.Add(new FrequencyPeak(frequency, magnitudes[i]));
            }
        }

        // Sort by magnitude descending and take top N
        var sortedPeaks = peaks
            .OrderByDescending(p => p.Magnitude)
            .Take(maxPeaks)
            .ToList();

        return new SpectralResult { Peaks = sortedPeaks };
    }

    private static int NextPowerOfTwo(int value)
    {
        var result = 1;
        while (result < value)
            result <<= 1;
        return result;
    }
}
