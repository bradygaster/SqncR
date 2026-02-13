namespace SqncR.Testing.Audio;

/// <summary>
/// Generates test audio signals for spectral analysis verification.
/// </summary>
public static class ToneGenerator
{
    /// <summary>
    /// Generate a pure sine wave at the given frequency.
    /// </summary>
    public static float[] GenerateSineWave(double frequencyHz, int sampleRate, double durationSeconds, double amplitude = 1.0)
    {
        var numSamples = (int)(sampleRate * durationSeconds);
        var samples = new float[numSamples];
        var angularFrequency = 2 * Math.PI * frequencyHz / sampleRate;

        for (var i = 0; i < numSamples; i++)
        {
            samples[i] = (float)(amplitude * Math.Sin(angularFrequency * i));
        }

        return samples;
    }

    /// <summary>
    /// Generate a chord by summing multiple sine waves, normalized to prevent clipping.
    /// </summary>
    public static float[] GenerateChord(double[] frequencies, int sampleRate, double durationSeconds)
    {
        var numSamples = (int)(sampleRate * durationSeconds);
        var samples = new float[numSamples];

        if (frequencies.Length == 0)
            return samples;

        var amplitude = 1.0 / frequencies.Length;

        foreach (var freq in frequencies)
        {
            var angularFrequency = 2 * Math.PI * freq / sampleRate;
            for (var i = 0; i < numSamples; i++)
            {
                samples[i] += (float)(amplitude * Math.Sin(angularFrequency * i));
            }
        }

        return samples;
    }

    /// <summary>
    /// Generate silence (all zeros).
    /// </summary>
    public static float[] GenerateSilence(int sampleRate, double durationSeconds)
    {
        var numSamples = (int)(sampleRate * durationSeconds);
        return new float[numSamples];
    }
}
