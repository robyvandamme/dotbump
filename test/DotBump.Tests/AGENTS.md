# Unit Test Instructions

When working on unit tests, follow these guidelines.

## Tech Stack

xUnit v3, Shouldly, Moq (Loose)

## Structure

* Layout: Mirror production structure. `Path/To/Class.cs` -> `test/UnitTests/Path/To/ClassTests.cs`.
* Hierarchy: One top-level class (`{ClassName}Tests`) containing nested `public class` groups per method.
* For constructors use 'Constructor' as the nested class name, for standard methods like `Equals` use 'Equals_' as the
  nested class name to avoid conflicts.
* Naming: Pascal_Snake_Case: `With_{Condition}_Returns_{Outcome}` or `With_{Condition}_Throws_{Exception}`.

## Patterns

* AAA: Always follow Arrange-Act-Assert.
* Assertions: Use Shouldly for all value/state assertions. Use ShouldSatisfyAllConditions for multiple assertions on one
  outcome. Use Shouldly's ShouldThrow for exception assertions. Use Moq Verify for interaction/mocking assertions where
  appropriate.
* Do not use the terms `SUT` or `ACT` in code. Instead, use descriptive names that do not need comments to explain their
  purpose.
* One logical outcome per test method.

## Canonical Example

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
