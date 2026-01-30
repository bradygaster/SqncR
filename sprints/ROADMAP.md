# SqncR Roadmap

**Development Rule: Tests First, Always**

Each sprint starts by writing failing tests. Code is written only to make tests pass.

---

## Sprint Overview

| Sprint | Name | Entry Point (Tests) | Exit Criteria |
|--------|------|---------------------|---------------|
| P0 | Play Songs | NoteParserTests | ✅ **DONE** |
| P1 | Generate Patterns | PatternGeneratorTests, PatternValidatorTests | `dotnet test` passes, `sqncr generate` works |
| P2 | Music Theory | ScaleTests, ChordTests, ProgressionTests | Generator outputs scale-correct notes |
| P3 | Save Sessions | SequenceSerializerTests, RoundTripTests | Save → load → play identical |
| P4 | Skills Framework | ISkillTests, BassSkillTests, CompositionTests | Multiple skills compose |
| P5 | Interactive Mode | SessionStateTests, CommandTests | Modify while playing |
| P6 | MCP Server | McpProtocolTests, ToolTests | Claude Desktop integration |
| P7 | Polish | IntegrationTests, E2ETests | External contributor success |

---

## P0: Play Songs ✅ COMPLETE

### Tests Written
- [x] `NoteParserTests.cs` - 26 tests passing

### Features Delivered
- [x] `sqncr list-devices`
- [x] `sqncr play <file> --device <n>`
- [x] .sqnc.yaml parsing
- [x] Multi-track playback
- [x] Ctrl+C graceful shutdown

---

## P1: Generate Patterns

### Tests to Write FIRST

```
tests/SqncR.Core.Tests/
├── PatternValidatorTests.cs     ← Validates pattern structure
├── PromptParserTests.cs         ← Extracts intent from natural language
└── Generation/
    ├── PatternGeneratorTests.cs ← Mock LLM, verify prompt→pattern
    └── LlmClientTests.cs        ← API contract tests

tests/prompts/
├── bass-cminor.yaml             ← Prompt regression test
└── pads-fmajor.yaml             ← Prompt regression test
```

### Test Specifications

```gherkin
Feature: Pattern Generation

  Scenario: Generate pattern from natural language
    Given a prompt "simple bass line in C minor"
    When I call PatternGenerator.Generate(prompt)
    Then I receive a Pattern object
    And Pattern.Length > 0
    And Pattern.Events is not empty

  Scenario: Generated notes are valid MIDI
    Given a prompt "any musical phrase"
    When I call PatternGenerator.Generate(prompt)
    Then all note values are between 0 and 127
    And all velocity values are between 1 and 127
    And all duration values are positive

  Scenario: Dry run outputs YAML
    Given a prompt "chill bass in Am"
    When I run "sqncr generate 'chill bass in Am' --dry-run"
    Then stdout contains valid YAML
    And the YAML parses to a Pattern object

  Scenario: Generate and play
    Given a prompt "simple melody"
    And a mock MIDI device
    When I run "sqncr generate 'simple melody' --device mock"
    Then MIDI note-on events are sent
    And MIDI note-off events follow
    And exit code is 0
```

### Prompt Regression Spec

```yaml
# tests/prompts/bass-cminor.yaml
name: "Bass line in C minor"
prompt: "simple bass line in C minor, 4 bars, mostly quarter notes"

pattern_constraints:
  min_length: 1920      # At least 1 bar at 480 TPQ
  max_length: 7680      # At most 4 bars

event_constraints:
  min_count: 4
  max_count: 32
  type: "note"

note_constraints:
  # C minor scale: C, D, Eb, F, G, Ab, Bb
  allowed_notes: ["C", "D", "Eb", "F", "G", "Ab", "Bb"]
  min_octave: 1
  max_octave: 3         # Bass register
  min_velocity: 40
  max_velocity: 100
  min_duration: 240     # Eighth note minimum
  max_duration: 1920    # Whole note maximum
```

### Implementation Order

1. Write `PatternValidatorTests.cs` - define valid pattern structure
2. Write `PatternValidator.cs` - make tests pass
3. Write `PromptParserTests.cs` - extract key, scale, instrument from prompt
4. Write `PromptParser.cs` - make tests pass
5. Write `LlmClientTests.cs` - mock HTTP, verify request/response contract
6. Write `LlmClient.cs` - make tests pass
7. Write `PatternGeneratorTests.cs` - integration of above
8. Write `PatternGenerator.cs` - make tests pass
9. Write CLI integration tests
10. Add `generate` command to CLI

### Definition of Done

```bash
dotnet test                              # All pass
sqncr generate "bass in Cm" --dry-run    # Outputs valid YAML
sqncr generate "bass in Cm" -d 0         # Plays on device
```

---

## P2: Music Theory

### Tests to Write FIRST

```
tests/SqncR.Core.Tests/MusicTheory/
├── ScaleTests.cs           ← Scale note calculation
├── ChordTests.cs           ← Chord voicing
├── ProgressionTests.cs     ← Chord progression parsing
└── ConstraintTests.cs      ← Constraining notes to scale/chord
```

### Test Specifications

```gherkin
Feature: Music Theory

  Scenario Outline: Scale returns correct notes
    Given the scale "<root>" "<mode>"
    When I get the scale notes
    Then the notes are "<expected>"

    Examples:
      | root | mode       | expected                    |
      | C    | major      | C, D, E, F, G, A, B         |
      | C    | minor      | C, D, Eb, F, G, Ab, Bb      |
      | A    | minor      | A, B, C, D, E, F, G         |
      | D    | dorian     | D, E, F, G, A, B, C         |
      | E    | pentatonic | E, F#, G#, B, C#            |

  Scenario: Chord parsing
    Given the chord symbol "Cm7"
    When I parse the chord
    Then the root is "C"
    And the quality is "minor"
    And the extension is "7"
    And the notes are "C, Eb, G, Bb"

  Scenario: Constrain melody to scale
    Given a pattern with notes [C4, C#4, D4, E4, F4]
    And the scale "C major"
    When I constrain the pattern to the scale
    Then C#4 is replaced with C4 or D4
    And all other notes are unchanged
```

### Definition of Done

```bash
dotnet test --filter "MusicTheory"        # All pass
# Generator uses Scale class
# Out-of-scale notes are corrected or flagged
```

---

## P3: Save Sessions

### Tests to Write FIRST

```
tests/SqncR.Core.Tests/
├── SequenceSerializerTests.cs    ← Serialize to YAML
└── RoundTripTests.cs             ← Parse → Serialize → Parse == original
```

### Test Specifications

```gherkin
Feature: Save Sessions

  Scenario: Serialize pattern to YAML
    Given a Pattern with 4 notes
    When I serialize to YAML
    Then the output is valid YAML
    And parsing the YAML returns an equivalent Pattern

  Scenario: Round-trip full sequence
    Given the file "examples/chill-ambient.sqnc.yaml"
    When I parse it to a Sequence
    And serialize back to YAML
    And parse again
    Then the two Sequence objects are equivalent

  Scenario: Save includes generation metadata
    Given I generate a pattern with prompt "bass in Cm"
    When I save the session
    Then the file contains "generated_by: sqncr"
    And the file contains the original prompt
    And the file contains a timestamp
```

### Definition of Done

```bash
dotnet test --filter "Serializer|RoundTrip"  # All pass
sqncr generate "bass in Cm" --save out.yaml  # Creates file
sqncr play out.yaml -d 0                     # Plays identically
```

---

## P4: Skills Framework

### Tests to Write FIRST

```
tests/SqncR.Core.Tests/Skills/
├── ISkillTests.cs          ← Interface contract
├── BassSkillTests.cs       ← Bass generation
├── PadSkillTests.cs        ← Pad/chord generation
├── DrumSkillTests.cs       ← Percussion generation
└── CompositionTests.cs     ← Combining skills
```

### Test Specifications

```gherkin
Feature: Skills Framework

  Scenario: Skill interface contract
    Given any implementation of ISkill
    When I call Generate(context)
    Then I receive Pattern[]
    And each Pattern is valid

  Scenario: Bass skill generates bass-appropriate content
    Given a BassSkill
    And context with key "C minor" tempo 120
    When I generate
    Then all notes are in octaves 1-3
    And note durations suggest rhythmic foundation

  Scenario: Skills compose into arrangement
    Given BassSkill, PadSkill, DrumSkill
    And context with key "F major" tempo 100
    When I compose all skills
    Then I get a Section with 3 tracks
    And track 1 is bass on channel 1
    And track 2 is pads on channel 2
    And track 3 is drums on channel 10
```

### Definition of Done

```bash
dotnet test --filter "Skills"                           # All pass
sqncr generate "chill in Cm" --skills bass,pads,drums   # Full arrangement
```

---

## P5: Interactive Mode

### Tests to Write FIRST

```
tests/SqncR.Core.Tests/Session/
├── SessionStateTests.cs     ← State management
├── CommandParserTests.cs    ← Parse modification commands
├── ModificationTests.cs     ← Apply changes to session
└── QuantizationTests.cs     ← Changes apply at bar boundaries
```

### Test Specifications

```gherkin
Feature: Interactive Mode

  Scenario: Session holds playing state
    Given a playing session with a bass pattern
    When I query the session
    Then I can see the current pattern
    And I can see playback position

  Scenario: Modify without stopping
    Given a playing session
    When I send command "make bass busier"
    Then playback continues
    And the bass pattern changes at next bar

  Scenario: Undo modification
    Given a session with modification history
    When I send command "undo"
    Then the previous state is restored
    And playback continues
```

### Definition of Done

```bash
dotnet test --filter "Session"    # All pass
sqncr interactive                  # Starts interactive mode
# Type "make it busier" → pattern changes live
# Type "undo" → reverts
```

---

## P6: MCP Server

### Tests to Write FIRST

```
tests/SqncR.Mcp.Tests/
├── McpProtocolTests.cs      ← JSON-RPC message handling
├── ToolRegistrationTests.cs ← Tool discovery
└── ToolExecutionTests.cs    ← Tool invocation
```

### Definition of Done

```bash
dotnet test --filter "Mcp"    # All pass
sqncr serve                    # Starts MCP server
# Claude Desktop connects
# "Generate a chill beat" → music plays
```

---

## P7: Polish

### Tests to Write FIRST

```
tests/SqncR.E2E.Tests/
├── NewUserExperienceTests.cs    ← Clone, build, run sequence
├── ErrorMessageTests.cs         ← Actionable error messages
└── DocumentationTests.cs        ← README examples work
```

### Definition of Done

```bash
dotnet test                       # ALL tests pass (100+ tests)
# Fresh clone → dotnet build → dotnet test → all green
# README quickstart works verbatim
# No compiler warnings
```

---

## Automation Summary

Every sprint gate is a single command:

```bash
dotnet test && echo "SPRINT COMPLETE" || echo "KEEP WORKING"
```

AI agents can:
1. Read this roadmap
2. Read existing tests
3. Write new tests for current sprint
4. Implement until `dotnet test` passes
5. Report completion

**The tests are the spec. The spec is the tests.**
