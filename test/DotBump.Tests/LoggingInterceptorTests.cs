// Copyright © Roby Van Damme.

using DotBump.Commands.BumpSdk;
using DotBump.Common;
using DotBump.Tests.TestHelpers;
using Moq;
using Serilog;
using Serilog.Events;
using Shouldly;
using Spectre.Console.Cli;

namespace DotBump.Tests;

/// <summary>
/// Tests for the logging interceptor. These mutate the static <see cref="Log.Logger"/>, which is only safe because
/// <c>xunit.runner.json</c> disables parallel test collections.
/// </summary>
public class LoggingInterceptorTests
{
    public class Intercept
    {
        [Fact]
        public void With_Debug_True_Enables_Debug_Logging()
        {
            var originalLogger = Log.Logger;

            try
            {
                var interceptor = CreateInterceptor(CreateVersionInfo());

                interceptor.Intercept(CreateContext("sdk"), new BumpSdkSettings { Debug = true });

                Log.IsEnabled(LogEventLevel.Debug).ShouldBeTrue();
            }
            finally
            {
                ResetLogger(originalLogger);
            }
        }

        [Fact]
        public void With_Debug_False_Keeps_Error_Level()
        {
            var originalLogger = Log.Logger;

            try
            {
                var interceptor = CreateInterceptor(CreateVersionInfo());

                interceptor.Intercept(CreateContext("sdk"), new BumpSdkSettings { Debug = false });

                Log.IsEnabled(LogEventLevel.Debug).ShouldBeFalse();
                Log.IsEnabled(LogEventLevel.Error).ShouldBeTrue();
            }
            finally
            {
                ResetLogger(originalLogger);
            }
        }

        [Fact]
        public void With_LogFile_Writes_Startup_Diagnostics_To_File()
        {
            var directory = new LocalDirectory("./temp");
            var logFile = Path.Combine(directory.AbsolutePath, "app.log");
            var versionInfo = CreateVersionInfo();
            var originalLogger = Log.Logger;

            directory.EnsureDirectoryDeleted();
            directory.EnsureDirectoryCreated();

            try
            {
                var interceptor = CreateInterceptor(versionInfo);

                interceptor.Intercept(
                    CreateContext("sdk"),
                    new BumpSdkSettings { Debug = true, LogFile = logFile });

                Log.CloseAndFlush();

                var writtenFile = Directory.GetFiles(directory.AbsolutePath, "app*.log").ShouldHaveSingleItem();
                var content = File.ReadAllText(writtenFile);
                versionInfo.Version.ShouldNotBeNull();
                content.ShouldContain(versionInfo.Version);
                content.ShouldContain("Starting sdk");
            }
            finally
            {
                ResetLogger(originalLogger);
                directory.EnsureDirectoryDeleted();
            }
        }

        private static VersionInfo CreateVersionInfo()
        {
            return new VersionInfo(typeof(LoggingInterceptor).Assembly);
        }

        private static LoggingInterceptor CreateInterceptor(VersionInfo versionInfo)
        {
            return new LoggingInterceptor(versionInfo);
        }

        private static CommandContext CreateContext(string commandName)
        {
            var remainingArguments = new Mock<IRemainingArguments>();
            return new CommandContext(["dotbump", commandName], remainingArguments.Object, commandName, null);
        }

        private static void ResetLogger(ILogger originalLogger)
        {
            Log.CloseAndFlush();
            Log.Logger = originalLogger;
        }
    }
}
