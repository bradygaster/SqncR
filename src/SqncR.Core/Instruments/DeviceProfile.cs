namespace SqncR.Core.Instruments;

/// <summary>
/// Persistent device profile describing a MIDI instrument's identity, capabilities, and mappings.
/// Saved as JSON to ~/.sqncr/devices/.
/// </summary>
public record DeviceProfile
{
    /// <summary>Unique identifier (used as filename).</summary>
    public required string Id { get; init; }

    /// <summary>Human-readable display name.</summary>
    public required string Name { get; init; }

    /// <summary>Optional description of the device.</summary>
    public string? Description { get; init; }

    /// <summary>Whether this is a hardware MIDI device, Sonic Pi, or VCV Rack instrument.</summary>
    public InstrumentType Type { get; init; }

    /// <summary>Default musical role for this device.</summary>
    public InstrumentRole DefaultRole { get; init; }

    /// <summary>MIDI channel (1-16).</summary>
    public int MidiChannel { get; init; } = 1;

    /// <summary>Hardware and performance capabilities.</summary>
    public InstrumentCapabilities Capabilities { get; init; } = new();

    /// <summary>Named CC parameter mappings (e.g. "Filter Cutoff" -> 74).</summary>
    public Dictionary<string, int>? CcMappings { get; init; }

    /// <summary>Velocity response curve: linear, exponential, or logarithmic.</summary>
    public string? VelocityCurve { get; init; }
}

/// <summary>
/// Lightweight summary of a device profile for list display.
/// </summary>
public sealed record DeviceProfileSummary(string Id, string Name, InstrumentType Type, bool IsBuiltIn);
