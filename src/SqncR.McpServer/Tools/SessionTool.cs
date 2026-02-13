using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using ModelContextProtocol.Server;
using SqncR.Core.Generation;
using SqncR.Core.Persistence;

namespace SqncR.McpServer.Tools;

[McpServerToolType]
public static class SessionTool
{
    internal static readonly ActivitySource ActivitySource = new("SqncR.McpServer");

    [McpServerTool(Name = "save_session"), Description("Saves the current generation state as a named session for later recall.")]
    public static async Task<string> SaveSession(
        GenerationEngine engine,
        GenerationState state,
        SessionStore store,
        [Description("Name for this session")] string name)
    {
        using var activity = ActivitySource.StartActivity("mcp.save_session");
        activity?.SetTag("mcp.tool", "save_session");
        activity?.SetTag("session.name", name);

        try
        {
            var session = SessionState.FromGenerationState(state, name);
            await store.SaveAsync(session).ConfigureAwait(false);
            return $"Session '{name}' saved.";
        }
        catch (Exception ex)
        {
            return $"Error saving session: {ex.Message}";
        }
    }

    [McpServerTool(Name = "load_session"), Description("Loads a previously saved session, restoring generation parameters.")]
    public static async Task<string> LoadSession(
        GenerationEngine engine,
        GenerationState state,
        SessionStore store,
        [Description("Name of session to load")] string name)
    {
        using var activity = ActivitySource.StartActivity("mcp.load_session");
        activity?.SetTag("mcp.tool", "load_session");
        activity?.SetTag("session.name", name);

        try
        {
            var session = await store.LoadAsync(name).ConfigureAwait(false);
            session.ApplyTo(engine);
            return $"Session '{name}' loaded (saved {session.SavedAt:u}).";
        }
        catch (FileNotFoundException)
        {
            return $"Session '{name}' not found.";
        }
        catch (Exception ex)
        {
            return $"Error loading session: {ex.Message}";
        }
    }

    [McpServerTool(Name = "list_sessions"), Description("Lists all saved generation sessions.")]
    public static async Task<string> ListSessions(SessionStore store)
    {
        using var activity = ActivitySource.StartActivity("mcp.list_sessions");
        activity?.SetTag("mcp.tool", "list_sessions");

        try
        {
            var names = await store.ListAsync().ConfigureAwait(false);
            if (names.Count == 0)
                return "No saved sessions.";

            var sb = new StringBuilder();
            sb.AppendLine($"Saved sessions ({names.Count}):");
            foreach (var name in names)
            {
                sb.AppendLine($"  • {name}");
            }

            return sb.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            return $"Error listing sessions: {ex.Message}";
        }
    }
}
