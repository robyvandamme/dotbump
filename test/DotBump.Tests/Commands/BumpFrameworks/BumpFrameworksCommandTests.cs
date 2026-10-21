// Copyright © Roby Van Damme.

using System.Text.Json;
using DotBump.Commands;
using DotBump.Commands.BumpFrameworks;
using DotBump.Commands.BumpFrameworks.DataModel;
using DotBump.Commands.BumpFrameworks.Interfaces;
using DotBump.Commands.BumpSdk;
using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Common;
using DotBump.Reports;
using DotBump.Tests.TestHelpers;
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
        public async Task With_Changes_Returns_0_And_Writes_Report()
        {
            ResetTempDirectory();
            var reportPath = Path.Combine(TempDirectory.AbsolutePath, "frameworks.json");
            var manifest = CreateChangedManifest();
            var handler = new Mock<IBumpFrameworksHandler>();
            handler
                .Setup(h => h.HandleAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyCollection<string>>()))
                .ReturnsAsync(new BumpReport(manifest, BumpType.Major));

            using var testConsole = new TestConsole();
            var command = new BumpFrameworksCommand(testConsole, Mock.Of<ILogger>(), handler.Object);

            var result = await command.ExecuteAsync(
                CreateContext("frameworks"),
                new BumpFrameworksSettings { Output = reportPath },
                CancellationToken.None);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBe(0),
                () => testConsole.Output.ShouldContain("Target frameworks bumped"),
                () => File.Exists(reportPath).ShouldBeTrue());
        }

        [Fact]
        public async Task With_No_Changes_Returns_0_And_Reports_No_Changes()
        {
            var handler = new Mock<IBumpFrameworksHandler>();
            handler
                .Setup(h => h.HandleAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyCollection<string>>()))
                .ReturnsAsync(new BumpReport(new FrameworkManifest(), BumpType.Major));

            using var testConsole = new TestConsole();
            var command = new BumpFrameworksCommand(testConsole, Mock.Of<ILogger>(), handler.Object);

            var result = await command.ExecuteAsync(
                CreateContext("frameworks"),
                new BumpFrameworksSettings(),
                CancellationToken.None);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBe(0),
                () => testConsole.Output.ShouldContain("No target frameworks were bumped."));
        }

        [Fact]
        public async Task With_Handler_Throws_Returns_1()
        {
            var handler = new Mock<IBumpFrameworksHandler>();
            handler
                .Setup(h => h.HandleAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyCollection<string>>()))
                .ThrowsAsync(new DotBumpException("boom"));

            using var testConsole = new TestConsole();
            var command = new BumpFrameworksCommand(testConsole, Mock.Of<ILogger>(), handler.Object);

            var result = await command.ExecuteAsync(
                CreateContext("frameworks"),
                new BumpFrameworksSettings(),
                CancellationToken.None);

            result.ShouldBe(1);
        }

        [Fact]
        public async Task With_Unexpected_Command_Name_Throws_DotBumpException()
        {
            using var testConsole = new TestConsole();
            var command = new BumpFrameworksCommand(
                testConsole,
                Mock.Of<ILogger>(),
                Mock.Of<IBumpFrameworksHandler>());

            await Should.ThrowAsync<DotBumpException>(() => command.ExecuteAsync(
                CreateContext("packages"),
                new BumpFrameworksSettings(),
                CancellationToken.None));
        }

        [Fact]
        public async Task With_Valid_GlobalJson_And_Project_Updates_TargetFramework()
        {
            ResetTempDirectory();
            var globalJsonPath = Path.Combine(TempDirectory.AbsolutePath, "global.json");
            var projectPath = Path.Combine(TempDirectory.AbsolutePath, "App.csproj");
            var reportPath = Path.Combine(TempDirectory.AbsolutePath, "frameworks.md");

            await File.WriteAllTextAsync(
                globalJsonPath,
                JsonSerializer.Serialize(new GlobalJson(new Sdk("10.0.100", "latestFeature"))));
            await File.WriteAllTextAsync(
                projectPath,
                "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <TargetFramework>net8.0</TargetFramework>\n  </PropertyGroup>\n</Project>\n");

            var logger = Mock.Of<ILogger>();
            var handler = new BumpFrameworksHandler(
                new FrameworkFileService(logger),
                new SdkFileService(logger),
                logger);

            using var testConsole = new TestConsole();
            var command = new BumpFrameworksCommand(testConsole, logger, handler);

            var result = await command.ExecuteAsync(
                CreateContext("frameworks"),
                new BumpFrameworksSettings
                {
                    GlobalJsonPath = globalJsonPath,
                    RepositoryPath = TempDirectory.AbsolutePath,
                    Output = reportPath,
                },
                CancellationToken.None);

            var project = await File.ReadAllTextAsync(projectPath);
            var markdown = await File.ReadAllTextAsync(reportPath);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBe(0),
                () => project.ShouldContain("<TargetFramework>net10.0</TargetFramework>"),
                () => markdown.ShouldContain("## Frameworks"),
                () => markdown.ShouldContain("App.csproj: net8.0 → net10.0"));
        }

        [Fact]
        public async Task With_Comment_Split_TargetFramework_Leaves_File_Unchanged()
        {
            ResetTempDirectory();
            var globalJsonPath = Path.Combine(TempDirectory.AbsolutePath, "global.json");
            var projectPath = Path.Combine(TempDirectory.AbsolutePath, "App.csproj");
            var reportPath = Path.Combine(TempDirectory.AbsolutePath, "frameworks.md");
            const string projectContent =
                "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
                "  <PropertyGroup>\n" +
                "    <TargetFramework>net8<!-- keep -->.0</TargetFramework>\n" +
                "  </PropertyGroup>\n" +
                "</Project>\n";

            await File.WriteAllTextAsync(
                globalJsonPath,
                JsonSerializer.Serialize(new GlobalJson(new Sdk("10.0.100", "latestFeature"))));
            await File.WriteAllTextAsync(projectPath, projectContent);

            var logger = Mock.Of<ILogger>();
            var handler = new BumpFrameworksHandler(
                new FrameworkFileService(logger),
                new SdkFileService(logger),
                logger);

            using var testConsole = new TestConsole();
            var command = new BumpFrameworksCommand(testConsole, logger, handler);

            var result = await command.ExecuteAsync(
                CreateContext("frameworks"),
                new BumpFrameworksSettings
                {
                    GlobalJsonPath = globalJsonPath,
                    RepositoryPath = TempDirectory.AbsolutePath,
                    Output = reportPath,
                },
                CancellationToken.None);

            var project = await File.ReadAllTextAsync(projectPath);
            var markdown = await File.ReadAllTextAsync(reportPath);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBe(0),
                () => project.ShouldBe(projectContent),
                () => markdown.ShouldContain("could not be located"));
        }

        private static LocalDirectory TempDirectory => new("./temp/frameworks-command");

        private static void ResetTempDirectory()
        {
            TempDirectory.EnsureDirectoryDeleted();
            TempDirectory.EnsureDirectoryCreated();
        }

        private static FrameworkManifest CreateChangedManifest()
        {
            var manifest = new FrameworkManifest();
            var entry = new FrameworkEntry("App.csproj", "/repo/App.csproj", "net8.0", string.Empty, 0, 6);
            manifest.Add(entry);
            manifest.SetFramework(entry, "net10.0");
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
}
