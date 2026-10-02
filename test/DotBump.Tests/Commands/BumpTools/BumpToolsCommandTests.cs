// Copyright © Roby Van Damme.

using System.Text.Json;
using DotBump.Commands;
using DotBump.Commands.BumpTools;
using DotBump.Commands.BumpTools.DataModel.LocalTools;
using DotBump.Commands.BumpTools.Interfaces;
using DotBump.NuGet;
using DotBump.Reports;
using DotBump.Tests.TestHelpers;
using Moq;
using Serilog;
using Shouldly;
using Spectre.Console.Cli;
using Spectre.Console.Testing;

namespace DotBump.Tests.Commands.BumpTools;

public class BumpToolsCommandTests
{
    public class ExecuteForTestAsync
    {
        private static readonly string s_defaultNugetConfig = "nuget.config";

        private static readonly LocalDirectory s_tempDirectory = new("./temp");

        private static readonly string s_toolManifestPath = Path.Combine(s_tempDirectory.AbsolutePath, "dotnet-tools.json");

        private static readonly JsonSerializerOptions s_serializerOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        [Fact]
        public async Task With_Missing_Tools_Manifest_Returns_1_And_FileNotFoundException()
        {
            var missingManifestPath = Path.Combine(s_tempDirectory.AbsolutePath, "missing-dotnet-tools.json");
            s_tempDirectory.EnsureFileDeleted("missing-dotnet-tools.json");

            var loggerMock = new Mock<ILogger>().Object;
            using var testConsole = new TestConsole();
            var fileService = new ToolFileService(loggerMock);
            var nugetConfigFileService = new NuGetConfigFileService(loggerMock);
            var clientFactory = new NuGetClientFactory(loggerMock);
            var releaseService = new NuGetReleaseFinder(loggerMock);
            var validator = new NuGetConfigValidator(loggerMock);
            var handler = new BumpToolsHandler(fileService, nugetConfigFileService, new PackageVersionResolver(clientFactory, releaseService, loggerMock), validator, loggerMock);

            var command = new BumpToolsCommand(testConsole, loggerMock, handler);
            var arguments = new[] { "bump", "tools" };
            var remainingArguments = new Mock<IRemainingArguments>();
            var context = new CommandContext(arguments, remainingArguments.Object, "tools", null);
            var result = await command.ExecuteForTestAsync(
                context,
                new BumpToolsSettings() { ToolManifestPath = missingManifestPath },
                CancellationToken.None);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBe(1),
                () => testConsole.Output.ShouldContain("FileNotFoundException: Tool manifest file not found"));
        }

        [Fact]
        public async Task With_Minor_Bump_Returns_0()
        {
            ConfigureToolsManifest();

            var loggerMock = new Mock<ILogger>().Object;
            using var testConsole = new TestConsole();
            var fileService = new ToolFileService(loggerMock);
            var nugetConfigFileService = new NuGetConfigFileService(loggerMock);
            var clientFactory = new NuGetClientFactory(loggerMock);
            var releaseService = new NuGetReleaseFinder(loggerMock);
            var validator = new NuGetConfigValidator(loggerMock);
            var handler = new BumpToolsHandler(fileService, nugetConfigFileService, new PackageVersionResolver(clientFactory, releaseService, loggerMock), validator, loggerMock);

            var command = new BumpToolsCommand(testConsole, loggerMock, handler);
            var arguments = new[] { "bump", "tools" };
            var remainingArguments = new Mock<IRemainingArguments>();
            var context = new CommandContext(arguments, remainingArguments.Object, "tools", null);
            var result = await command.ExecuteForTestAsync(
                context,
                new BumpToolsSettings() { ToolManifestPath = s_toolManifestPath },
                CancellationToken.None);

            var updatedManifest = fileService.GetToolsManifest(s_toolManifestPath);
            result.ShouldSatisfyAllConditions(
                () => result.ShouldBe(0),
                () => updatedManifest.Tools.First(o => o.Key.Equals("dotnet-sonarscanner")).Value.Version.ShouldBe("10.4.1"),
                () => updatedManifest.Tools.First(o => o.Key.Equals("amazon.lambda.tools")).Value.Version.ShouldBe("3.3.1"),
                () => updatedManifest.Tools.First(o => o.Key.Equals("dotnet-reportgenerator-globaltool")).Value.Version.ShouldBe("4.8.13"));
        }

        [Fact]
        public async Task With_Patch_Bump_Returns_0()
        {
            ConfigureToolsManifest();

            var loggerMock = new Mock<ILogger>().Object;
            using var testConsole = new TestConsole();
            var fileService = new ToolFileService(loggerMock);
            var nugetConfigFileService = new NuGetConfigFileService(loggerMock);
            var clientFactory = new NuGetClientFactory(loggerMock);
            var releaseService = new NuGetReleaseFinder(loggerMock);
            var validator = new NuGetConfigValidator(loggerMock);
            var handler = new BumpToolsHandler(fileService, nugetConfigFileService, new PackageVersionResolver(clientFactory, releaseService, loggerMock), validator, loggerMock);

            var command = new BumpToolsCommand(testConsole, loggerMock, handler);
            var arguments = new[] { "bump", "tools" };
            var remainingArguments = new Mock<IRemainingArguments>();
            var context = new CommandContext(arguments, remainingArguments.Object, "tools", null);
            var result = await command.ExecuteForTestAsync(
                context,
                new BumpToolsSettings() { BumpType = BumpType.Patch, ToolManifestPath = s_toolManifestPath },
                CancellationToken.None);

            var updatedManifest = fileService.GetToolsManifest(s_toolManifestPath);
            result.ShouldSatisfyAllConditions(
                () => result.ShouldBe(0),
                () => updatedManifest.Tools.First(o => o.Key.Equals("dotnet-sonarscanner")).Value.Version.ShouldBe("10.1.2"),
                () => updatedManifest.Tools.First(o => o.Key.Equals("amazon.lambda.tools")).Value.Version.ShouldBe("3.2.3"),
                () => updatedManifest.Tools.First(o => o.Key.Equals("dotnet-reportgenerator-globaltool")).Value.Version.ShouldBe("4.6.7"));
        }

        [Fact]
        public async Task With_Output_Parameter_Returns_0()
        {
            var resultFile = new FileInfo("bump-tools-report.json");
            resultFile.Delete();

            ConfigureToolsManifest();

            try
            {
                var loggerMock = new Mock<ILogger>().Object;
                using var testConsole = new TestConsole();
                var fileService = new ToolFileService(loggerMock);
                var nugetConfigFileService = new NuGetConfigFileService(loggerMock);
                var clientFactory = new NuGetClientFactory(loggerMock);
                var releaseService = new NuGetReleaseFinder(loggerMock);
                var validator = new NuGetConfigValidator(loggerMock);
                var handler = new BumpToolsHandler(fileService, nugetConfigFileService, new PackageVersionResolver(clientFactory, releaseService, loggerMock), validator, loggerMock);

                var command = new BumpToolsCommand(testConsole, loggerMock, handler);
                var arguments = new[] { "bump", "tools" };
                var remainingArguments = new Mock<IRemainingArguments>();
                var context = new CommandContext(arguments, remainingArguments.Object, "tools", null);
                var result = await command.ExecuteForTestAsync(
                    context,
                    new BumpToolsSettings() { BumpType = BumpType.Patch, Output = "bump-tools-report.json", ToolManifestPath = s_toolManifestPath },
                    CancellationToken.None);

                resultFile.Refresh();
                result.ShouldSatisfyAllConditions(
                    () => result.ShouldBe(0),
                    () => resultFile.Exists.ShouldBeTrue());
            }
            finally
            {
                if (resultFile.Exists)
                {
                    resultFile.Delete();
                }
            }
        }

        [Fact]
        public async Task With_Private_Feed_And_Patch_Bump_Returns_0()
        {
            // Arrange
            var xmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
                <configuration>
                    <packageSources>
                        <add key=""myorg"" value=""https://nuget.pkg.github.com/robyvandamme/index.json"" protocolVersion=""3"" />
                    </packageSources>
                    <packageSourceCredentials>
                        <myorg>
                            <add key=""Username"" value=""%PRIVATE_GITHUB_FEED_USER%"" />
                            <add key=""ClearTextPassword"" value=""%PRIVATE_GITHUB_FEED_PASSWORD%"" />
                        </myorg>
                    </packageSourceCredentials>
                </configuration>";

            var tempFile = CreateTempConfigFile(xmlContent);
            ConfigurePrivateToolsManifest();

            try
            {
                var loggerMock = new Mock<ILogger>().Object;
                using var testConsole = new TestConsole();
                var fileService = new ToolFileService(loggerMock);
                var nugetConfigFileService = new NuGetConfigFileService(loggerMock);
                var clientFactory = new NuGetClientFactory(loggerMock);
                var releaseService = new NuGetReleaseFinder(loggerMock);
                var validator = new NuGetConfigValidator(loggerMock);
                var handler = new BumpToolsHandler(
                    fileService,
                    nugetConfigFileService,
                    new PackageVersionResolver(clientFactory, releaseService, loggerMock),
                    validator,
                    loggerMock);

                var command = new BumpToolsCommand(testConsole, loggerMock, handler);
                var arguments = new[] { "bump", "tools" };
                var remainingArguments = new Mock<IRemainingArguments>();
                var context = new CommandContext(arguments, remainingArguments.Object, "tools", null);
                var result = await command.ExecuteForTestAsync(
                    context,
                    new BumpToolsSettings() { BumpType = BumpType.Patch, ToolManifestPath = s_toolManifestPath },
                    CancellationToken.None);

                var updatedManifest = fileService.GetToolsManifest(s_toolManifestPath);
                result.ShouldSatisfyAllConditions(
                    () => result.ShouldBe(0),
                    () => updatedManifest.Tools.First(o => o.Key.Equals("dotbump")).Value.Version.ShouldBe("0.1.1-beta.8"));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task With_Markup_Unsafe_Config_Path_Returns_0_And_Writes_Literal_Path()
        {
            using var testConsole = new TestConsole().Width(500);
            var handler = new Mock<IBumpToolsHandler>();
            handler
                .Setup(h => h.HandleAsync(It.IsAny<BumpType>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new BumpReport(
                    new ToolsManifest
                    {
                        Version = 1,
                        IsRoot = true,
                        Tools = new Dictionary<string, ToolManifestEntry>(),
                    },
                    BumpType.Minor));
            var command = new BumpToolsCommand(testConsole, Mock.Of<ILogger>(), handler.Object);
            var context = new CommandContext(["bump", "tools"], new Mock<IRemainingArguments>().Object, "tools", null);

            var result = await command.ExecuteForTestAsync(
                context,
                new BumpToolsSettings { NuGetConfigPath = "./[draft]/nuget.config" },
                CancellationToken.None);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBe(0),
                () => testConsole.Output.ShouldContain("[draft]"));
        }

        [Fact]
        public async Task With_Markup_Unsafe_Manifest_Path_Returns_0_And_Writes_Literal_Path()
        {
            using var testConsole = new TestConsole().Width(500);
            var handler = new Mock<IBumpToolsHandler>();
            handler
                .Setup(h => h.HandleAsync(It.IsAny<BumpType>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new BumpReport(
                    new ToolsManifest
                    {
                        Version = 1,
                        IsRoot = true,
                        Tools = new Dictionary<string, ToolManifestEntry>(),
                    },
                    BumpType.Minor));
            var command = new BumpToolsCommand(testConsole, Mock.Of<ILogger>(), handler.Object);
            var context = new CommandContext(["bump", "tools"], new Mock<IRemainingArguments>().Object, "tools", null);

            var result = await command.ExecuteForTestAsync(
                context,
                new BumpToolsSettings { ToolManifestPath = "./[draft]/dotnet-tools.json" },
                CancellationToken.None);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBe(0),
                () => testConsole.Output.ShouldContain("[draft]"));
        }

        private static void ConfigurePrivateToolsManifest()
        {
            var tools = new Dictionary<string, ToolManifestEntry>();

            // should bump patch to 0.1.1-beta.8
            tools.Add(
                "dotbump",
                new ToolManifestEntry { Version = "0.1.1-beta.7", RollForward = false, Commands = ["dotbump"], });

            var manifest = new ToolsManifest() { Version = 1, IsRoot = true, Tools = tools };
            s_tempDirectory.EnsureFileDeleted("dotnet-tools.json");
            s_tempDirectory.EnsureFileCreated("dotnet-tools.json", JsonSerializer.Serialize(manifest, s_serializerOptions));
        }

        private static void ConfigureToolsManifest()
        {
            var tools = new Dictionary<string, ToolManifestEntry>();

            // latest minor is 10.4.1. Should be stable since version 11 has been released.
            // latest patch should be 10.1.2
            tools.Add(
                "dotnet-sonarscanner",
                new ToolManifestEntry { Version = "10.1.0", RollForward = false, Commands = ["dotnet-sonarscanner"], });

            // this is an old version. Minor should bump to 3.3.1. Patch should bump to 3.2.3
            tools.Add(
                "amazon.lambda.tools",
                new ToolManifestEntry { Version = "3.2.0", RollForward = false, Commands = ["dotnet-lambda"], });

            // this one contains a version that fails the semantic version match test
            // using an old version 4.6.1. Minor should bump to 4.8.13. Patch should bump to 4.6.7.
            // and .... there appears to be a version 4.9.0.... which does not show up on the NuGet page...
            // because it has been unlisted.... So this is a good one to add to the release finder tests as well.
            tools.Add(
                "dotnet-reportgenerator-globaltool",
                new ToolManifestEntry { Version = "4.6.1", RollForward = false, Commands = ["reportgenerator"], });

            var manifest = new ToolsManifest() { Version = 1, IsRoot = true, Tools = tools };
            s_tempDirectory.EnsureFileDeleted("dotnet-tools.json");
            s_tempDirectory.EnsureFileCreated("dotnet-tools.json", JsonSerializer.Serialize(manifest, s_serializerOptions));
        }

        private static string CreateTempConfigFile(string content)
        {
            var localDirectory = new LocalDirectory(Environment.CurrentDirectory);
            var filename = s_defaultNugetConfig;
            localDirectory.EnsureFileDeleted(filename);
            localDirectory.EnsureFileCreated(filename, content);
            return "nuget.config";
        }
    }
}
