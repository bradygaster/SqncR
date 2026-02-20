---
name: "dotnet-otel-instrumentation"
description: "Pattern for adding OpenTelemetry tracing to .NET projects with clean library/host separation"
domain: "observability"
confidence: "low"
source: "earned"
---

## Context
When instrumenting .NET applications with OpenTelemetry, the standard pattern separates instrumentation (library code) from collection/export (host code). This keeps libraries lightweight and lets the host decide where telemetry goes.

## Patterns

### Library Projects — System.Diagnostics Only
Library projects use `System.Diagnostics.ActivitySource` and `Activity` for instrumentation. No OpenTelemetry NuGet packages in libraries. `ActivitySource` is declared as `internal static readonly` on the class that owns the operations:

```csharp
public class MyService
{
    internal static readonly ActivitySource ActivitySource = new("MyApp.MySubsystem");

    public void DoWork()
    {
        using var activity = ActivitySource.StartActivity("mysubsystem.do_work");
        activity?.SetTag("work.param", value);
        // actual work
    }
}
```

### Host Project — OpenTelemetry SDK
Only the composition root (CLI, web host, AppHost) references OpenTelemetry packages and registers sources by name:

```csharp
var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyApp"))
    .AddSource("MyApp.MySubsystem")
    .AddOtlpExporter()
    .Build();
```

### Tag Naming
Use `{subsystem}.{attribute}` naming for span tags: `midi.channel`, `note.name`, `sequence.title`.

### Null-Safe Pattern
`ActivitySource.StartActivity()` returns null when no listener is registered. Always use `activity?.SetTag()` — never assume the activity exists.

### Disposal
Dispose `TracerProvider` before exit to flush pending spans. In CLI apps, do this explicitly. In hosted apps, use `OpenTelemetry.Extensions.Hosting`.

## Anti-Patterns
- **OTel SDK in libraries** — Libraries should never reference `OpenTelemetry.*` packages. Use `System.Diagnostics` only.
- **Static ActivitySource name mismatch** — The source name string in the library must exactly match what's registered in `AddSource()` in the host.
- **Forgetting disposal** — If `TracerProvider` isn't disposed, the last batch of spans may be lost.
- **Non-null activity assumptions** — Never call `.SetTag()` without null-conditional. Activity is null when no collector is listening.
