using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using ModelContextProtocol.Server;
using SqncR.Core.Instruments;
using SqncR.Midi;
using SqncR.Midi.Testing;

namespace SqncR.McpServer.Tools;

[McpServerToolType]
public static class SetupTool
{
    internal static readonly ActivitySource ActivitySource = new("SqncR.McpServer");

    [McpServerTool(Name = "setup_instrument"), Description("Conversational instrument setup — discovers MIDI devices for hardware, applies software synth defaults for Sonic Pi / VCV Rack, creates a device profile, and registers the instrument.")]
    public static async Task<string> SetupInstrument(
        InstrumentRegistry registry,
        DeviceProfileStore profileStore,
        MidiService midiService,
        [Description("Human-readable name for the instrument")] string name,
        [Description("Instrument type: Hardware, SonicPi, VcvRack")] string type,
        [Description("Musical role: Bass, Pad, Lead, Drums, Melody")] string? role = null,
        [Description("MIDI channel (1-16). Auto-assigned if omitted.")] int? channel = null,
        [Description("Optional description of the instrument")] string? description = null)
    {
        using var activity = ActivitySource.StartActivity("mcp.setup_instrument");
        activity?.SetTag("mcp.tool", "setup_instrument");

        if (!Enum.TryParse<InstrumentType>(type, true, out var instrumentType))
            return $"Error: Invalid instrument type '{type}'. Valid types: Hardware, SonicPi, VcvRack";

        var instrumentRole = InstrumentRole.Melody;
        if (role is not null && !Enum.TryParse(role, true, out instrumentRole))
            return $"Error: Invalid instrument role '{role}'. Valid roles: Bass, Pad, Lead, Drums, Melody";

        if (channel.HasValue && (channel.Value < 1 || channel.Value > 16))
            return "Error: MIDI channel must be between 1 and 16.";

        var id = name.ToLowerInvariant().Replace(' ', '-');

        // Auto-assign channel if not specified
        var assignedChannel = channel ?? FindNextAvailableChannel(registry);

        // Build capabilities based on type
        var capabilities = instrumentType switch
        {
            InstrumentType.SonicPi => new InstrumentCapabilities
            {
                MinNote = 24,
                MaxNote = 108,
                MaxPolyphony = 8,
                Timbre = "digital synthesis",
            },
            InstrumentType.VcvRack => new InstrumentCapabilities
            {
                MinNote = 0,
                MaxNote = 127,
                MaxPolyphony = 16,
                Timbre = "modular synthesis",
            },
            _ => new InstrumentCapabilities()
        };

        // For hardware, list available MIDI devices to include in response
        var sb = new StringBuilder();
        if (instrumentType == InstrumentType.Hardware)
        {
            var devices = midiService.ListOutputDevices();
            if (devices.Count > 0)
            {
                sb.AppendLine("Available MIDI devices:");
                foreach (var d in devices)
                    sb.AppendLine($"  [{d.Index}] {d.Name}");
                sb.AppendLine();
            }
        }

        // Create and save the device profile
        var profile = new DeviceProfile
        {
            Id = id,
            Name = name,
            Description = description,
            Type = instrumentType,
            DefaultRole = instrumentRole,
            MidiChannel = assignedChannel,
            Capabilities = capabilities,
        };

        await profileStore.SaveAsync(profile).ConfigureAwait(false);

        // Register the instrument
        var instrument = new Instrument
        {
            Id = id,
            Name = name,
            Type = instrumentType,
            Role = instrumentRole,
            MidiChannel = assignedChannel,
            Capabilities = capabilities,
        };

        try
        {
            registry.Add(instrument);
        }
        catch (ArgumentException)
        {
            return $"Error: An instrument with id '{id}' is already registered.";
        }

        sb.AppendLine($"✅ Instrument '{name}' set up successfully!");
        sb.AppendLine($"  ID:      {id}");
        sb.AppendLine($"  Type:    {instrumentType}");
        sb.AppendLine($"  Role:    {instrumentRole}");
        sb.AppendLine($"  Channel: {assignedChannel}");
        if (description is not null)
            sb.AppendLine($"  Desc:    {description}");
        sb.AppendLine($"  Range:   {capabilities.MinNote}-{capabilities.MaxNote}");
        sb.AppendLine($"  Poly:    {capabilities.MaxPolyphony}");

        return sb.ToString().TrimEnd();
    }

    [McpServerTool(Name = "describe_instrument"), Description("Returns a detailed human-readable description of a registered instrument including name, type, role, channel, capabilities, and CC mappings.")]
    public static async Task<string> DescribeInstrument(
        InstrumentRegistry registry,
        DeviceProfileStore profileStore,
        [Description("Instrument ID to describe")] string id)
    {
        using var activity = ActivitySource.StartActivity("mcp.describe_instrument");
        activity?.SetTag("mcp.tool", "describe_instrument");

        var instrument = registry.Get(id);
        if (instrument is null)
            return $"Instrument '{id}' not found. Use list_setup_instruments to see registered instruments.";

        var sb = new StringBuilder();
        sb.AppendLine($"🎹 {instrument.Name}");
        sb.AppendLine($"  ID:      {instrument.Id}");
        sb.AppendLine($"  Type:    {instrument.Type}");
        sb.AppendLine($"  Role:    {instrument.Role}");
        sb.AppendLine($"  Channel: {instrument.MidiChannel}");
        sb.AppendLine($"  Range:   {instrument.Capabilities.MinNote}-{instrument.Capabilities.MaxNote}");
        sb.AppendLine($"  Poly:    {instrument.Capabilities.MaxPolyphony}");
        if (instrument.Capabilities.Timbre is not null)
            sb.AppendLine($"  Timbre:  {instrument.Capabilities.Timbre}");
        if (instrument.Capabilities.EstimatedLatencyMs > 0)
            sb.AppendLine($"  Latency: {instrument.Capabilities.EstimatedLatencyMs}ms");

        // Try to load profile for CC mappings
        try
        {
            var profile = await profileStore.LoadAsync(id).ConfigureAwait(false);
            if (profile.CcMappings is { Count: > 0 })
            {
                sb.AppendLine("  CC Mappings:");
                foreach (var (ccName, ccNumber) in profile.CcMappings)
                    sb.AppendLine($"    {ccName}: CC {ccNumber}");
            }
            if (profile.VelocityCurve is not null)
                sb.AppendLine($"  Velocity: {profile.VelocityCurve}");
        }
        catch (FileNotFoundException)
        {
            // No profile on disk — that's fine
        }

        return sb.ToString().TrimEnd();
    }

    [McpServerTool(Name = "list_setup_instruments"), Description("Lists all registered instruments grouped by musical role with channel assignments.")]
    public static string ListSetupInstruments(InstrumentRegistry registry)
    {
        using var activity = ActivitySource.StartActivity("mcp.list_setup_instruments");
        activity?.SetTag("mcp.tool", "list_setup_instruments");

        var all = registry.GetAll();
        if (all.Count == 0)
            return "No instruments registered. Use setup_instrument to add one.";

        var sb = new StringBuilder();
        sb.AppendLine($"🎛️ Registered instruments ({all.Count}):");

        foreach (var roleGroup in all.GroupBy(i => i.Role).OrderBy(g => g.Key))
        {
            sb.AppendLine($"\n  [{roleGroup.Key}]");
            foreach (var inst in roleGroup)
            {
                sb.AppendLine($"    {inst.Id}: {inst.Name} | {inst.Type} | Ch {inst.MidiChannel} | Notes {inst.Capabilities.MinNote}-{inst.Capabilities.MaxNote}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    [McpServerTool(Name = "remove_setup_instrument"), Description("Removes an instrument from the registry, optionally deletes its device profile, and sends AllNotesOff on its channel.")]
    public static async Task<string> RemoveSetupInstrument(
        InstrumentRegistry registry,
        DeviceProfileStore profileStore,
        IMidiOutput midiOutput,
        [Description("Instrument ID to remove")] string id,
        [Description("Whether to also delete the device profile from disk")] bool deleteProfile = false)
    {
        using var activity = ActivitySource.StartActivity("mcp.remove_setup_instrument");
        activity?.SetTag("mcp.tool", "remove_setup_instrument");

        var instrument = registry.Get(id);
        if (instrument is null)
            return $"Instrument '{id}' not found. Use list_setup_instruments to see registered instruments.";

        // Send AllNotesOff on the instrument's channel
        try
        {
            midiOutput.AllNotesOff(instrument.MidiChannel);
        }
        catch
        {
            // Best-effort — device may not be open
        }

        registry.Remove(id);

        if (deleteProfile)
            await profileStore.DeleteAsync(id).ConfigureAwait(false);

        var profileNote = deleteProfile ? " Device profile deleted." : "";
        return $"✅ Removed instrument '{instrument.Name}' (ch {instrument.MidiChannel}).{profileNote} AllNotesOff sent.";
    }

    internal static int FindNextAvailableChannel(InstrumentRegistry registry)
    {
        var usedChannels = new HashSet<int>(registry.GetAll().Select(i => i.MidiChannel));
        for (int ch = 1; ch <= 16; ch++)
        {
            if (!usedChannels.Contains(ch))
                return ch;
        }
        // All channels occupied, default to 1
        return 1;
    }
}
