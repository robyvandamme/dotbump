// Copyright © Roby Van Damme.

using DotBump.Commands;
using DotBump.Commands.BumpDocker;
using DotBump.Commands.BumpDocker.DataModel;
using DotBump.Commands.BumpDocker.Interfaces;
using DotBump.Commands.BumpSdk;
using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Common;
using DotBump.Reports;
using DotBump.Tests.Commands.BumpDocker.Fakes;
using DotBump.Tests.TestHelpers;
using Moq;
using Serilog;
using Shouldly;
using Spectre.Console.Cli;
using Spectre.Console.Testing;

namespace DotBump.Tests.Commands.BumpDocker;

public class BumpDockerCommandTests
{
    public class ExecuteAsync
    {
        [Fact]
        public async Task With_Changes_Returns_0_And_Writes_Report()
        {
            ResetTempDirectory();
            var reportPath = Path.Combine(TempDirectory.AbsolutePath, "docker.json");
            var manifest = CreateChangedManifest();
            var handler = new Mock<IBumpDockerHandler>();
            handler
                .Setup(h => h.HandleAsync(
                    It.IsAny<BumpType>(),
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyCollection<string>>(),
                    It.IsAny<bool>()))
                .ReturnsAsync(new BumpReport(manifest, BumpType.Minor));

            using var testConsole = new TestConsole();
            var command = new BumpDockerCommand(testConsole, Mock.Of<ILogger>(), handler.Object);

            var result = await command.ExecuteAsync(
                CreateContext("docker"),
                new BumpDockerSettings { Output = reportPath },
                CancellationToken.None);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBe(0),
                () => testConsole.Output.ShouldContain("Docker image tags bumped"),
                () => File.Exists(reportPath).ShouldBeTrue());
        }

        [Fact]
        public async Task With_No_Changes_Returns_0_And_Reports_No_Changes()
        {
            var handler = new Mock<IBumpDockerHandler>();
            handler
                .Setup(h => h.HandleAsync(
                    It.IsAny<BumpType>(),
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyCollection<string>>(),
                    It.IsAny<bool>()))
                .ReturnsAsync(new BumpReport(new DockerfileManifest(), BumpType.Minor));

            using var testConsole = new TestConsole();
            var command = new BumpDockerCommand(testConsole, Mock.Of<ILogger>(), handler.Object);

            var result = await command.ExecuteAsync(
                CreateContext("docker"),
                new BumpDockerSettings(),
                CancellationToken.None);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBe(0),
                () => testConsole.Output.ShouldContain("No Docker image tags were bumped."));
        }

        [Fact]
        public async Task With_Handler_Throws_Returns_1()
        {
            var handler = new Mock<IBumpDockerHandler>();
            handler
                .Setup(h => h.HandleAsync(
                    It.IsAny<BumpType>(),
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyCollection<string>>(),
                    It.IsAny<bool>()))
                .ThrowsAsync(new DotBumpException("boom"));

            using var testConsole = new TestConsole();
            var command = new BumpDockerCommand(testConsole, Mock.Of<ILogger>(), handler.Object);

            var result = await command.ExecuteAsync(
                CreateContext("docker"),
                new BumpDockerSettings(),
                CancellationToken.None);

            result.ShouldBe(1);
        }

        [Fact]
        public async Task With_Unexpected_Command_Name_Throws_DotBumpException()
        {
            using var testConsole = new TestConsole();
            var command = new BumpDockerCommand(
                testConsole,
                Mock.Of<ILogger>(),
                Mock.Of<IBumpDockerHandler>());

            await Should.ThrowAsync<DotBumpException>(() => command.ExecuteAsync(
                CreateContext("packages"),
                new BumpDockerSettings(),
                CancellationToken.None));
        }

        [Fact]
        public async Task With_Sdk_Image_And_Newer_Release_Updates_Dockerfile_And_Writes_Markdown()
        {
            ResetTempDirectory();
            var dockerfilePath = Path.Combine(TempDirectory.AbsolutePath, "Dockerfile");
            var reportPath = Path.Combine(TempDirectory.AbsolutePath, "docker.md");
            const string dockerfileContent =
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.100 AS build\n" +
                "FROM mcr.microsoft.com/dotnet/aspnet:10.0.3 AS runtime\n";
            File.WriteAllText(dockerfilePath, dockerfileContent);

            var logger = Mock.Of<ILogger>();
            var handler = new BumpDockerHandler(
                new DockerfileService(logger),
                new FakeReleaseService(new Release("10.0", "10.0.101", "lts", false)),
                new ReleaseFinder(logger),
                logger);

            using var testConsole = new TestConsole();
            var command = new BumpDockerCommand(testConsole, logger, handler);

            var result = await command.ExecuteAsync(
                CreateContext("docker"),
                new BumpDockerSettings
                {
                    RepositoryPath = TempDirectory.AbsolutePath,
                    Output = reportPath,
                },
                CancellationToken.None);

            var dockerfile = await File.ReadAllTextAsync(dockerfilePath);
            var markdown = await File.ReadAllTextAsync(reportPath);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBe(0),
                () => dockerfile.ShouldContain("sdk:10.0.101 AS build"),
                () => dockerfile.ShouldContain("aspnet:10.0.3"),
                () => markdown.ShouldContain("## Docker"),
                () => markdown.ShouldContain("mcr.microsoft.com/dotnet/sdk: 10.0.100 → 10.0.101"));
        }

        private static DockerfileManifest CreateChangedManifest()
        {
            var manifest = new DockerfileManifest();
            var entry = new DockerImageEntry(
                "mcr.microsoft.com/dotnet/sdk",
                "Dockerfile",
                "/repo/Dockerfile",
                1,
                "10.0.100",
                0,
                8);
            manifest.Add(entry);
            manifest.SetTag(entry, "10.0.101");
            return manifest;
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

    private static LocalDirectory TempDirectory => new("./temp/docker-command");

    private static void ResetTempDirectory()
    {
        TempDirectory.EnsureDirectoryDeleted();
        TempDirectory.EnsureDirectoryCreated();
    }
}
