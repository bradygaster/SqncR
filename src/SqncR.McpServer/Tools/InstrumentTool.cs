using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using ModelContextProtocol.Server;
using SqncR.Core.Generation;
using SqncR.Core.Instruments;

namespace SqncR.McpServer.Tools;

[McpServerToolType]
public static class InstrumentTool
{
    internal static readonly ActivitySource ActivitySource = new("SqncR.McpServer");

    [McpServerTool(Name = "add_instrument"), Description("Adds an instrument to the generation engine with a specified role and MIDI channel.")]
    public static string AddInstrument(
        GenerationEngine engine,
        GenerationState state,
        [Description("Unique name for the instrument")] string name,
        [Description("Instrument type: Hardware, SonicPi, VcvRack")] string type = "Hardware",
        [Description("Musical role: Bass, Pad, Lead, Drums, Melody")] string role = "Melody",
        [Description("MIDI channel (1-16)")] int channel = 1)
    {
        using var activity = ActivitySource.StartActivity("mcp.add_instrument");
        activity?.SetTag("mcp.tool", "add_instrument");

        try
        {
            if (!Enum.TryParse<InstrumentType>(type, true, out var instrumentType))
                return $"Error: Invalid instrument type '{type}'. Valid types: Hardware, SonicPi, VcvRack";

            if (!Enum.TryParse<InstrumentRole>(role, true, out var instrumentRole))
                return $"Error: Invalid instrument role '{role}'. Valid roles: Bass, Pad, Lead, Drums, Melody";

            if (channel < 1 || channel > 16)
                return "Error: MIDI channel must be between 1 and 16.";

            var id = name.ToLowerInvariant().Replace(' ', '-');
            var instrument = new Instrument
            {
                Id = id,
                Name = name,
                Type = instrumentType,
                Role = instrumentRole,
                MidiChannel = channel
            };

            engine.Commands.TryWrite(new GenerationCommand.AddInstrument(instrument));

            return $"Added instrument '{name}' (id: {id}) as {role} on channel {channel}.";
        }
        catch (ArgumentException ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    [McpServerTool(Name = "remove_instrument"), Description("Removes an instrument from the generation engine by its ID.")]
    public static string RemoveInstrument(
        GenerationEngine engine,
        [Description("Instrument ID to remove")] string id)
    {
        using var activity = ActivitySource.StartActivity("mcp.remove_instrument");
        activity?.SetTag("mcp.tool", "remove_instrument");

        engine.Commands.TryWrite(new GenerationCommand.RemoveInstrument(id));
        return $"Removed instrument '{id}'.";
    }

    [McpServerTool(Name = "list_instruments"), Description("Lists all registered instruments and their roles, channels, and capabilities.")]
    public static string ListInstruments(GenerationState state)
    {
        using var activity = ActivitySource.StartActivity("mcp.list_instruments");
        activity?.SetTag("mcp.tool", "list_instruments");

        var instruments = state.Instruments.GetAll();
        if (instruments.Count == 0)
            return "No instruments registered.";

        var sb = new StringBuilder();
        sb.AppendLine($"Registered instruments ({instruments.Count}):");
        foreach (var inst in instruments)
        {
            sb.AppendLine($"  {inst.Id}: {inst.Name} | {inst.Type} | {inst.Role} | Ch {inst.MidiChannel} | Notes {inst.Capabilities.MinNote}-{inst.Capabilities.MaxNote}");
        }
        return sb.ToString().TrimEnd();
    }
}
