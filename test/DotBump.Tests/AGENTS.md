# Test Instructions

When working on tests, follow these guidelines.

## Tech Stack

xUnit v2, Shouldly, Moq (Loose), Spectre.Console.Testing

## Structure

* Layout: Mirror production structure. `src/DotBump/Path/To/Class.cs` -> `test/DotBump.Tests/Path/To/ClassTests.cs`.
* Hierarchy: One top-level class (`{ClassName}Tests`) containing nested `public class` groups per method (nested classes
  can further group distinct scenarios when testing multiple fixture variations).
* For constructors use 'Constructor' as the nested class name, for standard methods like `Equals` use 'Equals_' as the
  nested class name to avoid conflicts.
* Naming: Pascal_Snake_Case: `With_{Condition}_Returns_{Outcome}`, `{Condition}_Returns_{Outcome}`, or
  `With_{Condition}_Throws_{Exception}`.

## Patterns

* AAA: Always follow Arrange-Act-Assert.
* Assertions: Use Shouldly for all value/state assertions. Use `ShouldSatisfyAllConditions` for multiple assertions on
  one outcome. Use Shouldly's `Should.Throw` / `Should.ThrowAsync` for exception assertions. Use Moq `Verify` for
  interaction/mocking assertions where appropriate.
* Do not use the terms `SUT` or `ACT` in code. Instead, use descriptive names that do not need comments to explain their
  purpose.
* One logical outcome per test method.
* CLI & Command Testing: Test commands via `ExecuteForTestAsync` using `Spectre.Console.Testing.TestConsole`, mock
  `IRemainingArguments`, and assert on exit codes and console output.
* File System & Isolation: Keep disk-touching tests isolated. Use `LocalDirectory("./temp")` and helpers
  (`EnsureFileCreated`, `EnsureFileDeleted`) or ensure generated output files are deleted before/after execution. Never
  mutate permanent repo files.
* Test Data & Fixtures: Use offline JSON files under `Data/` (e.g., `Data/NuGet/*`, `Data/global.json`) instead of live
  network calls. Refer to `TESTS.md` for documented test cases, data feeds, and pinned tool versions.

## Canonical Examples

### Service / Unit Test

```csharp
public class MyMethod
{
    // shared state 
    private readonly Mock<IDependency> _dependency = new();

    [Fact]
    public async Task With_Valid_Input_Returns_Expected_Value()
    {
        // Arrange
        _dependency.Setup(d => d.Get()).Returns("value");
        var myClass = new MyClass(_dependency.Object);

        // Act
        var result = await myClass.MyMethod("input");

        // Assert
        result.ShouldBe("expected");
    }

    [Fact]
    public async Task With_Null_Input_Throws_ArgumentNullException()
    {
        var myClass = new MyClass(_dependency.Object);

        await Should.ThrowAsync<ArgumentNullException>(() => myClass.MyMethod(null));
    }
}
```

### CLI / Command Test

```csharp
public class ExecuteForTestAsync
{
    [Fact]
    public async Task With_Valid_Settings_Returns_0()
    {
        // Arrange
        using var testConsole = new TestConsole();
        var remainingArguments = new Mock<IRemainingArguments>();
        var context = new CommandContext(new[] { "bump", "tools" }, remainingArguments.Object, "tools", null);
        var command = new BumpToolsCommand(testConsole, Mock.Of<ILogger>(), Mock.Of<IBumpToolsHandler>());

        // Act
        var result = await command.ExecuteForTestAsync(context, new BumpToolsSettings(), CancellationToken.None);

        // Assert
        result.ShouldBe(0);
    }
}
```
