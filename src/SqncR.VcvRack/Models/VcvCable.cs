namespace SqncR.VcvRack.Models;

/// <summary>
/// Represents a cable connection between two module ports in a VCV Rack patch.
/// </summary>
public record VcvCable
{
    public int Id { get; init; }
    public int OutputModuleId { get; init; }
    public int OutputPortId { get; init; }
    public int InputModuleId { get; init; }
    public int InputPortId { get; init; }
    public string Color { get; init; } = "#e6cf37";
}
