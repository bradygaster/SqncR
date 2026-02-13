using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using ModelContextProtocol.Server;
using SqncR.Core.Generation;
using SqncR.Core.Persistence;

namespace SqncR.McpServer.Tools;

[McpServerToolType]
public static class SceneTool
{
    internal static readonly ActivitySource ActivitySource = new("SqncR.McpServer");

    [McpServerTool(Name = "save_scene"), Description("Saves the current generation state as a named scene preset for instant recall.")]
    public static async Task<string> SaveScene(
        GenerationState state,
        SceneStore store,
        [Description("Name for this scene preset")] string name,
        [Description("Optional description of this scene")] string? description = null)
    {
        using var activity = ActivitySource.StartActivity("mcp.save_scene");
        activity?.SetTag("mcp.tool", "save_scene");
        activity?.SetTag("scene.name", name);

        try
        {
            var scene = Scene.FromGenerationState(state, name, description);
            await store.SaveAsync(scene).ConfigureAwait(false);
            return $"Scene '{name}' saved.";
        }
        catch (Exception ex)
        {
            return $"Error saving scene: {ex.Message}";
        }
    }

    [McpServerTool(Name = "load_scene"), Description("Loads a scene preset, applying its settings to the generation engine.")]
    public static async Task<string> LoadScene(
        GenerationEngine engine,
        SceneStore store,
        [Description("Name of scene to load")] string name)
    {
        using var activity = ActivitySource.StartActivity("mcp.load_scene");
        activity?.SetTag("mcp.tool", "load_scene");
        activity?.SetTag("scene.name", name);

        try
        {
            var scene = await store.LoadAsync(name).ConfigureAwait(false);
            scene.ApplyTo(engine);
            return $"Scene '{name}' loaded — {scene.Tempo} BPM, {scene.ScaleName}, {scene.RootNote}.";
        }
        catch (FileNotFoundException)
        {
            return $"Scene '{name}' not found.";
        }
        catch (Exception ex)
        {
            return $"Error loading scene: {ex.Message}";
        }
    }

    [McpServerTool(Name = "list_scenes"), Description("Lists all available scene presets (user-saved and built-in).")]
    public static async Task<string> ListScenes(SceneStore store)
    {
        using var activity = ActivitySource.StartActivity("mcp.list_scenes");
        activity?.SetTag("mcp.tool", "list_scenes");

        try
        {
            var scenes = await store.ListAsync().ConfigureAwait(false);
            if (scenes.Count == 0)
                return "No scenes available.";

            var sb = new StringBuilder();
            sb.AppendLine($"Scenes ({scenes.Count}):");
            foreach (var scene in scenes)
            {
                var tag = scene.IsBuiltIn ? " [built-in]" : "";
                var desc = !string.IsNullOrWhiteSpace(scene.Description) ? $" — {scene.Description}" : "";
                sb.AppendLine($"  • {scene.Name}{tag}{desc}");
            }

            return sb.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            return $"Error listing scenes: {ex.Message}";
        }
    }

    [McpServerTool(Name = "delete_scene"), Description("Deletes a user-saved scene preset.")]
    public static async Task<string> DeleteScene(
        SceneStore store,
        [Description("Name of scene to delete")] string name)
    {
        using var activity = ActivitySource.StartActivity("mcp.delete_scene");
        activity?.SetTag("mcp.tool", "delete_scene");
        activity?.SetTag("scene.name", name);

        try
        {
            await store.DeleteAsync(name).ConfigureAwait(false);
            return $"Scene '{name}' deleted.";
        }
        catch (Exception ex)
        {
            return $"Error deleting scene: {ex.Message}";
        }
    }
}
