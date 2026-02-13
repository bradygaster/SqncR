using System.ComponentModel;
using System.Diagnostics;
using ModelContextProtocol.Server;

namespace SqncR.McpServer.Tools;

[McpServerToolType]
public static class PingTool
{
    internal static readonly ActivitySource ActivitySource = new("SqncR.McpServer");

    [McpServerTool(Name = "ping"), Description("Returns server status and confirms the MCP server is alive.")]
    public static string Ping()
    {
        using var activity = ActivitySource.StartActivity("mcp.ping");
        activity?.SetTag("mcp.tool", "ping");

        return $"pong — SqncR MCP Server is running. Time: {DateTimeOffset.UtcNow:O}";
    }
}
