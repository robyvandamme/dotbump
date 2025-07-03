// Copyright © 2025 Roby Van Damme.

using DotBump.Common;
using Shouldly;

namespace DotBump.Tests.Common;

public class ArgumentHandlerTests
{
    [Fact]
    public void Args_Is_Empty_Returns_False()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var result = ArgumentHandler.IsDebugMode(args);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Debug_Flag_Is_Not_Present_Returns_False()
    {
        // Arrange
        var args = new[] { "--other", "value" };

        // Act
        var result = ArgumentHandler.IsDebugMode(args);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Debug_Flag_Is_Present_As_Last_Arg_Returns_False()
    {
        // Arrange
        var args = new[] { "--other", "--debug" };

        // Act
        var result = ArgumentHandler.IsDebugMode(args);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Debug_Flag_With_True_Value_Returns_True()
    {
        // Arrange
        var args = new[] { "--debug", "true" };

        // Act
        var result = ArgumentHandler.IsDebugMode(args);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Debug_Flag_With_True_Uppercase_Value_Returns_True()
    {
        // Arrange
        var args = new[] { "--debug", "TRUE" };

        // Act
        var result = ArgumentHandler.IsDebugMode(args);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Debug_Flag_With_Non_True_Value_Returns_False()
    {
        // Arrange
        var args = new[] { "--debug", "yes" };

        // Act
        var result = ArgumentHandler.IsDebugMode(args);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Debug_Uppercase_Flag_With_True_Value_Returns_True()
    {
        // Arrange
        var args = new[] { "--DEBUG", "true" };

        // Act
        var result = ArgumentHandler.IsDebugMode(args);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Debug_Flag_In_Middle_Of_Args_With_True_Value_Returns_True()
    {
        // Arrange
        var args = new[] { "--first", "value", "--debug", "true", "--other", "flag" };

        // Act
        var result = ArgumentHandler.IsDebugMode(args);

        // Assert
        result.ShouldBeTrue();
    }
}
