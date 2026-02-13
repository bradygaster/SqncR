using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SqncR.Core.Generation;
using SqncR.Core.Persistence;
using SqncR.Midi;
using SqncR.Midi.Testing;
using SqncR.SonicPi;
using SqncR.VcvRack;

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
            .AddSource("SqncR.Playback")
            .AddSource("SqncR.Generation")
            .AddSource("SqncR.SonicPi")
            .AddSource("SqncR.VcvRack");
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("SqncR.Generation")
            .AddMeter("SqncR.SonicPi")
            .AddMeter("SqncR.VcvRack");
    });

var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
if (!string.IsNullOrWhiteSpace(otlpEndpoint))
{
    builder.Services.AddOpenTelemetry().UseOtlpExporter();
}

// Register MidiService for DI
builder.Services.AddSingleton<MidiService>();
builder.Services.AddSingleton<IMidiOutput>(sp => sp.GetRequiredService<MidiService>());

// Register VcvRackLauncher for DI
builder.Services.AddSingleton<VcvRackLauncher>();

// Register Sonic Pi OscClient for DI
builder.Services.AddSingleton<OscClient>();

// Register SessionStore for session persistence
builder.Services.AddSingleton<SessionStore>();

// Register GenerationEngine and its state as singletons
builder.Services.AddSingleton<GenerationState>();
builder.Services.AddSingleton<GenerationEngine>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<GenerationEngine>());

// Register the MCP server with stdio transport and auto-discover tools in this assembly
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
