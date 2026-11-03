// Copyright © Roby Van Damme.

using DotBump.Commands.BumpSdk;
using DotBump.Common;
using Moq;
using Serilog;
using Serilog.Events;
using Shouldly;
using Spectre.Console.Cli;

namespace DotBump.Tests;

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
                var interceptor = CreateInterceptor();

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
                var interceptor = CreateInterceptor();

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
            var directory = Path.Combine(Path.GetTempPath(), $"dotbump-logging-{Guid.NewGuid():N}");
            var logFile = Path.Combine(directory, "app.log");
            var versionInfo = new VersionInfo(typeof(LoggingInterceptor).Assembly);
            var originalLogger = Log.Logger;

            Directory.CreateDirectory(directory);

            try
            {
                var interceptor = new LoggingInterceptor(versionInfo);

                interceptor.Intercept(
                    CreateContext("sdk"),
                    new BumpSdkSettings { Debug = true, LogFile = logFile });

                Log.CloseAndFlush();

                var writtenFile = Directory.GetFiles(directory, "app*.log").ShouldHaveSingleItem();
                var content = File.ReadAllText(writtenFile);
                versionInfo.Version.ShouldNotBeNull();
                content.ShouldContain(versionInfo.Version);
                content.ShouldContain("Starting sdk");
            }
            finally
            {
                ResetLogger(originalLogger);
                Directory.Delete(directory, true);
            }
        }

        private static LoggingInterceptor CreateInterceptor()
        {
            return new LoggingInterceptor(new VersionInfo(typeof(LoggingInterceptor).Assembly));
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
