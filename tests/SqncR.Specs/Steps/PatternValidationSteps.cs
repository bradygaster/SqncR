using SqncR.Core.Models;
using TechTalk.SpecFlow;

namespace SqncR.Specs.Steps;

[Binding]
public class PatternValidationSteps
{
    private Pattern _pattern = new();
    private NoteEvent _currentEvent = new();
    private int? _overrideMidiNote;
    private ValidationResult? _result;

    [Given(@"a pattern with length (.*)")]
    public void GivenAPatternWithLength(int length)
    {
        _pattern.Length = length;
    }

    [Given(@"the pattern has (.*) note events")]
    public void GivenThePatternHasNoteEvents(int count)
    {
        _pattern.Events.Clear();
        for (int i = 0; i < count; i++)
        {
            _pattern.Events.Add(new NoteEvent
            {
                T = i * 480,
                Type = "note",
                Note = "C4",
                Vel = 80,
                Dur = 480
            });
        }
    }

    [Given(@"a pattern with a note event")]
    public void GivenAPatternWithANoteEvent()
    {
        _pattern = new Pattern { Length = 1920 };
        _currentEvent = new NoteEvent
        {
            T = 0,
            Type = "note",
            Note = "C4",
            Vel = 80,
            Dur = 480
        };
        _pattern.Events.Add(_currentEvent);
        _overrideMidiNote = null;
    }

    [Given(@"the note value is (.*)")]
    public void GivenTheNoteValueIs(int midiNote)
    {
        // Override MIDI note directly for validation testing
        _overrideMidiNote = midiNote;
    }

    [Given(@"the velocity is (.*)")]
    public void GivenTheVelocityIs(int velocity)
    {
        _currentEvent.Vel = velocity;
    }

    [Given(@"the duration is (.*)")]
    public void GivenTheDurationIs(int duration)
    {
        _currentEvent.Dur = duration;
    }

    [Given(@"a note event starting at tick (.*)")]
    public void GivenANoteEventStartingAtTick(int tick)
    {
        _currentEvent = new NoteEvent
        {
            T = tick,
            Type = "note",
            Note = "C4",
            Vel = 80,
            Dur = 480
        };
        _pattern.Events.Clear();
        _pattern.Events.Add(_currentEvent);
    }

    [When(@"I validate the pattern")]
    public void WhenIValidateThePattern()
    {
        // PatternValidator is what we need to implement!
        _result = PatternValidator.Validate(_pattern, _overrideMidiNote);
    }

    [Then(@"validation should pass")]
    public void ThenValidationShouldPass()
    {
        Assert.NotNull(_result);
        Assert.True(_result.IsValid, $"Expected valid but got: {_result.Error}");
    }

    [Then(@"validation should fail with ""(.*)""")]
    public void ThenValidationShouldFailWith(string expectedError)
    {
        Assert.NotNull(_result);
        Assert.False(_result.IsValid, "Expected validation to fail but it passed");
        Assert.Contains(expectedError, _result.Error ?? "");
    }
}

// ============================================================
// BELOW: Placeholder implementation that MUST move to SqncR.Core
// Tests will fail with NotImplementedException until this is done
// ============================================================

public record ValidationResult(bool IsValid, string? Error = null)
{
    public static ValidationResult Success() => new(true);
    public static ValidationResult Failure(string error) => new(false, error);
}

public static class PatternValidator
{
    public static ValidationResult Validate(Pattern pattern, int? overrideMidiNote = null)
    {
        // Step 1: Uncomment this to see tests fail properly
        // throw new NotImplementedException("Implement PatternValidator to make tests pass!");

        // Step 2: Replace with real implementation
        if (pattern.Length <= 0)
            return ValidationResult.Failure("Pattern length must be positive");

        if (pattern.Events.Count == 0)
            return ValidationResult.Failure("Pattern must have at least one event");

        foreach (var evt in pattern.Events)
        {
            // Check MIDI note (use override if testing specific value)
            var midiNote = overrideMidiNote ?? SqncR.Core.NoteParser.Parse(evt.Note);
            if (midiNote < 0 || midiNote > 127)
                return ValidationResult.Failure("Note must be between 0 and 127");

            // Check velocity
            var velocity = evt.Vel switch
            {
                int v => v,
                long v => (int)v,
                _ => 80
            };
            if (velocity < 1 || velocity > 127)
                return ValidationResult.Failure("Velocity must be between 1 and 127");

            // Check duration
            if (evt.Dur <= 0)
                return ValidationResult.Failure("Duration must be positive");

            // Check event timing
            if (evt.T >= pattern.Length)
                return ValidationResult.Failure("Event starts after pattern end");
        }

        return ValidationResult.Success();
    }
}
