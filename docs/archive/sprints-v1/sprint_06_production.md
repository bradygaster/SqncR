# Sprint 06: Device-Specific Skills & Polish

**Duration:** 2 weeks  
**Goal:** Implement device-specific skills, complete all device profiles, production polish

---

## Sprint Objectives

- ✅ All 6 device profiles complete and tested
- ✅ 10+ device-specific skills implemented
- ✅ Production-ready packaging
- ✅ Documentation complete
- ✅ Performance optimization
- ✅ CI/CD pipeline

---

## User Stories

### US-06-01: Device-specific control
**As a user, I want deep control of my specific devices**

**Acceptance Criteria:**
- [ ] "set Polyend engine 1 to wavetable" works
- [ ] "trigger DFAM step 3" works via MAFD
- [ ] "switch MESS to preset 12" works
- [ ] All device-specific features accessible

### US-06-02: Production deployment
**As a developer, I want to deploy SqncR to production**

**Acceptance Criteria:**
- [ ] Docker images built
- [ ] NuGet packages published (local)
- [ ] Executables signed
- [ ] Installation scripts work
- [ ] Health checks configured

---

## Tasks

### Task 1: Complete All Device Profiles

**Remaining profiles:**
- [ ] MoogMother32Profile
- [ ] MoogDfamProfile  
- [ ] SonoclastMafdProfile
- [ ] PolyendMessProfile
- [ ] PolyendPlayProfile

**Each profile needs:**
- [ ] MIDI implementation details
- [ ] Capability mapping
- [ ] Role suggestions
- [ ] Matching logic
- [ ] Tests with real hardware

**Estimated Time:** 8 hours

---

### Task 2: Implement Device-Specific Skills

**Polyend Synth:**
- [ ] skill-polyend-synth-engine-select
- [ ] skill-polyend-synth-aftertouch

**Polyend MESS:**
- [ ] skill-polyend-mess-preset
- [ ] skill-mess-glitch-automation
- [ ] skill-mess-sequencer-pattern

**Moog:**
- [ ] skill-moog-mother32-sequence
- [ ] skill-moog-dfam-pattern
- [ ] skill-mother32-cv-routing

**Play+:**
- [ ] skill-polyend-play-pattern
- [ ] skill-play-plus-track-mute

**Estimated Time:** 12 hours

---

### Task 3: Performance Optimization

**Profile and optimize:**
- [ ] MIDI message latency < 5ms
- [ ] Theory calculations cached
- [ ] Device profile matching optimized
- [ ] Memory allocations minimized
- [ ] Benchmark tests
- [ ] Document performance characteristics

**Estimated Time:** 6 hours

---

### Task 4: Production Packaging

**Docker:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspire:9.0
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SqncR.McpServer.dll"]
```

**NuGet:**
```xml
<PropertyGroup>
  <PackageId>SqncR.Sdk</PackageId>
  <Version>1.0.0</Version>
  <Authors>Brady Gaster</Authors>
  <Description>AI-Native Generative Music SDK</Description>
</PropertyGroup>
```

**Checklist:**
- [ ] Docker images for MCP server and API
- [ ] NuGet package for SDK
- [ ] Single-file executables for CLI
- [ ] Installation scripts (Windows, macOS, Linux)
- [ ] Version management strategy

**Estimated Time:** 8 hours

---

### Task 5: Complete Documentation

**User guides:**
- [ ] Getting Started guide
- [ ] Device Setup guide (per device)
- [ ] API Reference
- [ ] CLI Reference
- [ ] MCP Tool Reference
- [ ] Troubleshooting guide

**Video content:**
- [ ] Quick start video (5 min)
- [ ] Advanced workflows (10 min)
- [ ] Device setup walkthrough

**Estimated Time:** 10 hours

---

### Task 6: CI/CD Pipeline

**GitHub Actions:**
```yaml
name: Build and Test

on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      - run: dotnet restore
      - run: dotnet build
      - run: dotnet test
```

**Checklist:**
- [ ] Build workflow
- [ ] Test workflow
- [ ] Publish workflow (NuGet, executables)
- [ ] Docker image build
- [ ] Release automation

**Estimated Time:** 4 hours

---

### Task 7: Final Integration Testing

**Test all workflows:**
- [ ] CLI: Complete user journey
- [ ] MCP: Complete conversation in Claude
- [ ] API: All endpoints
- [ ] SDK: Example apps
- [ ] Multi-device: All devices working together
- [ ] All device-specific features
- [ ] Performance under load

**Estimated Time:** 6 hours

---

## Definition of Done

- ✅ All 6 device profiles tested with hardware
- ✅ All device-specific skills working
- ✅ Production packaging complete
- ✅ Documentation complete
- ✅ CI/CD pipeline operational
- ✅ Performance benchmarks met
- ✅ Ready for v1.0 release

---

## Deliverables

1. **Complete Device Support** - All 6 devices fully functional
2. **Production Packages** - Docker, NuGet, executables
3. **Complete Documentation** - User guides, API docs, videos
4. **CI/CD** - Automated build/test/release
5. **v1.0 Release** - Production-ready software

---

**Sprint Status:** 🔲 Not Started  
**Updated:** January 29, 2026
