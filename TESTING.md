# Testing Strategy

**Rule #1: Nothing enters the repo without a test.**

No exceptions. No "I'll add tests later." No "it's just a small change."

---

## Test-First Development

Every feature follows this flow:

```
1. Human writes intent (natural language)
2. Intent becomes executable test (fails)
3. AI implements code to pass test
4. Test passes → code is merged
5. Repeat
```

If a test doesn't exist, the feature doesn't exist.

---

## Test Types

### 1. Unit Tests (xUnit)
For pure functions and isolated logic.

```csharp
// ✅ Good: Tests one thing, clear intent
[Fact]
public void Parse_C4_Returns60()
{
    Assert.Equal(60, NoteParser.Parse("C4"));
}
```

Location: `tests/SqncR.Core.Tests/`

### 2. Behavior Tests (Gherkin → SpecFlow)
For features that span multiple components.

```gherkin
Feature: Pattern Generation
  As a musician
  I want to describe music in natural language
  So that I can quickly create patterns without manual MIDI programming

  Scenario: Generate a bass pattern in C minor
    Given I request "a simple bass line in C minor"
    When the pattern is generated
    Then all notes should be in the C minor scale
    And the pattern should have at least 4 notes
    And all velocities should be between 1 and 127

  Scenario: Generated pattern plays without errors
    Given I request "chill pads in F major"
    When the pattern is generated
    And I play it on a mock MIDI device
    Then no exceptions should be thrown
    And note-on events should precede corresponding note-offs
```

Location: `tests/SqncR.Specs/Features/`

### 3. Integration Tests
For end-to-end CLI verification.

```csharp
[Fact]
public async Task CLI_ListDevices_ReturnsZeroExitCode()
{
    var result = await RunCli("list-devices");
    Assert.Equal(0, result.ExitCode);
}

[Fact]
public async Task CLI_PlayNonexistentFile_ReturnsErrorExitCode()
{
    var result = await RunCli("play", "nonexistent.yaml", "-d", "0");
    Assert.Equal(1, result.ExitCode);
    Assert.Contains("not found", result.StdErr);
}
```

Location: `tests/SqncR.Cli.Tests/`

### 4. Prompt Regression Tests
For AI generation consistency.

```yaml
# tests/prompts/bass-cminor.yaml
prompt: "simple bass line in C minor, 4 bars, quarter notes"
constraints:
  scale: [C, D, Eb, F, G, Ab, Bb]
  min_notes: 8
  max_notes: 32
  min_velocity: 40
  max_velocity: 100
  valid_durations: [480, 960, 1920]  # quarter, half, whole at 480 TPQ
```

These YAML specs are loaded by a test runner that:
1. Sends prompt to generator
2. Validates output against constraints
3. Fails if any constraint violated

Location: `tests/prompts/`

---

## The Testing Contract

### Before Writing Code

1. **Write the test first.** Describe what "done" looks like.
2. **Run the test.** It must fail (red).
3. **Commit the failing test.** This documents intent.

### While Writing Code

4. **Write minimal code** to make the test pass.
5. **Run the test.** It must pass (green).
6. **Refactor** if needed, keeping tests green.

### Before Merging

7. **All tests pass.** No exceptions.
8. **No skipped tests.** Fix or delete, never skip.
9. **Coverage check.** New code must have tests.

---

## Automating with AI

The entire test suite is designed to be runnable by an AI agent:

```bash
# AI runs this to check if work is complete
dotnet test --logger:"console;verbosity=normal"

# Exit code 0 = all tests pass = feature complete
# Exit code 1 = tests fail = keep working
```

### Prompt-Driven Test Generation

AI can generate tests from natural language specs:

```
Human: "Pattern generation should never produce notes outside the requested scale"

AI generates:
- Unit test: ScaleTests.cs - verify scale note calculation
- Unit test: PatternValidatorTests.cs - reject out-of-scale notes
- Behavior test: PatternGeneration.feature - end-to-end scenario
- Prompt test: tests/prompts/scale-constraint.yaml
```

### Test as Definition of Done

When human says "implement bass pattern generation", AI:

1. Reads existing tests for context
2. Writes new tests that define "bass pattern generation"
3. Commits tests (they fail)
4. Implements until tests pass
5. Reports: "All tests pass. Feature complete."

---

## Directory Structure

```
tests/
├── SqncR.Core.Tests/          # Unit tests for Core library
│   ├── NoteParserTests.cs
│   ├── SequenceParserTests.cs
│   ├── PatternValidatorTests.cs
│   └── MusicTheory/
│       ├── ScaleTests.cs
│       └── ChordTests.cs
│
├── SqncR.Cli.Tests/           # Integration tests for CLI
│   └── CliIntegrationTests.cs
│
├── SqncR.Specs/               # Gherkin behavior specs
│   ├── Features/
│   │   ├── PatternGeneration.feature
│   │   ├── Playback.feature
│   │   └── SessionManagement.feature
│   └── Steps/
│       └── PatternGenerationSteps.cs
│
└── prompts/                   # Prompt regression tests
    ├── bass-cminor.yaml
    ├── pads-fmajor.yaml
    └── drums-basic.yaml
```

---

## Running Tests

```bash
# All tests
dotnet test

# Unit tests only
dotnet test tests/SqncR.Core.Tests

# Behavior tests only
dotnet test tests/SqncR.Specs

# With coverage
dotnet test --collect:"XPlat Code Coverage"

# Specific test
dotnet test --filter "FullyQualifiedName~NoteParser"
```

---

## CI/CD Gate

Pull requests are blocked if:
- Any test fails
- New code has no corresponding test
- Test coverage drops below threshold

```yaml
# .github/workflows/test.yml
- name: Test
  run: dotnet test --no-build --verbosity normal

- name: Fail if tests fail
  if: failure()
  run: exit 1
```

---

## The Golden Rule

> "If you can't write a test for it, you don't understand it well enough to build it."

Tests are not overhead. Tests are the specification. Tests are how we communicate intent between human and AI.

**Write the test. Then write the code.**
