// Copyright © Roby Van Damme.

using DotBump.Common;
using DotBump.Tests.TestHelpers;
using Serilog;
using Shouldly;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Testing;

namespace DotBump.Tests;

/// <summary>
/// Tests the command app wiring. The integration test mutates the static <see cref="Log.Logger"/>, which is only safe
/// because <c>xunit.runner.json</c> disables parallel test collections.
/// </summary>
public class CommandConfigurationTests
{
    public class Configure
    {
        [Fact]
        public async Task With_Debug_And_LogFile_Configures_Logging_Before_Command_Runs()
        {
            var directory = new LocalDirectory("./temp");
            var logFile = Path.Combine(directory.AbsolutePath, "app.log");
            var missingGlobalJson = Path.Combine(directory.AbsolutePath, "missing.json");
            var originalLogger = Log.Logger;

            directory.EnsureDirectoryDeleted();
            directory.EnsureDirectoryCreated();

            try
            {
                using var testConsole = new TestConsole();
                testConsole.EmitAnsiSequences = false;
                IAnsiConsole console = testConsole;

                var commandApp = new CommandApp();
                commandApp.Configure(config => config.Settings.Registrar.RegisterInstance(console));
                commandApp.Configure(
                    new DelegatingLogger(),
                    new VersionInfo(typeof(CommandConfiguration).Assembly));

                var result = await commandApp.RunAsync(
                    ["sdk", "--debug", "--logfile", logFile, "--file", missingGlobalJson]);

                await Log.CloseAndFlushAsync();

                var writtenFile = Directory.GetFiles(directory.AbsolutePath, "app*.log").ShouldHaveSingleItem();
                var content = File.ReadAllText(writtenFile);
                result.ShouldSatisfyAllConditions(
                    () => result.ShouldBe(1),
                    () => content.ShouldContain("Starting sdk"),
                    () => content.ShouldContain("An error occurred while trying to bump the sdk"));
            }
            finally
            {
                await Log.CloseAndFlushAsync();
                Log.Logger = originalLogger;
                directory.EnsureDirectoryDeleted();
            }
        }
    }
}
