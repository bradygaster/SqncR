using SqncR.VcvRack.Models;

namespace SqncR.VcvRack;

/// <summary>
/// Fluent API for constructing VCV Rack patches.
/// Auto-assigns module IDs, cable IDs, and left-to-right layout positions.
/// </summary>
public class PatchBuilder
{
    private readonly List<VcvModule> _modules = [];
    private readonly List<VcvCable> _cables = [];
    private int _nextModuleId = 1;
    private int _nextCableId = 1;

    private const float ModuleSpacing = 90f;
    private const float StartX = 30f;
    private const float StartY = 30f;

    /// <summary>
    /// Adds a module template to the patch, assigning it a unique ID and position.
    /// Returns the builder for fluent chaining, and outputs the positioned module.
    /// </summary>
    public PatchBuilder AddModule(VcvModule template, out VcvModule module)
    {
        ArgumentNullException.ThrowIfNull(template);

        module = template with
        {
            Id = _nextModuleId++,
            PositionX = StartX + (_modules.Count * ModuleSpacing),
            PositionY = StartY
        };

        _modules.Add(module);
        return this;
    }

    /// <summary>
    /// Connects an output port on one module to an input port on another.
    /// Port names are resolved against the module's port dictionaries.
    /// </summary>
    public PatchBuilder Cable(VcvModule outputModule, string outputPortName, VcvModule inputModule, string inputPortName)
    {
        ArgumentNullException.ThrowIfNull(outputModule);
        ArgumentNullException.ThrowIfNull(inputModule);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPortName);
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPortName);

        if (!outputModule.OutputPorts.TryGetValue(outputPortName, out var outputPortId))
            throw new ArgumentException($"Output port '{outputPortName}' not found on module '{outputModule.Model}'.", nameof(outputPortName));

        if (!inputModule.InputPorts.TryGetValue(inputPortName, out var inputPortId))
            throw new ArgumentException($"Input port '{inputPortName}' not found on module '{inputModule.Model}'.", nameof(inputPortName));

        var cable = new VcvCable
        {
            Id = _nextCableId++,
            OutputModuleId = outputModule.Id,
            OutputPortId = outputPortId,
            InputModuleId = inputModule.Id,
            InputPortId = inputPortId,
            Color = CableColors[(_nextCableId - 2) % CableColors.Length]
        };

        _cables.Add(cable);
        return this;
    }

    /// <summary>
    /// Builds the final VcvPatch from all added modules and cables.
    /// </summary>
    public VcvPatch Build()
    {
        return new VcvPatch
        {
            Modules = [.. _modules],
            Cables = [.. _cables]
        };
    }

    private static readonly string[] CableColors =
    [
        "#e6cf37", // yellow
        "#e64b37", // red
        "#37b5e6", // blue
        "#37e659", // green
        "#e637cf", // pink
        "#e69b37"  // orange
    ];
}
