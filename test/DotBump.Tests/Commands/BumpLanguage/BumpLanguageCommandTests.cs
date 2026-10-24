// Copyright © Roby Van Damme.

using System.Text.Json;
using DotBump.Commands;
using DotBump.Commands.BumpLanguage;
using DotBump.Commands.BumpLanguage.DataModel;
using DotBump.Commands.BumpLanguage.Interfaces;
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

namespace DotBump.Tests.Commands.BumpLanguage;

public class BumpLanguageCommandTests
{
    public class ExecuteAsync
    {
        [Fact]
        public async Task With_Changes_Returns_0_And_Writes_Report()
        {
            ResetTempDirectory();
            var reportPath = Path.Combine(TempDirectory.AbsolutePath, "language.json");
            var manifest = CreateChangedManifest();
            var handler = new Mock<IBumpLanguageHandler>();
            handler
                .Setup(h => h.HandleAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyCollection<string>>()))
                .ReturnsAsync(new BumpReport(manifest, BumpType.Major));

            using var testConsole = new TestConsole();
            var command = new BumpLanguageCommand(testConsole, Mock.Of<ILogger>(), handler.Object);

            var result = await command.ExecuteAsync(
                CreateContext("language"),
                new BumpLanguageSettings { Output = reportPath },
                CancellationToken.None);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBe(0),
                () => testConsole.Output.ShouldContain("Language versions bumped"),
                () => File.Exists(reportPath).ShouldBeTrue());
        }

        [Fact]
        public async Task With_No_Changes_Returns_0_And_Reports_No_Changes()
        {
            var handler = new Mock<IBumpLanguageHandler>();
            handler
                .Setup(h => h.HandleAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyCollection<string>>()))
                .ReturnsAsync(new BumpReport(new LanguageManifest(), BumpType.Major));

            using var testConsole = new TestConsole();
            var command = new BumpLanguageCommand(testConsole, Mock.Of<ILogger>(), handler.Object);

            var result = await command.ExecuteAsync(
                CreateContext("language"),
                new BumpLanguageSettings(),
                CancellationToken.None);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBe(0),
                () => testConsole.Output.ShouldContain("No language versions were bumped."));
        }

        [Fact]
        public async Task With_Handler_Throws_Returns_1()
        {
            var handler = new Mock<IBumpLanguageHandler>();
            handler
                .Setup(h => h.HandleAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyCollection<string>>()))
                .ThrowsAsync(new DotBumpException("boom"));

            using var testConsole = new TestConsole();
            var command = new BumpLanguageCommand(testConsole, Mock.Of<ILogger>(), handler.Object);

            var result = await command.ExecuteAsync(
                CreateContext("language"),
                new BumpLanguageSettings(),
                CancellationToken.None);

            result.ShouldBe(1);
        }

        [Fact]
        public async Task With_Unexpected_Command_Name_Throws_DotBumpException()
        {
            using var testConsole = new TestConsole();
            var command = new BumpLanguageCommand(
                testConsole,
                Mock.Of<ILogger>(),
                Mock.Of<IBumpLanguageHandler>());

            await Should.ThrowAsync<DotBumpException>(() => command.ExecuteAsync(
                CreateContext("packages"),
                new BumpLanguageSettings(),
                CancellationToken.None));
        }

        [Fact]
        public async Task With_Valid_GlobalJson_And_Project_Updates_LanguageVersion()
        {
            ResetTempDirectory();
            var globalJsonPath = Path.Combine(TempDirectory.AbsolutePath, "global.json");
            var projectPath = Path.Combine(TempDirectory.AbsolutePath, "App.csproj");
            var reportPath = Path.Combine(TempDirectory.AbsolutePath, "language.md");

            await File.WriteAllTextAsync(
                globalJsonPath,
                JsonSerializer.Serialize(new GlobalJson(new Sdk("10.0.100", "latestFeature"))));
            await File.WriteAllTextAsync(
                projectPath,
                "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <LangVersion>12.0</LangVersion>\n  </PropertyGroup>\n</Project>\n");

            var logger = Mock.Of<ILogger>();
            var handler = new BumpLanguageHandler(
                new LanguageFileService(logger),
                new SdkFileService(logger),
                logger);

            using var testConsole = new TestConsole();
            var command = new BumpLanguageCommand(testConsole, logger, handler);

            var result = await command.ExecuteAsync(
                CreateContext("language"),
                new BumpLanguageSettings
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
                () => project.ShouldContain("<LangVersion>14.0</LangVersion>"),
                () => markdown.ShouldContain("## Language"),
                () => markdown.ShouldContain("App.csproj: 12.0 → 14.0"));
        }

        [Fact]
        public async Task With_Legacy_Sdk_Version_Leaves_Project_Unchanged()
        {
            ResetTempDirectory();
            var globalJsonPath = Path.Combine(TempDirectory.AbsolutePath, "global.json");
            var projectPath = Path.Combine(TempDirectory.AbsolutePath, "App.csproj");
            const string projectContent =
                "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
                "  <PropertyGroup>\n" +
                "    <LangVersion>12.0</LangVersion>\n" +
                "  </PropertyGroup>\n" +
                "</Project>\n";

            await File.WriteAllTextAsync(
                globalJsonPath,
                JsonSerializer.Serialize(new GlobalJson(new Sdk("3.1.426", "latestFeature"))));
            await File.WriteAllTextAsync(projectPath, projectContent);

            var logger = Mock.Of<ILogger>();
            var handler = new BumpLanguageHandler(
                new LanguageFileService(logger),
                new SdkFileService(logger),
                logger);

            using var testConsole = new TestConsole();
            var command = new BumpLanguageCommand(testConsole, logger, handler);

            var result = await command.ExecuteAsync(
                CreateContext("language"),
                new BumpLanguageSettings
                {
                    GlobalJsonPath = globalJsonPath,
                    RepositoryPath = TempDirectory.AbsolutePath,
                },
                CancellationToken.None);

            var project = await File.ReadAllTextAsync(projectPath);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBe(1),
                () => project.ShouldBe(projectContent),
                () => testConsole.Output.ShouldContain("requires .NET 5 or later"));
        }

        private static LocalDirectory TempDirectory => new("./temp/language-command");

        private static void ResetTempDirectory()
        {
            TempDirectory.EnsureDirectoryDeleted();
            TempDirectory.EnsureDirectoryCreated();
        }

        private static LanguageManifest CreateChangedManifest()
        {
            var manifest = new LanguageManifest();
            var entry = new LanguageEntry("App.csproj", "/repo/App.csproj", "12.0", 0, 4);
            manifest.Add(entry);
            manifest.SetVersion(entry, "14.0");
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
