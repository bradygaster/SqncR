# Decision: Package Update Strategy

**Date:** 2024-12-10  
**Decided by:** Jake (Core Dev)  
**Context:** Regular maintenance update of .NET SDK and NuGet packages

## Decision

Established a systematic approach for keeping the SqncR project dependencies up to date:

1. **Use automated tooling**: Leverage `dotnet-outdated-tool` for discovering and applying package updates
   - Command: `dotnet-outdated --upgrade --version-lock Major`
   - This respects semantic versioning and only updates within the same major version

2. **Security-first updates**: Prioritize packages with known vulnerabilities
   - OpenTelemetry packages were updated from 1.11.x to 1.15.3 to fix GHSA-4625-4j76-fww9
   - This vulnerability affected the disk retry mechanism in OTLP exporter

3. **SDK alignment**: Keep global.json SDK version aligned with latest stable patch releases
   - Updated to 9.0.101 (latest .NET 9 SDK available)
   - Maintain `"rollForward": "latestMajor"` for forward compatibility

4. **Preview package strategy**: 
   - Updated ModelContextProtocol to latest preview (0.9.0-preview.2)
   - Continue tracking preview releases until stable version is available

## Rationale

- **Security**: The OpenTelemetry vulnerability was rated moderate severity and affects multi-user systems
- **Stability**: Staying within major version boundaries minimizes breaking changes
- **Automation**: Using `dotnet-outdated-tool` reduces manual effort and human error
- **Forward compatibility**: The rollForward policy allows development on newer SDK versions while targeting .NET 9

## Runtime Dependency Note

Tests require .NET 9.0.0 runtime to be installed. While the project builds with .NET 10 SDK (thanks to rollForward), the test host requires the exact target runtime version. Future consideration: Update CI/CD pipelines to ensure correct runtime versions are installed.

## Related Commit

- `0eb6864` - chore: update .NET SDK and NuGet packages to latest stable versions
