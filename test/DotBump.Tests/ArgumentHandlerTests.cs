// Copyright © 2025 Roby Van Damme.

using Shouldly;

namespace DotBump.Tests;

public class ArgumentHandlerTests
{
    [Fact]
    public void Args_Is_Empty_Returns_False()
    {
        var args = Array.Empty<string>();

        var result = ArgumentHandler.IsDebugMode(args);

        result.ShouldBeFalse();
    }

    [Fact]
    public void Debug_Flag_Is_Not_Present_Returns_False()
    {
        var args = new[] { "--other", "value" };

        var result = ArgumentHandler.IsDebugMode(args);

        result.ShouldBeFalse();
    }

    [Fact]
    public void Debug_Flag_Is_Present_As_Last_Arg_Returns_False()
    {
        var args = new[] { "--other", "--debug" };

        var result = ArgumentHandler.IsDebugMode(args);

        result.ShouldBeFalse();
    }

    [Fact]
    public void Debug_Flag_With_True_Value_Returns_True()
    {
        var args = new[] { "--debug", "true" };

        var result = ArgumentHandler.IsDebugMode(args);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Debug_Flag_With_True_Uppercase_Value_Returns_True()
    {
        var args = new[] { "--debug", "TRUE" };

        var result = ArgumentHandler.IsDebugMode(args);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Debug_Flag_With_Non_True_Value_Returns_False()
    {
        var args = new[] { "--debug", "yes" };

        var result = ArgumentHandler.IsDebugMode(args);

        result.ShouldBeFalse();
    }

    [Fact]
    public void Debug_Uppercase_Flag_With_True_Value_Returns_True()
    {
        var args = new[] { "--DEBUG", "true" };

        var result = ArgumentHandler.IsDebugMode(args);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Debug_Flag_In_Middle_Of_Args_With_True_Value_Returns_True()
    {
        var args = new[] { "--first", "value", "--debug", "true", "--other", "flag" };

        var result = ArgumentHandler.IsDebugMode(args);

        result.ShouldBeTrue();
    }
}
