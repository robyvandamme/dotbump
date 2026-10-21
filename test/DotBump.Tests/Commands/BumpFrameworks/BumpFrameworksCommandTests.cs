// Copyright © Roby Van Damme.

using DotBump.Commands.BumpFrameworks;
using DotBump.Common;
using Moq;
using Serilog;
using Shouldly;
using Spectre.Console.Cli;
using Spectre.Console.Testing;

namespace DotBump.Tests.Commands.BumpFrameworks;

public class BumpFrameworksCommandTests
{
    public class ExecuteAsync
    {
        [Fact]
        public async Task With_Valid_Settings_Returns_1_And_Writes_Not_Implemented_Message()
        {
            using var testConsole = new TestConsole();
            var command = new BumpFrameworksCommand(testConsole, Mock.Of<ILogger>());

            var result = await command.ExecuteAsync(
                CreateContext("frameworks"),
                new BumpFrameworksSettings(),
                CancellationToken.None);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBe(1),
                () => testConsole.Output.ShouldContain("The frameworks command is not implemented yet."));
        }

        [Fact]
        public async Task With_Unexpected_Command_Name_Throws_DotBumpException()
        {
            using var testConsole = new TestConsole();
            var command = new BumpFrameworksCommand(testConsole, Mock.Of<ILogger>());

            await Should.ThrowAsync<DotBumpException>(() => command.ExecuteAsync(
                CreateContext("packages"),
                new BumpFrameworksSettings(),
                CancellationToken.None));
        }

        private static CommandContext CreateContext(string commandName)
        {
            return new CommandContext(
                ["bump", commandName],
                new Mock<IRemainingArguments>().Object,
                commandName,
                null);
        }
    }
}
