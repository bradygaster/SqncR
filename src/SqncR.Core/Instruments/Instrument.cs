namespace SqncR.Core.Instruments;

/// <summary>
/// Unified instrument model supporting hardware MIDI, Sonic Pi, and VCV Rack instruments.
/// </summary>
public record Instrument
{
    /// <summary>Unique identifier for this instrument.</summary>
    public required string Id { get; init; }

    /// <summary>Human-readable display name.</summary>
    public required string Name { get; init; }

    /// <summary>Whether this is a hardware MIDI device, Sonic Pi, or VCV Rack instrument.</summary>
    public InstrumentType Type { get; init; }

    /// <summary>Musical role (Bass, Pad, Lead, Drums, Melody).</summary>
    public InstrumentRole Role { get; init; }

    /// <summary>MIDI channel for this instrument (1-16).</summary>
    public int MidiChannel { get; init; } = 1;

    /// <summary>Hardware and performance capabilities.</summary>
    public InstrumentCapabilities Capabilities { get; init; } = new();
}

/// <summary>Classifies the instrument backend.</summary>
public enum InstrumentType
{
    Hardware,
    SonicPi,
    VcvRack
}

/// <summary>Musical role an instrument fills in the arrangement.</summary>
public enum InstrumentRole
{
    Bass,
    Pad,
    Lead,
    Drums,
    Melody
}

/// <summary>Describes the performance envelope of an instrument.</summary>
public record InstrumentCapabilities
{
    /// <summary>Lowest playable MIDI note.</summary>
    public int MinNote { get; init; } = 0;

    /// <summary>Highest playable MIDI note.</summary>
    public int MaxNote { get; init; } = 127;

    /// <summary>Maximum simultaneous notes the instrument can play.</summary>
    public int MaxPolyphony { get; init; } = 8;

    /// <summary>Estimated round-trip latency in milliseconds.</summary>
    public int EstimatedLatencyMs { get; init; } = 0;

    /// <summary>Description of the instrument's sound character.</summary>
    public string? Timbre { get; init; }
}
