# P5: Production - Tests, CI/CD, Packaging

**Priority:** 5
**Depends on:** P4 (Transports)
**Goal:** Ship it. Real users can install and use SqncR.
**Duration:** ~2 weeks

---

## The Wow Moment

```bash
# Windows
winget install sqncr

# macOS
brew install sqncr

# Direct
dotnet tool install -g sqncr
```

It just works. Anywhere.

---

## What We're Building

1. **Comprehensive tests** - Unit, integration, end-to-end
2. **CI/CD pipeline** - GitHub Actions for build/test/release
3. **Packaging** - dotnet tool, standalone executables
4. **Documentation** - User guide, API reference
5. **Performance validation** - MIDI latency < 10ms

## What We're NOT Building Yet

- Web UI
- Cloud features
- Marketplace/sharing

---

## Tasks

### Task 1: Test Coverage

**Unit tests (target: 90%+):**

```bash
# Create test projects if not exists
dotnet new xunit -n SqncR.Core.Tests
dotnet new xunit -n SqncR.Midi.Tests

# Add to solution
dotnet sln add tests/SqncR.Core.Tests
dotnet sln add tests/SqncR.Midi.Tests
```

**Test categories:**

| Project | Coverage Target | Focus |
|---------|----------------|-------|
| SqncR.Theory.Tests | 95% | Note, Scale, Chord correctness |
| SqncR.Core.Tests | 90% | Skills, parsing, generation |
| SqncR.Midi.Tests | 80% | Device mocking, message formatting |
| SqncR.Integration.Tests | N/A | End-to-end with real hardware |

**Example skill test:**

```csharp
[Fact]
public async Task ChordProgressionSkill_MinorKey_ReturnsCorrectChords()
{
    var skill = new ChordProgressionSkill();
    var result = await skill.ExecuteAsync(new SkillInput(new()
    {
        ["key"] = "A",
        ["mode"] = "minor",
        ["bars"] = 4
    }));

    result.Success.Should().BeTrue();
    var data = (dynamic)result.Data!;
    var progression = (List<ChordInfo>)data.progression;

    progression.Should().HaveCount(4);
    progression[0].Symbol.Should().Contain("Am");
}
```

---

### Task 2: Integration Tests

**With mocked MIDI:**

```csharp
public class MockOutputDevice : IOutputDevice
{
    public List<MidiEvent> SentEvents { get; } = new();

    public void SendEvent(MidiEvent evt)
    {
        SentEvents.Add(evt);
    }
}

[Fact]
public async Task GenerateAndPlay_SendsCorrectMidiMessages()
{
    var mockDevice = new MockOutputDevice();
    var midi = new MidiService(mockDevice);
    var service = new SqncRService(midi);

    await service.GenerateAndPlayAsync("C major, 4 bars", cancellationToken);

    mockDevice.SentEvents.Should().ContainItemsAssignableTo<NoteOnEvent>();

    var noteOns = mockDevice.SentEvents.OfType<NoteOnEvent>().ToList();
    noteOns.Should().AllSatisfy(n => n.NoteNumber.Should().BeInRange(0, 127));
}
```

**With real hardware (manual/CI-optional):**

```csharp
[Fact]
[Trait("Category", "Hardware")]
public async Task RealDevice_PlaysWithoutErrors()
{
    // Skip if no hardware
    var devices = OutputDevice.GetAll().ToList();
    if (devices.Count == 0)
    {
        Assert.Skip("No MIDI devices connected");
        return;
    }

    var midi = new MidiService();
    midi.OpenDevice(0);

    // Should not throw
    midi.SendNoteOn(1, 60, 80);
    await Task.Delay(100);
    midi.SendNoteOff(1, 60);
}
```

---

### Task 3: GitHub Actions CI

**File:** `.github/workflows/ci.yml`

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest, macos-latest]

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore -c Release

      - name: Test
        run: dotnet test --no-build -c Release --verbosity normal

  code-coverage:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Test with coverage
        run: dotnet test --collect:"XPlat Code Coverage"

      - name: Upload coverage
        uses: codecov/codecov-action@v4
        with:
          files: '**/coverage.cobertura.xml'
```

---

### Task 4: Release Workflow

**File:** `.github/workflows/release.yml`

```yaml
name: Release

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        include:
          - os: windows-latest
            rid: win-x64
          - os: ubuntu-latest
            rid: linux-x64
          - os: macos-latest
            rid: osx-x64
          - os: macos-latest
            rid: osx-arm64

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Publish CLI
        run: |
          dotnet publish src/SqncR.Cli -c Release -r ${{ matrix.rid }} --self-contained -o artifacts/${{ matrix.rid }}

      - name: Publish MCP Server
        run: |
          dotnet publish src/SqncR.McpServer -c Release -r ${{ matrix.rid }} --self-contained -o artifacts/${{ matrix.rid }}-mcp

      - name: Upload artifacts
        uses: actions/upload-artifact@v4
        with:
          name: sqncr-${{ matrix.rid }}
          path: artifacts/

  release:
    needs: build
    runs-on: ubuntu-latest
    steps:
      - name: Download all artifacts
        uses: actions/download-artifact@v4

      - name: Create Release
        uses: softprops/action-gh-release@v1
        with:
          files: |
            sqncr-*/sqncr*
          generate_release_notes: true
```

---

### Task 5: NuGet Package (dotnet tool)

**File:** `src/SqncR.Cli/SqncR.Cli.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>

    <!-- Tool packaging -->
    <PackAsTool>true</PackAsTool>
    <ToolCommandName>sqncr</ToolCommandName>
    <PackageId>SqncR</PackageId>
    <Version>0.1.0</Version>
    <Authors>Brady Gaster</Authors>
    <Description>AI-Native Generative Music for MIDI Devices</Description>
    <PackageTags>midi;music;generative;ai;claude;mcp</PackageTags>
    <PackageProjectUrl>https://github.com/bradygaster/SqncR</PackageProjectUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageReadmeFile>README.md</PackageReadmeFile>
  </PropertyGroup>

  <ItemGroup>
    <None Include="../../README.md" Pack="true" PackagePath="\"/>
  </ItemGroup>
</Project>
```

**Publish to NuGet:**

```bash
dotnet pack src/SqncR.Cli -c Release
dotnet nuget push src/SqncR.Cli/bin/Release/*.nupkg --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json
```

---

### Task 6: Performance Validation

**MIDI latency test:**

```csharp
[Fact]
public async Task MidiLatency_IsUnder10ms()
{
    var midi = new MidiService();
    midi.OpenDevice(0);

    var latencies = new List<double>();

    for (int i = 0; i < 100; i++)
    {
        var sw = Stopwatch.StartNew();
        midi.SendNoteOn(1, 60, 80);
        sw.Stop();

        latencies.Add(sw.Elapsed.TotalMilliseconds);

        await Task.Delay(50);
        midi.SendNoteOff(1, 60);
    }

    var avgLatency = latencies.Average();
    var maxLatency = latencies.Max();

    Console.WriteLine($"Avg latency: {avgLatency:F2}ms, Max: {maxLatency:F2}ms");

    avgLatency.Should().BeLessThan(5);
    maxLatency.Should().BeLessThan(10);
}
```

---

### Task 7: User Documentation

**docs/USER_GUIDE.md:**

```markdown
# SqncR User Guide

## Installation

### dotnet tool (Recommended)
```bash
dotnet tool install -g sqncr
```

### Windows
Download `sqncr-win-x64.zip` from Releases, extract, add to PATH.

### macOS
Download `sqncr-osx-arm64.zip` (Apple Silicon) or `sqncr-osx-x64.zip` (Intel).

### Linux
Download `sqncr-linux-x64.zip`, extract, chmod +x, add to PATH.

## Quick Start

1. Connect your MIDI devices
2. List devices: `sqncr list-devices`
3. Play a file: `sqncr play example.sqnc.yaml -d 0`
4. Generate music: `sqncr generate "ambient in C minor" -d 0`

## Using with Claude

1. Configure Claude Desktop (see MCP Setup below)
2. Ask Claude: "list my midi devices"
3. Ask Claude: "play something ambient on my Polyend"

## MCP Setup

Add to `~/.config/claude/claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "sqncr": {
      "command": "sqncr-mcp"
    }
  }
}
```

## Commands Reference

| Command | Description |
|---------|-------------|
| `sqncr list-devices` | Show MIDI devices |
| `sqncr play <file> -d <n>` | Play .sqnc.yaml file |
| `sqncr generate <desc> -d <n>` | Generate music |
| `sqncr save <name>` | Save session |
| `sqncr load <name>` | Load session |
| `sqncr sessions` | List saved sessions |
| `sqncr interactive` | Start REPL mode |
```

---

## Definition of Done

- [ ] Test coverage > 80% overall
- [ ] All tests pass on Windows, macOS, Linux
- [ ] CI/CD pipeline runs on every PR
- [ ] Release workflow creates artifacts for all platforms
- [ ] `dotnet tool install -g sqncr` works
- [ ] User guide complete
- [ ] MIDI latency validated < 10ms
- [ ] v0.1.0 published to GitHub Releases

---

## v1.0 Checklist

Before calling it v1.0:

- [ ] All P0-P5 complete
- [ ] 6+ MVP skills working
- [ ] MCP integration stable
- [ ] Works with at least 3 different MIDI devices
- [ ] User documentation complete
- [ ] No critical bugs open
- [ ] Dogfooded for 2+ weeks

---

## What's Next

**Beyond v1.0:**
- More skills (40+ planned)
- Agent system
- REST API
- SDK package
- Web UI
- Device profiles for more hardware

---

**Priority:** P5
**Status:** Waiting for P4
**Updated:** January 29, 2026
