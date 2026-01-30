# Sprint 00: Foundation & Project Setup

**Duration:** 2 weeks  
**Goal:** Initialize .NET solution with Aspire, establish project structure, configure tooling

---

## Sprint Objectives

- ✅ Create .NET 9 solution with all projects
- ✅ Configure Aspire for distributed orchestration
- ✅ Set up OpenTelemetry and observability
- ✅ Establish development workflow
- ✅ Verify hot reload and debugging work

---

## User Stories

### US-00-01: As a developer, I need a .NET solution structure
**Acceptance Criteria:**
- [ ] Solution file created with all projects
- [ ] Projects compile successfully
- [ ] Project references configured correctly
- [ ] All NuGet packages restored

### US-00-02: As a developer, I need Aspire orchestration
**Acceptance Criteria:**
- [ ] Aspire AppHost project created
- [ ] ServiceDefaults project configured
- [ ] All service projects referenced in AppHost
- [ ] `dotnet run` launches Aspire Dashboard
- [ ] Dashboard accessible at http://localhost:15888

### US-00-03: As a developer, I need observability configured
**Acceptance Criteria:**
- [ ] OpenTelemetry configured in ServiceDefaults
- [ ] ActivitySource set up for each project
- [ ] Test trace appears in Aspire Dashboard
- [ ] Logs visible in dashboard
- [ ] Resource monitoring working

---

## Tasks

### Task 1: Initialize Solution
```bash
dotnet new sln -n SqncR
```

**Checklist:**
- [ ] Create solution file
- [ ] Initialize git repository (already done)
- [ ] Configure .gitignore (already done)
- [ ] Create src/ directory structure

**Estimated Time:** 30 minutes

---

### Task 2: Create Aspire Projects
```bash
cd src
dotnet new aspire-apphost -n SqncR.AppHost
dotnet new aspire-servicedefaults -n SqncR.ServiceDefaults
cd ..
dotnet sln add src/SqncR.AppHost
dotnet sln add src/SqncR.ServiceDefaults
```

**Checklist:**
- [ ] Create SqncR.AppHost project
- [ ] Create SqncR.ServiceDefaults project
- [ ] Add to solution
- [ ] Verify projects restore successfully

**Estimated Time:** 1 hour

---

### Task 3: Create Core Library Projects
```bash
cd src
dotnet new classlib -n SqncR.Core -f net9.0
dotnet new classlib -n SqncR.Midi -f net9.0
dotnet new classlib -n SqncR.Theory -f net9.0
dotnet new classlib -n SqncR.State -f net9.0
cd ..
dotnet sln add src/SqncR.Core
dotnet sln add src/SqncR.Midi
dotnet sln add src/SqncR.Theory
dotnet sln add src/SqncR.State
```

**Checklist:**
- [ ] Create SqncR.Core
- [ ] Create SqncR.Midi
- [ ] Create SqncR.Theory
- [ ] Create SqncR.State
- [ ] Add all to solution
- [ ] Enable nullable reference types in all projects

**Estimated Time:** 1 hour

---

### Task 4: Configure Project References

**SqncR.Core** depends on:
- SqncR.Midi
- SqncR.Theory
- SqncR.State

**SqncR.Midi** depends on:
- SqncR.Theory (for musical concepts)

```bash
cd src/SqncR.Core
dotnet add reference ../SqncR.Midi
dotnet add reference ../SqncR.Theory
dotnet add reference ../SqncR.State

cd ../SqncR.Midi
dotnet add reference ../SqncR.Theory
```

**Checklist:**
- [ ] Add SqncR.Core references
- [ ] Add SqncR.Midi references
- [ ] Verify no circular dependencies
- [ ] Test build: `dotnet build`

**Estimated Time:** 30 minutes

---

### Task 5: Add NuGet Packages

**SqncR.Core:**
```bash
cd src/SqncR.Core
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package Microsoft.Extensions.Logging
dotnet add package System.Text.Json
```

**SqncR.Midi:**
```bash
cd src/SqncR.Midi
dotnet add package Melanchall.DryWetMidi
```

**SqncR.State:**
```bash
cd src/SqncR.State
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
```

**All Projects:**
```bash
# Add to each project
dotnet add package OpenTelemetry
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
dotnet add package OpenTelemetry.Extensions.Hosting
```

**Checklist:**
- [ ] Add packages to SqncR.Core
- [ ] Add DryWetMidi to SqncR.Midi
- [ ] Add EF Core to SqncR.State
- [ ] Add OpenTelemetry to all projects
- [ ] Restore all packages: `dotnet restore`

**Estimated Time:** 1 hour

---

### Task 6: Create Test Projects
```bash
mkdir tests
cd tests
dotnet new xunit -n SqncR.Core.Tests -f net9.0
dotnet new xunit -n SqncR.Midi.Tests -f net9.0
dotnet new xunit -n SqncR.Theory.Tests -f net9.0
cd ..
dotnet sln add tests/SqncR.Core.Tests
dotnet sln add tests/SqncR.Midi.Tests
dotnet sln add tests/SqncR.Theory.Tests
```

**Add Test Dependencies:**
```bash
cd tests/SqncR.Core.Tests
dotnet add package FluentAssertions
dotnet add package Moq
dotnet add reference ../../src/SqncR.Core
```

**Checklist:**
- [ ] Create test projects
- [ ] Add xUnit, FluentAssertions, Moq
- [ ] Add project references to code under test
- [ ] Create first "hello world" test
- [ ] Run tests: `dotnet test`

**Estimated Time:** 1 hour

---

### Task 7: Configure global.json and Directory.Build.props

**global.json:**
```json
{
  "sdk": {
    "version": "9.0.0",
    "rollForward": "latestMinor"
  }
}
```

**Directory.Build.props:**
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
</Project>
```

**Checklist:**
- [ ] Create global.json at solution root
- [ ] Create Directory.Build.props at solution root
- [ ] Verify .NET 9 SDK installed
- [ ] Clean and rebuild solution

**Estimated Time:** 30 minutes

---

### Task 8: Configure Aspire AppHost

**src/SqncR.AppHost/Program.cs:**
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// SQLite for state
var sqlite = builder.AddSqlite("sqlite")
    .WithDataVolume();

// Core services will be added in future sprints
// For now, just verify AppHost runs

builder.Build().Run();
```

**Checklist:**
- [ ] Edit AppHost Program.cs
- [ ] Add basic configuration
- [ ] Run: `cd src/SqncR.AppHost && dotnet run`
- [ ] Verify dashboard opens at http://localhost:15888
- [ ] Verify SQLite resource appears in dashboard

**Estimated Time:** 1 hour

---

### Task 9: Configure OpenTelemetry in ServiceDefaults

**src/SqncR.ServiceDefaults/Extensions.cs:**
```csharp
public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddRuntimeInstrumentation()
                       .AddAspNetCoreInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation();
            });
        
        return builder;
    }
}
```

**Checklist:**
- [ ] Configure OpenTelemetry metrics
- [ ] Configure OpenTelemetry tracing
- [ ] Set up OTLP exporter
- [ ] Add custom ActivitySource for SqncR
- [ ] Test trace appears in dashboard

**Estimated Time:** 2 hours

---

### Task 10: Create Initial Project Files

**SqncR.Core/SqncRService.cs:**
```csharp
namespace SqncR.Core;

public class SqncRService
{
    private readonly ILogger<SqncRService> _logger;
    
    public SqncRService(ILogger<SqncRService> logger)
    {
        _logger = logger;
    }
    
    // Placeholder methods - implement in future sprints
    public Task<string> GetVersionAsync()
    {
        return Task.FromResult("0.1.0-alpha");
    }
}
```

**SqncR.Theory/Models/Note.cs:**
```csharp
namespace SqncR.Theory.Models;

/// <summary>
/// Represents a musical note as a MIDI number (0-127).
/// </summary>
public readonly record struct Note(int MidiNumber)
{
    // Implementation in Sprint 01
}
```

**Checklist:**
- [ ] Create placeholder SqncRService
- [ ] Create placeholder Note struct
- [ ] Create placeholder IMidiService
- [ ] Projects compile with no errors

**Estimated Time:** 1 hour

---

## Definition of Done

- ✅ All projects created and added to solution
- ✅ All NuGet packages installed and restored
- ✅ Solution builds successfully (`dotnet build`)
- ✅ All tests pass (`dotnet test`) - even if minimal
- ✅ Aspire AppHost runs and dashboard accessible
- ✅ OpenTelemetry configured and test trace visible
- ✅ Hot reload verified (change code, see update without restart)
- ✅ Git commits follow conventions
- ✅ Documentation updated (DOCS_INDEX.md)

---

## Deliverables

1. **Working .NET solution** - All projects compile
2. **Aspire Dashboard** - Accessible and showing telemetry
3. **Test suite** - Running (even with placeholder tests)
4. **Development workflow** - Documented and verified
5. **Git history** - Clean commits following conventions

---

## Dependencies

**Required Tools:**
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Rider](https://www.jetbrains.com/rider/) or [VS Code](https://code.visualstudio.com/)
- Git

**Optional but Recommended:**
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for SQLite volume)

---

## Risks & Mitigation

**Risk:** NuGet package version conflicts  
**Mitigation:** Use specific versions, test restore frequently

**Risk:** .NET 9 not installed  
**Mitigation:** Document requirement clearly, provide installation link

**Risk:** Aspire dashboard doesn't start  
**Mitigation:** Check firewall, verify ports available (15888)

---

## Sprint Review Agenda

**Demo:**
1. Show solution structure in IDE
2. Run `dotnet build` - all green
3. Run `dotnet test` - all pass
4. Run `dotnet run` in AppHost - dashboard opens
5. Show OpenTelemetry trace in dashboard

**Retrospective Questions:**
- What went well?
- What was challenging?
- What should we change for Sprint 01?

---

## Next Sprint Preview

**Sprint 01: Music Theory & MIDI Foundation**
- Implement Note, Scale, Chord value types
- Build MidiService with DryWetMidi
- Device scanning and enumeration
- First device profile (Polyend Synth)
- Theory tests with correctness validation

---

**Sprint Status:** 🔲 Not Started  
**Updated:** January 29, 2026
