# Sprint 04: REST API & SDK Library

**Duration:** 2 weeks  
**Goal:** Complete all 4 transport layers (CLI, MCP, API, SDK)

---

## Sprint Objectives

- ✅ REST API with OpenAPI documentation
- ✅ .NET SDK library with fluent API
- ✅ All 4 transports use same Core
- ✅ API hosted in Aspire
- ✅ SDK published as NuGet package (local)

---

## User Stories

### US-04-01: REST API
**As a developer, I want to call SqncR via HTTP/REST**

**Acceptance Criteria:**
- [ ] GET /api/devices returns device list
- [ ] POST /api/generate starts generation
- [ ] PATCH /api/generate/modify modifies generation
- [ ] Swagger UI accessible
- [ ] CORS configured for web apps
- [ ] OpenTelemetry tracing

### US-04-02: .NET SDK
**As a .NET developer, I want a fluent SDK to use SqncR**

**Acceptance Criteria:**
- [ ] Install SqncR.Sdk NuGet package
- [ ] `sqncr.Devices.ListAsync()` works
- [ ] `sqncr.Generate("ambient")` works
- [ ] Fluent, discoverable API
- [ ] IntelliSense documentation
- [ ] Works against API or direct Core

---

## Tasks

### Task 1: Create REST API Project

```bash
cd src
dotnet new webapi -n SqncR.Api -f net9.0
dotnet sln add SqncR.Api
cd SqncR.Api
dotnet add reference ../SqncR.Core
dotnet add reference ../SqncR.ServiceDefaults
```

**Checklist:**
- [ ] Create API project
- [ ] Reference SqncR.Core
- [ ] Reference ServiceDefaults (Aspire)
- [ ] Remove default WeatherForecast code

**Estimated Time:** 1 hour

---

### Task 2: Implement API Endpoints

**File:** `src/SqncR.Api/Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add SqncR services
builder.Services.AddSqncRCore();
builder.Services.AddSqncRMidi();
builder.Services.AddSqncRTheory();
builder.Services.AddSqncRState();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod());

// Devices endpoints
app.MapGet("/api/devices", async (SqncRService sqncr) =>
{
    var devices = await sqncr.ListDevicesAsync();
    return Results.Ok(devices);
});

// Generation endpoints
app.MapPost("/api/generate", async (GenerationRequest request, SqncRService sqncr) =>
{
    var result = await sqncr.GenerateAsync(request);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPatch("/api/generate/modify", async (ModifyRequest request, SqncRService sqncr) =>
{
    await sqncr.ModifyAsync(request.Instruction);
    return Results.Ok();
});

app.MapDelete("/api/generate", async (SqncRService sqncr) =>
{
    await sqncr.StopAsync();
    return Results.Ok();
});

app.Run();
```

**Checklist:**
- [ ] GET /api/devices
- [ ] POST /api/generate
- [ ] PATCH /api/generate/modify
- [ ] DELETE /api/generate
- [ ] GET/POST/DELETE /api/session
- [ ] Swagger documentation
- [ ] Test with REST client

**Estimated Time:** 6 hours

---

### Task 3: Add API to Aspire AppHost

**File:** `src/SqncR.AppHost/Program.cs`

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var sqlite = builder.AddSqlite("sqlite").WithDataVolume();

var api = builder.AddProject<Projects.SqncR_Api>("api")
    .WithReference(sqlite)
    .WithHttpEndpoint(port: 5000, name: "http");

builder.Build().Run();
```

**Checklist:**
- [ ] Add API project to AppHost
- [ ] Configure HTTP endpoint
- [ ] Reference SQLite
- [ ] Test API shows in dashboard
- [ ] Access API at http://localhost:5000

**Estimated Time:** 1 hour

---

### Task 4: Create SDK Project

```bash
cd src
dotnet new classlib -n SqncR.Sdk -f net9.0
dotnet sln add SqncR.Sdk
cd SqncR.Sdk
dotnet add reference ../SqncR.Core
```

**Checklist:**
- [ ] Create SDK project
- [ ] Reference SqncR.Core
- [ ] Configure as NuGet package
- [ ] Add package metadata

**Estimated Time:** 1 hour

---

### Task 5: Implement Fluent SDK API

**File:** `src/SqncR.Sdk/SqncRClient.cs`

```csharp
namespace SqncR.Sdk;

/// <summary>
/// Client for interacting with SqncR.
/// </summary>
public class SqncRClient : IDisposable
{
    private readonly SqncRService _service;
    
    public SqncRClient()
    {
        // Set up DI internally
        var services = new ServiceCollection();
        services.AddSqncRCore();
        services.AddSqncRMidi();
        services.AddSqncRTheory();
        var provider = services.BuildServiceProvider();
        
        _service = provider.GetRequiredService<SqncRService>();
    }
    
    public DevicesApi Devices => new(_service);
    public GenerationApi Generation => new(_service);
    public SessionApi Session => new(_service);
    
    public void Dispose() { /* cleanup */ }
}

public class DevicesApi
{
    private readonly SqncRService _service;
    public DevicesApi(SqncRService service) => _service = service;
    
    public async Task<IReadOnlyList<MidiDevice>> ListAsync()
        => await _service.ListDevicesAsync();
}

public class GenerationApi
{
    private readonly SqncRService _service;
    public GenerationApi(SqncRService service) => _service = service;
    
    public async Task<GenerationResult> StartAsync(string description)
        => await _service.GenerateAsync(new GenerationRequest { Description = description });
        
    public async Task ModifyAsync(string instruction)
        => await _service.ModifyAsync(instruction);
        
    public async Task StopAsync()
        => await _service.StopAsync();
}
```

**Checklist:**
- [ ] Implement SqncRClient
- [ ] Fluent API for devices
- [ ] Fluent API for generation
- [ ] Fluent API for sessions
- [ ] XML documentation
- [ ] Usage examples in README

**Estimated Time:** 4 hours

---

### Task 6: SDK Usage Example

**File:** `examples/sdk-example/Program.cs`

```csharp
using SqncR.Sdk;

using var sqncr = new SqncRClient();

// List devices
var devices = await sqncr.Devices.ListAsync();
foreach (var device in devices)
{
    Console.WriteLine($"{device.Index}: {device.PortName}");
}

// Generate music
var result = await sqncr.Generation.StartAsync("ambient drone in A minor, 60 BPM");
Console.WriteLine($"Playing: {result.Key} at {result.Tempo} BPM");

// Modify
await sqncr.Generation.ModifyAsync("darker");

// Stop
await sqncr.Generation.StopAsync();
```

**Checklist:**
- [ ] Create example project
- [ ] Reference SqncR.Sdk
- [ ] Test all SDK methods
- [ ] Document usage

**Estimated Time:** 2 hours

---

### Task 7: Test All 4 Transports Do Same Thing

**Integration Test:**

```csharp
[Fact]
public async Task AllTransports_ListDevices_ShouldReturnSameResults()
{
    // 1. Via Core directly
    var coreService = CreateSqncRService();
    var coreDevices = await coreService.ListDevicesAsync();
    
    // 2. Via SDK
    var sdkClient = new SqncRClient();
    var sdkDevices = await sdkClient.Devices.ListAsync();
    
    // 3. Via API
    var apiClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
    var apiResponse = await apiClient.GetFromJsonAsync<List<MidiDevice>>("/api/devices");
    
    // All should return same devices
    coreDevices.Should().BeEquivalentTo(sdkDevices);
    coreDevices.Should().BeEquivalentTo(apiResponse);
}
```

**Checklist:**
- [ ] Test CLI returns same as Core
- [ ] Test MCP returns same as Core
- [ ] Test API returns same as Core
- [ ] Test SDK returns same as Core
- [ ] Verify traces are consistent

**Estimated Time:** 3 hours

---

### Task 8: Implement Session API Endpoints

**File:** `src/SqncR.Api/Program.cs` (additions)

```csharp
// Session endpoints
app.MapGet("/api/sessions", async (SqncRService sqncr) =>
{
    var sessions = await sqncr.ListSessionsAsync();
    return Results.Ok(sessions);
});

app.MapGet("/api/sessions/{name}", async (string name, SqncRService sqncr) =>
{
    var session = await sqncr.GetSessionAsync(name);
    return session != null ? Results.Ok(session) : Results.NotFound();
});

app.MapPost("/api/sessions", async (SessionSaveRequest request, SqncRService sqncr) =>
{
    var result = await sqncr.SaveSessionAsync(request.Name, request.IncludeHistory);
    return Results.Created($"/api/sessions/{request.Name}", result);
});

app.MapPost("/api/sessions/{name}/load", async (string name, SqncRService sqncr) =>
{
    await sqncr.LoadSessionAsync(name);
    return Results.Ok();
});

app.MapPost("/api/sessions/{name}/export", async (string name, ExportRequest request, SqncRService sqncr) =>
{
    var result = await sqncr.ExportSessionAsync(name, request.Format, request.OutputPath);
    return Results.Ok(result);
});

// Direct .sqnc.yaml upload/download
app.MapGet("/api/sessions/{name}/download", async (string name, SqncRService sqncr) =>
{
    var yaml = await sqncr.GetSessionYamlAsync(name);
    return Results.Text(yaml, "application/x-sqnc+yaml");
});

app.MapPost("/api/sessions/upload", async (HttpRequest request, SqncRService sqncr) =>
{
    using var reader = new StreamReader(request.Body);
    var yaml = await reader.ReadToEndAsync();
    var result = await sqncr.ImportSessionYamlAsync(yaml);
    return Results.Created($"/api/sessions/{result.Name}", result);
});
```

**API Endpoints:**
- `GET /api/sessions` - List all saved sessions
- `GET /api/sessions/{name}` - Get session metadata
- `POST /api/sessions` - Save current session
- `POST /api/sessions/{name}/load` - Load session into playback
- `POST /api/sessions/{name}/export` - Export to MIDI
- `GET /api/sessions/{name}/download` - Download .sqnc.yaml
- `POST /api/sessions/upload` - Upload .sqnc.yaml

**Checklist:**
- [ ] Implement session list endpoint
- [ ] Implement session get endpoint
- [ ] Implement session save endpoint
- [ ] Implement session load endpoint
- [ ] Implement session export endpoint
- [ ] Implement YAML download endpoint
- [ ] Implement YAML upload endpoint
- [ ] Add to Swagger documentation
- [ ] Test all endpoints

**Estimated Time:** 4 hours

---

### Task 9: SDK Session API

**File:** `src/SqncR.Sdk/SessionApi.cs`

```csharp
public class SessionApi
{
    private readonly SqncRService _service;
    
    public async Task<IReadOnlyList<SessionInfo>> ListAsync()
        => await _service.ListSessionsAsync();
    
    public async Task<SessionSaveResult> SaveAsync(string name, bool includeHistory = true)
        => await _service.SaveSessionAsync(name, includeHistory);
    
    public async Task LoadAsync(string name)
        => await _service.LoadSessionAsync(name);
    
    public async Task<ExportResult> ExportAsync(string name, ExportFormat format, string outputPath)
        => await _service.ExportSessionAsync(name, format, outputPath);
    
    public async Task<Sequence> ParseAsync(string yaml)
        => SequenceFormat.Parse(yaml);
    
    public string Serialize(Sequence sequence)
        => SequenceFormat.Serialize(sequence);
}
```

**SDK Usage:**
```csharp
using var sqncr = new SqncRClient();

// List sessions
var sessions = await sqncr.Sessions.ListAsync();

// Save current session
var result = await sqncr.Sessions.SaveAsync("my-ambient");
Console.WriteLine($"Saved to {result.FilePath}");

// Load session
await sqncr.Sessions.LoadAsync("my-ambient");

// Export to MIDI
await sqncr.Sessions.ExportAsync("my-ambient", ExportFormat.Midi, "output.mid");

// Parse .sqnc.yaml directly
var yaml = File.ReadAllText("my-sequence.sqnc.yaml");
var sequence = await sqncr.Sessions.ParseAsync(yaml);
Console.WriteLine($"Loaded {sequence.Patterns.Count} patterns");
```

**Checklist:**
- [ ] Implement SessionApi class
- [ ] Add to SqncRClient
- [ ] Document all methods
- [ ] Add example usage
- [ ] Test all methods

**Estimated Time:** 3 hours

---

## Definition of Done

- ✅ REST API operational with Swagger UI
- ✅ SDK library with fluent API
- ✅ All 4 transports (CLI, MCP, API, SDK) work
- ✅ All call same SqncR.Core
- ✅ All traced in Aspire Dashboard
- ✅ API documented with OpenAPI
- ✅ SDK has usage examples
- ✅ Tests verify transport independence
- ✅ Session save/load/export works on all transports
- ✅ .sqnc.yaml format supported across all transports

---

## Deliverables

1. **SqncR.Api** - REST API with Swagger
2. **SqncR.Sdk** - NuGet package (local)
3. **4 Working Transports** - CLI, MCP, API, SDK
4. **Examples** - Usage examples for each transport
5. **Documentation** - API reference, SDK guide
6. **Session Support** - Full .sqnc.yaml support on all transports

---

## Demo Script

**Show All 4 Transports:**

1. **CLI:** `sqncr list-devices`
2. **MCP:** Claude: "list my midi devices"
3. **API:** `curl http://localhost:5000/api/devices`
4. **SDK:** Run C# example app

**All return same results!**

5. **Show Aspire Dashboard** - Same traces from all 4

**Session Demo:**

1. Generate music via CLI
2. Save: `sqncr session save "demo"`
3. Download YAML: `curl http://localhost:5000/api/sessions/demo/download`
4. View file structure
5. Load in Claude: "load session 'demo'"
6. Play again (with variations)

---

**Sprint Status:** 🔲 Not Started  
**Updated:** January 29, 2026
