# SKILL: Example File Testing Pattern

**Confidence:** low
**Source:** earned

## What

When testing parsers or loaders in SqncR, use the actual example `.sqnc.yaml` files from `examples/` as test fixtures rather than constructing synthetic YAML in tests. This gives real-world coverage and catches deserialization issues that synthetic tests miss.

## How

```csharp
private static string ExamplesDir =>
    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "examples");

[Theory]
[InlineData("chill-ambient.sqnc.yaml", "Late Night Ambient", 70, "Cm")]
public void Parse_ExampleFile_ReturnsCorrectMeta(string fileName, string expectedTitle, int expectedTempo, string expectedKey)
{
    var path = Path.GetFullPath(Path.Combine(ExamplesDir, fileName));
    var seq = _parser.Parse(path);
    Assert.Equal(expectedTitle, seq.Meta.Title);
}
```

## Key Points

- Use `AppContext.BaseDirectory` + relative path traversal to find `examples/` from test bin output directory
- Use `Path.GetFullPath()` to normalize the path
- Parameterize with `[Theory]`/`[InlineData]` so each example file is a separate test case
- Document known deserialization failures as explicit `Assert.ThrowsAny<Exception>()` tests — don't just skip broken files
- When an example file can't be parsed due to model limitations, that's a finding worth documenting as a decision

## Caveats

- Path traversal depth (`..` count) depends on the test project's position relative to repo root — currently 5 levels from `tests/SqncR.Core.Tests/bin/Debug/net9.0/`
- If the project structure changes, the path needs updating
