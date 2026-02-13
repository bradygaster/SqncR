namespace SqncR.VcvRack.Models;

/// <summary>
/// Represents a module in a VCV Rack patch.
/// Each module has a unique ID, a plugin/model slug pair, parameter values, and a position.
/// </summary>
public record VcvModule
{
    public int Id { get; init; }
    public string Plugin { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public Dictionary<string, float> Params { get; init; } = new();
    public float PositionX { get; init; }
    public float PositionY { get; init; }

    /// <summary>
    /// Named ports this module exposes, mapped to VCV Rack port indices.
    /// Key = friendly name (e.g. "V/Oct"), Value = port index.
    /// </summary>
    public Dictionary<string, int> OutputPorts { get; init; } = new();

    /// <summary>
    /// Named input ports this module exposes, mapped to VCV Rack port indices.
    /// </summary>
    public Dictionary<string, int> InputPorts { get; init; } = new();
}
