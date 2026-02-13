using SqncR.Testing.Audio;
using SqncR.Testing.Assertions;

namespace SqncR.Testing.Tests;

public class AudioAssertionTests
{
    private const int SampleRate = 44100;
    private const double Duration = 1.0;

    [Fact]
    public void AssertFrequencyPresent_PassesForPresentFrequency()
    {
        var samples = ToneGenerator.GenerateSineWave(440, SampleRate, Duration);
        AudioAssertions.AssertFrequencyPresent(samples, SampleRate, 440);
    }

    [Fact]
    public void AssertFrequencyPresent_FailsForAbsentFrequency()
    {
        var samples = ToneGenerator.GenerateSineWave(440, SampleRate, Duration);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            AudioAssertions.AssertFrequencyPresent(samples, SampleRate, 1000));
    }

    [Fact]
    public void AssertFrequencyAbsent_PassesForAbsentFrequency()
    {
        var samples = ToneGenerator.GenerateSineWave(440, SampleRate, Duration);
        AudioAssertions.AssertFrequencyAbsent(samples, SampleRate, 1000);
    }

    [Fact]
    public void AssertFrequencyAbsent_FailsForPresentFrequency()
    {
        var samples = ToneGenerator.GenerateSineWave(440, SampleRate, Duration);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            AudioAssertions.AssertFrequencyAbsent(samples, SampleRate, 440));
    }

    [Fact]
    public void AssertSilence_PassesForSilence()
    {
        var samples = ToneGenerator.GenerateSilence(SampleRate, Duration);
        AudioAssertions.AssertSilence(samples);
    }

    [Fact]
    public void AssertSilence_FailsForLoudSignal()
    {
        var samples = ToneGenerator.GenerateSineWave(440, SampleRate, Duration);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            AudioAssertions.AssertSilence(samples));
    }

    [Fact]
    public void AssertDominantFrequency_PassesForCorrectDominant()
    {
        var samples = ToneGenerator.GenerateSineWave(440, SampleRate, Duration);
        AudioAssertions.AssertDominantFrequency(samples, SampleRate, 440);
    }

    [Fact]
    public void AssertDominantFrequency_FailsForWrongDominant()
    {
        var samples = ToneGenerator.GenerateSineWave(440, SampleRate, Duration);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            AudioAssertions.AssertDominantFrequency(samples, SampleRate, 1000));
    }
}
