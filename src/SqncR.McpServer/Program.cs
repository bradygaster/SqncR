using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SqncR.Midi;

var builder = Host.CreateApplicationBuilder(args);

// MCP stdio transport requires all logs go to stderr, not stdout
builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// OpenTelemetry — register SqncR ActivitySources so spans flow to Aspire dashboard
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("SqncR.McpServer"))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("SqncR.McpServer")
            .AddSource("SqncR.Midi")
            .AddSource("SqncR.Playback");
    });

var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
if (!string.IsNullOrWhiteSpace(otlpEndpoint))
{
    builder.Services.AddOpenTelemetry().UseOtlpExporter();
}

// Register MidiService for DI
builder.Services.AddSingleton<MidiService>();

// Register the MCP server with stdio transport and auto-discover tools in this assembly
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
