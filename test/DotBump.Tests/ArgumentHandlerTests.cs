// Copyright © Roby Van Damme.

using Shouldly;

namespace DotBump.Tests;

public class ArgumentHandlerTests
{
    public class IsDebugMode
    {
        [Fact]
        public void With_Empty_Args_Returns_False()
        {
            var args = Array.Empty<string>();
            var result = ArgumentHandler.IsDebugMode(args);
            result.ShouldBeFalse();
        }

        [Fact]
        public void Without_Debug_Flag_Returns_False()
        {
            var args = new[] { "--other", "value" };
            var result = ArgumentHandler.IsDebugMode(args);
            result.ShouldBeFalse();
        }

        [Fact]
        public void With_Debug_Flag_As_Last_Arg_Returns_False()
        {
            var args = new[] { "--other", "--debug" };
            var result = ArgumentHandler.IsDebugMode(args);
            result.ShouldBeFalse();
        }

        [Fact]
        public void With_Debug_Flag_And_True_Value_Returns_True()
        {
            var args = new[] { "--debug", "true" };
            var result = ArgumentHandler.IsDebugMode(args);
            result.ShouldBeTrue();
        }

        [Fact]
        public void With_Debug_Flag_And_Uppercase_True_Value_Returns_True()
        {
            var args = new[] { "--debug", "TRUE" };
            var result = ArgumentHandler.IsDebugMode(args);
            result.ShouldBeTrue();
        }

        [Fact]
        public void With_Debug_Flag_And_Non_True_Value_Returns_False()
        {
            var args = new[] { "--debug", "yes" };
            var result = ArgumentHandler.IsDebugMode(args);
            result.ShouldBeFalse();
        }

        [Fact]
        public void With_Uppercase_Debug_Flag_And_True_Value_Returns_True()
        {
            var args = new[] { "--DEBUG", "true" };
            var result = ArgumentHandler.IsDebugMode(args);
            result.ShouldBeTrue();
        }

        [Fact]
        public void With_Debug_Flag_In_Middle_Of_Args_And_True_Value_Returns_True()
        {
            var args = new[] { "--first", "value", "--debug", "true", "--other", "flag" };
            var result = ArgumentHandler.IsDebugMode(args);
            result.ShouldBeTrue();
        }
    }

    public class LogFile
    {
        [Fact]
        public void With_Empty_Args_Returns_Empty_String()
        {
            var args = Array.Empty<string>();
            var result = ArgumentHandler.LogFile(args);
            result.ShouldBe(string.Empty);
        }

        [Fact]
        public void Without_LogFile_Argument_Returns_Empty_String()
        {
            var args = new[] { "--verbose", "--config", "config.json" };
            var result = ArgumentHandler.LogFile(args);
            result.ShouldBe(string.Empty);
        }

        [Fact]
        public void With_LogFile_Argument_Returns_LogFile_Value()
        {
            var args = new[] { "--verbose", "--logfile", "app.log", "--config", "config.json" };
            var result = ArgumentHandler.LogFile(args);
            result.ShouldBe("app.log");
        }

        [Fact]
        public void With_LogFile_Argument_First_Returns_LogFile_Value()
        {
            var args = new[] { "--logfile", "first.log", "--verbose" };
            var result = ArgumentHandler.LogFile(args);
            result.ShouldBe("first.log");
        }

        [Fact]
        public void With_LogFile_Argument_Last_With_Value_Returns_LogFile_Value()
        {
            var args = new[] { "--verbose", "--logfile", "last.log" };
            var result = ArgumentHandler.LogFile(args);
            result.ShouldBe("last.log");
        }

        [Fact]
        public void With_LogFile_Argument_Last_Without_Value_Returns_Empty_String()
        {
            var args = new[] { "--verbose", "--config", "config.json", "--logfile" };
            var result = ArgumentHandler.LogFile(args);
            result.ShouldBe(string.Empty);
        }

        [Fact]
        public void With_Uppercase_LogFile_Argument_Returns_LogFile_Value()
        {
            var args = new[] { "--LOGFILE", "uppercase.log" };
            var result = ArgumentHandler.LogFile(args);
            result.ShouldBe("uppercase.log");
        }

        [Fact]
        public void With_MixedCase_LogFile_Argument_Returns_LogFile_Value()
        {
            var args = new[] { "--LogFile", "mixedcase.log" };
            var result = ArgumentHandler.LogFile(args);
            result.ShouldBe("mixedcase.log");
        }

        [Fact]
        public void With_Multiple_LogFile_Arguments_Returns_First_LogFile_Value()
        {
            var args = new[] { "--logfile", "first.log", "--verbose", "--logfile", "second.log" };
            var result = ArgumentHandler.LogFile(args);
            result.ShouldBe("first.log");
        }

        [Fact]
        public void With_Path_And_Extension_Returns_LogFile_Value()
        {
            var args = new[] { "--logfile", "/var/logs/application.log" };
            var result = ArgumentHandler.LogFile(args);
            result.ShouldBe("/var/logs/application.log");
        }
    }
}
