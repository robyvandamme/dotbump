// Copyright © 2025 Roby Van Damme.

using Shouldly;

namespace DotBump.Tests;

public class ArgumentHandlerTests
{
    public class IsDebug
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

    public class LogFile
    {
        [Fact]
        public void Returns_Empty_String_When_Args_Array_Is_Empty()
        {
            var args = Array.Empty<string>();
            var result = ArgumentHandler.LogFile(args);
            result.ShouldBe(string.Empty);
        }

        [Fact]
        public void Returns_Empty_String_When_LogFile_Argument_Not_Found()
        {
            var args = new[] { "--verbose", "--config", "config.json" };
            var result = ArgumentHandler.LogFile(args);
            result.ShouldBe(string.Empty);
        }

        [Fact]
        public void Returns_LogFile_Value_When_LogFile_Argument_Is_Present()
        {
            var args = new[] { "--verbose", "--logfile", "app.log", "--config", "config.json" };
            var result = ArgumentHandler.LogFile(args);
            result.ShouldBe("app.log");
        }

        [Fact]
        public void Returns_LogFile_Value_When_LogFile_Argument_Is_First()
        {
            var args = new[] { "--logfile", "first.log", "--verbose" };
            var result = ArgumentHandler.LogFile(args);
            result.ShouldBe("first.log");
        }

        [Fact]
        public void Returns_LogFile_Value_When_LogFile_Argument_Is_Last_With_Value()
        {
            var args = new[] { "--verbose", "--logfile", "last.log" };
            var result = ArgumentHandler.LogFile(args);
            result.ShouldBe("last.log");
        }

        [Fact]
        public void Returns_Empty_String_When_LogFile_Argument_Is_Last_Without_Value()
        {
            var args = new[] { "--verbose", "--config", "config.json", "--logfile" };
            var result = ArgumentHandler.LogFile(args);
            result.ShouldBe(string.Empty);
        }

        [Fact]
        public void Is_Case_Insensitive_For_LogFile_Argument()
        {
            var args = new[] { "--LOGFILE", "uppercase.log" };
            var result = ArgumentHandler.LogFile(args);
            result.ShouldBe("uppercase.log");
        }

        [Fact]
        public void Is_Case_Insensitive_For_Mixed_Case_LogFile_Argument()
        {
            var args = new[] { "--LogFile", "mixedcase.log" };
            var result = ArgumentHandler.LogFile(args);
            result.ShouldBe("mixedcase.log");
        }

        [Fact]
        public void Returns_First_LogFile_Value_When_Multiple_LogFile_Arguments_Present()
        {
            var args = new[] { "--logfile", "first.log", "--verbose", "--logfile", "second.log" };
            var result = ArgumentHandler.LogFile(args);
            result.ShouldBe("first.log");
        }

        [Fact]
        public void Returns_LogFile_Value_With_Path_And_Extension()
        {
            var args = new[] { "--logfile", "/var/logs/application.log" };
            var result = ArgumentHandler.LogFile(args);
            result.ShouldBe("/var/logs/application.log");
        }
    }
}
