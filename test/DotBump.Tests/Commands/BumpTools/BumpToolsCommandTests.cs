// Copyright © 2025 Roby Van Damme.

using System.Text.Json;
using DotBump.Commands;
using DotBump.Commands.BumpTools;
using DotBump.Commands.BumpTools.DataModel.LocalTools;
using DotBump.Tests.TestHelpers;
using Moq;
using Serilog;
using Shouldly;
using Spectre.Console.Cli;
using Spectre.Console.Testing;

namespace DotBump.Tests.Commands.BumpTools;

public class BumpToolsCommandTests
{
    public class ExecuteAsync
    {
        private static readonly JsonSerializerOptions s_serializerOptions = new()
        {
            WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        [Fact]
        public async Task No_Tools_Manifest_Returns_1_And_FileNotFoundException()
        {
            var directory = new LocalDirectory("./.config");
            directory.EnsureFileDeleted("dotnet-tools.json");

            var loggerMock = new Mock<ILogger>().Object;
            using var testConsole = new TestConsole();
            var fileService = new ToolFileService(loggerMock);
            var clientFactory = new NuGetClientFactory(loggerMock);
            var releaseService = new NuGetReleaseFinder(loggerMock);
            var validator = new NuGetConfigValidator(loggerMock);
            var handler = new BumpToolsHandler(fileService, clientFactory, releaseService, validator, loggerMock);

            var command = new BumpToolsCommand(testConsole, loggerMock, handler);
            var arguments = new[] { "bump", "tools" };
            var remainingArguments = new Mock<IRemainingArguments>();
            var context = new CommandContext(arguments, remainingArguments.Object, "tools", null);
            var result = await command.ExecuteAsync(context, new BumpToolsSettings());
            result.ShouldBe(1);
            testConsole.Output.ShouldContain("FileNotFoundException: Tool manifest file not found");
        }

        [Fact]
        public async Task Updates_Tools_To_Latest_Minor_Or_Patch_Version_And_Returns_0()
        {
            ConfigureToolsManifest();

            var loggerMock = new Mock<ILogger>().Object;
            using var testConsole = new TestConsole();
            var fileService = new ToolFileService(loggerMock);
            var clientFactory = new NuGetClientFactory(loggerMock);
            var releaseService = new NuGetReleaseFinder(loggerMock);
            var validator = new NuGetConfigValidator(loggerMock);
            var handler = new BumpToolsHandler(fileService, clientFactory, releaseService, validator, loggerMock);

            var command = new BumpToolsCommand(testConsole, loggerMock, handler);
            var arguments = new[] { "bump", "tools" };
            var remainingArguments = new Mock<IRemainingArguments>();
            var context = new CommandContext(arguments, remainingArguments.Object, "tools", null);
            var result = await command.ExecuteAsync(context, new BumpToolsSettings());
            result.ShouldBe(0);

            var updatedManifest = fileService.GetToolManifest();
            updatedManifest.Tools.First(o => o.Key.Equals("dotnet-sonarscanner"))
                .Value.Version.ShouldBe("10.4.1");
            updatedManifest.Tools.First(o => o.Key.Equals("amazon.lambda.tools"))
                .Value.Version.ShouldBe("3.3.1");
            updatedManifest.Tools.First(o => o.Key.Equals("dotnet-reportgenerator-globaltool"))
                .Value.Version.ShouldBe("4.8.13"); // 4.9.0 is unlisted.
        }

        [Fact]
        public async Task Updates_Tools_To_Latest_Patch_Version_And_Returns_0()
        {
            ConfigureToolsManifest();

            var loggerMock = new Mock<ILogger>().Object;
            using var testConsole = new TestConsole();
            var fileService = new ToolFileService(loggerMock);
            var clientFactory = new NuGetClientFactory(loggerMock);
            var releaseService = new NuGetReleaseFinder(loggerMock);
            var validator = new NuGetConfigValidator(loggerMock);
            var handler = new BumpToolsHandler(fileService, clientFactory, releaseService, validator, loggerMock);

            var command = new BumpToolsCommand(testConsole, loggerMock, handler);
            var arguments = new[] { "bump", "tools" };
            var remainingArguments = new Mock<IRemainingArguments>();
            var context = new CommandContext(arguments, remainingArguments.Object, "tools", null);
            var result = await command.ExecuteAsync(context, new BumpToolsSettings() { BumpType = BumpType.Patch });
            result.ShouldBe(0);

            var updatedManifest = fileService.GetToolManifest();
            updatedManifest.Tools.First(o => o.Key.Equals("dotnet-sonarscanner"))
                .Value.Version.ShouldBe("10.1.2");
            updatedManifest.Tools.First(o => o.Key.Equals("amazon.lambda.tools"))
                .Value.Version.ShouldBe("3.2.3");
            updatedManifest.Tools.First(o => o.Key.Equals("dotnet-reportgenerator-globaltool"))
                .Value.Version.ShouldBe("4.6.7");
        }

        [Fact]
        public async Task With_Output_Parameter_Writes_Report_To_File()
        {
            var resultFile = new FileInfo("bump-tools-report.json");
            resultFile.Delete();

            ConfigureToolsManifest();

            var loggerMock = new Mock<ILogger>().Object;
            using var testConsole = new TestConsole();
            var fileService = new ToolFileService(loggerMock);
            var clientFactory = new NuGetClientFactory(loggerMock);
            var releaseService = new NuGetReleaseFinder(loggerMock);
            var validator = new NuGetConfigValidator(loggerMock);
            var handler = new BumpToolsHandler(fileService, clientFactory, releaseService, validator, loggerMock);

            var command = new BumpToolsCommand(testConsole, loggerMock, handler);
            var arguments = new[] { "bump", "tools" };
            var remainingArguments = new Mock<IRemainingArguments>();
            var context = new CommandContext(arguments, remainingArguments.Object, "tools", null);
            var result = await command.ExecuteAsync(
                context,
                new BumpToolsSettings() { BumpType = BumpType.Patch, Output = "bump-tools-report.json" });
            result.ShouldBe(0);

            resultFile.Refresh();
            resultFile.Exists.ShouldBeTrue();
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
            var directory = new LocalDirectory("./.config");
            directory.EnsureFileDeleted("dotnet-tools.json");
            directory.EnsureFileCreated(
                "dotnet-tools.json",
                JsonSerializer.Serialize(manifest, s_serializerOptions));
        }
    }
}
