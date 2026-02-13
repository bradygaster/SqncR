---
name: "aspire-apphost-setup"
description: "Pattern for adding .NET Aspire AppHost and ServiceDefaults to an existing .NET solution"
domain: "infrastructure"
confidence: "low"
source: "earned"
---

## Context
When adding .NET Aspire orchestration to an existing .NET solution, there are version-specific SDK patterns and common pitfalls around package compatibility.

## Patterns

### Aspire 9.x — Explicit Two-SDK Pattern
For Aspire SDK 9.x (targeting .NET 9), use `Microsoft.NET.Sdk` as the top-level SDK with a child `<Sdk>` element:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Sdk Name="Aspire.AppHost.Sdk" Version="9.5.2" />
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.AppHost" Version="9.5.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\MyApp\MyApp.csproj" />
  </ItemGroup>
</Project>
```

The Aspire.AppHost.Sdk 9.x does NOT auto-import Microsoft.NET.Sdk (it only does so for file-based apps where `FileBasedProgram=true`).

### Aspire 13.x+ — Simplified Top-Level SDK
From SDK 13.0.0+, the top-level `Sdk="Aspire.AppHost.Sdk/13.x.x"` works because it unconditionally imports Microsoft.NET.Sdk.

### Version Alignment
Keep all Aspire-related packages within the same minor version:
- AppHost SDK, Aspire.Hosting.AppHost, and Microsoft.Extensions.ServiceDiscovery should all be 9.5.x (for .NET 9) or 13.x (for .NET 10).
- OpenTelemetry packages are runtime-agnostic; use 1.11.x for .NET 9 era, 1.13+ for .NET 10.

### ServiceDefaults Project
The ServiceDefaults project is a shared library (`IsAspireSharedProject=true`) that configures OTel, resilience, and service discovery. It references `Microsoft.AspNetCore.App` as a FrameworkReference.

## Anti-Patterns
- **Using Aspire.AppHost.Sdk as sole top-level Sdk on 9.x** — causes NU1503 restore failures and missing build targets.
- **Mixing .NET 9 and .NET 10 Aspire packages** — version 10.x/13.x packages target net10.0 and won't restore for net9.0.
- **Applying TreatWarningsAsErrors globally with Directory.Build.props** — existing test projects may have analyzer warnings that become errors; scope it to src/ projects.
