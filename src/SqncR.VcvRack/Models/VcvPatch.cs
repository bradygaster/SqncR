using System.Formats.Tar;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using ZstdSharp;

namespace SqncR.VcvRack.Models;

/// <summary>
/// Represents a complete VCV Rack patch containing modules and cables.
/// Can serialize to JSON and save as a .vcv file (tar + zstd compressed).
/// </summary>
public record VcvPatch
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string Version { get; init; } = "2.0";
    public List<VcvModule> Modules { get; init; } = [];
    public List<VcvCable> Cables { get; init; } = [];

    /// <summary>
    /// Serializes the patch to VCV Rack's expected JSON format.
    /// </summary>
    public string ToJson()
    {
        var root = new JsonObject
        {
            ["version"] = Version,
            ["modules"] = new JsonArray(Modules.Select(ToModuleNode).ToArray()),
            ["cables"] = new JsonArray(Cables.Select(ToCableNode).ToArray())
        };

        return root.ToJsonString(JsonOptions);
    }

    /// <summary>
    /// Saves the patch to a .vcv file. Uses tar + zstd compression.
    /// Falls back to raw JSON if compression fails.
    /// </summary>
    public void SaveAs(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var json = ToJson();
        var jsonBytes = Encoding.UTF8.GetBytes(json);

        try
        {
            using var outputStream = File.Create(path);
            using var compressor = new CompressionStream(outputStream, 3);
            using var tarWriter = new TarWriter(compressor, TarEntryFormat.Pax, leaveOpen: false);

            var entry = new PaxTarEntry(TarEntryType.RegularFile, "patch.json")
            {
                DataStream = new MemoryStream(jsonBytes)
            };

            tarWriter.WriteEntry(entry);
        }
        catch (Exception)
        {
            // Fall back to raw JSON
            File.WriteAllText(path, json, Encoding.UTF8);
        }
    }

    private static JsonNode ToModuleNode(VcvModule m) => new JsonObject
    {
        ["id"] = m.Id,
        ["plugin"] = m.Plugin,
        ["model"] = m.Model,
        ["params"] = new JsonArray(m.Params.Select(p => (JsonNode)new JsonObject
        {
            ["id"] = p.Key,
            ["value"] = p.Value
        }).ToArray()),
        ["pos"] = new JsonArray(m.PositionX, m.PositionY),
        ["data"] = new JsonObject()
    };

    private static JsonNode ToCableNode(VcvCable c) => new JsonObject
    {
        ["id"] = c.Id,
        ["outputModuleId"] = c.OutputModuleId,
        ["outputId"] = c.OutputPortId,
        ["inputModuleId"] = c.InputModuleId,
        ["inputId"] = c.InputPortId,
        ["color"] = c.Color
    };
}
