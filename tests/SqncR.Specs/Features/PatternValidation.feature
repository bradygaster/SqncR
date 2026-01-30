Feature: Pattern Validation
  As a developer
  I want generated patterns to be validated
  So that only valid MIDI data reaches the hardware

  # These tests define what "valid pattern" means.
  # Implementation must satisfy ALL scenarios before P1 is complete.

  Scenario: Valid pattern has required properties
    Given a pattern with length 1920
    And the pattern has 4 note events
    When I validate the pattern
    Then validation should pass

  Scenario: Pattern must have positive length
    Given a pattern with length 0
    When I validate the pattern
    Then validation should fail with "Pattern length must be positive"

  Scenario: Pattern must have events
    Given a pattern with length 1920
    And the pattern has 0 note events
    When I validate the pattern
    Then validation should fail with "Pattern must have at least one event"

  Scenario: Note events must have valid MIDI note numbers
    Given a pattern with a note event
    And the note value is 128
    When I validate the pattern
    Then validation should fail with "Note must be between 0 and 127"

  Scenario: Note events must have valid velocity
    Given a pattern with a note event
    And the velocity is 0
    When I validate the pattern
    Then validation should fail with "Velocity must be between 1 and 127"

  Scenario: Note events must have positive duration
    Given a pattern with a note event
    And the duration is 0
    When I validate the pattern
    Then validation should fail with "Duration must be positive"

  Scenario: Note events must start within pattern bounds
    Given a pattern with length 1920
    And a note event starting at tick 2000
    When I validate the pattern
    Then validation should fail with "Event starts after pattern end"

  Scenario Outline: Valid note values are accepted
    Given a pattern with a note event
    And the note value is <note>
    When I validate the pattern
    Then validation should pass

    Examples:
      | note |
      | 0    |
      | 60   |
      | 127  |

  Scenario Outline: Valid velocities are accepted
    Given a pattern with a note event
    And the velocity is <velocity>
    When I validate the pattern
    Then validation should pass

    Examples:
      | velocity |
      | 1        |
      | 64       |
      | 127      |
