// Copyright © 2025 Roby Van Damme.

using System.Text.Json;
using DotBump.Commands.BumpSdk;
using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Commands.BumpTools;
using DotBump.Commands.BumpTools.DataModel.LocalTools;
using DotBump.Tests.Commands.BumpSdk.Fakes;
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

        public class No_Tools_Manifest
        {
            [Fact]
            public async Task Returns_1_And_FileNotFoundException()
            {
                var directory = new LocalDirectory("./.config");
                directory.EnsureFileDeleted("dotnet-tools.json");

                var loggerMock = new Mock<ILogger>().Object;
                using var testConsole = new TestConsole();
                var fileService = new ToolFileService(loggerMock);
                var clientFactory = new ClientFactory(loggerMock);
                var releaseService = new NuGetReleaseService(loggerMock);
                var handler = new BumpToolsHandler(fileService, clientFactory, releaseService, loggerMock);

                var command = new BumpToolsCommand(testConsole, loggerMock, handler);
                var arguments = new[] { "bump", "tools" };
                var remainingArguments = new Mock<IRemainingArguments>();
                var context = new CommandContext(arguments, remainingArguments.Object, "tools", null);
                var result = await command.ExecuteAsync(context, new BumpToolsSettings());
                result.ShouldBe(1);
                testConsole.Output.ShouldContain("FileNotFoundException: Tool manifest file not found");
            }
        }

        public class No_Config_Parameter_And_No_BumpType_Parameter
        {
            [Fact]
            public async Task Updates_Tools_To_Latest_Minor_Or_Patch_Version_And_Returns_0()
            {
                var tools = new Dictionary<string, ToolManifestEntry>();

                // latest minor is 10.4.1. Should be stable since version 11 has been released.
                // latest patch should be 10.1.2
                tools.Add(
                    "dotnet-sonarscanner",
                    new ToolManifestEntry
                    {
                        Version = "10.1.0", RollForward = false, Commands = ["dotnet-sonarscanner"],
                    });

                // this is an old version. Minor should bump to 3.3.1. Patch should bump to 3.2.3
                tools.Add(
                    "amazon.lambda.tools",
                    new ToolManifestEntry { Version = "3.2.0", RollForward = false, Commands = ["dotnet-lambda"], });

                var manifest = new ToolManifest() { Version = 1, IsRoot = true, Tools = tools };
                var directory = new LocalDirectory("./.config");
                directory.EnsureFileDeleted("dotnet-tools.json");
                directory.EnsureFileCreated(
                    "dotnet-tools.json",
                    JsonSerializer.Serialize(manifest, s_serializerOptions));

                var loggerMock = new Mock<ILogger>().Object;
                using var testConsole = new TestConsole();
                var fileService = new ToolFileService(loggerMock);
                var clientFactory = new ClientFactory(loggerMock);
                var releaseService = new NuGetReleaseService(loggerMock);
                var handler = new BumpToolsHandler(fileService, clientFactory, releaseService, loggerMock);

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
            }

            [Fact(Skip = "SDK Test - Review - Needed for Tools?")]
            public async Task Sdk_Version_Not_Updated_Returns_0()
            {
                var json = new GlobalJson(new Sdk("8.0.406", "disable"));
                var directory = new LocalDirectory("./temp");
                directory.EnsureFileDeleted("global.json");
                directory.EnsureFileCreated("global.json", JsonSerializer.Serialize(json));

                var loggerMock = new Mock<ILogger>().Object;
                using var testConsole = new TestConsole();
                var sdkFileService = new SdkFileService(loggerMock);
                var releaseService = new ReleaseFileService(); // has an sdk version 8.0.406
                var releaseFinder = new ReleaseFinder(loggerMock);
                var handler = new BumpSdkHandler(sdkFileService, releaseService, releaseFinder, loggerMock);

                var command = new BumpSdkCommand(testConsole, loggerMock, handler);
                var arguments = new[] { "bump", "sdk" };
                var remainingArguments = new Mock<IRemainingArguments>();
                var context = new CommandContext(arguments, remainingArguments.Object, "sdk", null);
                var result = await command.ExecuteAsync(
                    context,
                    new BumpSdkSettings { GlobalJsonPath = "./temp/global.json" });
                result.ShouldBe(0);

                var globalJson = sdkFileService.GetCurrentSdkVersionFromFile("./temp/global.json");
                globalJson.Version.ShouldBe("8.0.406");

                testConsole.Output.ShouldContain("Updated = False");
            }

            [Fact(Skip = "SDK Test - Adapt")]
            public async Task Invalid_FilePath_Returns_1()
            {
                var directory = new LocalDirectory("./temp");
                directory.EnsureFileDeleted("global.json");

                var loggerMock = new Mock<ILogger>().Object;
                using var testConsole = new TestConsole();
                var sdkFileService = new SdkFileService(loggerMock);
                var releaseService = new ReleaseFileService(); // has an sdk version 8.0.406
                var releaseFinder = new ReleaseFinder(loggerMock);
                var handler = new BumpSdkHandler(sdkFileService, releaseService, releaseFinder, loggerMock);

                var command = new BumpSdkCommand(testConsole, loggerMock, handler);
                var arguments = new[] { "bump", "sdk" };
                var remainingArguments = new Mock<IRemainingArguments>();
                var context = new CommandContext(arguments, remainingArguments.Object, "sdk", null);
                var result = await command.ExecuteAsync(
                    context,
                    new BumpSdkSettings { GlobalJsonPath = "./temp/global.json" });
                result.ShouldBe(1);

                testConsole.Output.ShouldContain("DotBumpException");
            }

            [Fact(Skip = "SDK Test - Adapt")]
            public async Task With_Output_Parameter_Writes_Result_To_File()
            {
                var json = new GlobalJson(new Sdk("8.0.405", "disable"));
                var directory = new LocalDirectory("./temp");
                directory.EnsureFileDeleted("global.json");
                directory.EnsureFileCreated("global.json", JsonSerializer.Serialize(json));

                var resultFile = new FileInfo("bump-sdk.result.json");
                resultFile.Delete();

                var loggerMock = new Mock<ILogger>().Object;
                using var testConsole = new TestConsole();
                var sdkFileService = new SdkFileService(loggerMock);
                var releaseService = new ReleaseFileService(); // has an sdk version 8.0.406
                var releaseFinder = new ReleaseFinder(loggerMock);
                var handler = new BumpSdkHandler(sdkFileService, releaseService, releaseFinder, loggerMock);

                var command = new BumpSdkCommand(testConsole, loggerMock, handler);
                var arguments = new[] { "bump", "sdk" };
                var remainingArguments = new Mock<IRemainingArguments>();
                var context = new CommandContext(arguments, remainingArguments.Object, "sdk", null);
                var result = await command.ExecuteAsync(
                    context,
                    new BumpSdkSettings { GlobalJsonPath = "./temp/global.json", Output = "bump-sdk.result.json" });
                result.ShouldBe(0);

                var globalJson = sdkFileService.GetCurrentSdkVersionFromFile("./temp/global.json");
                globalJson.Version.ShouldBe("8.0.406");

                testConsole.Output.ShouldContain("Updated = True");

                resultFile.Refresh();
                resultFile.Exists.ShouldBeTrue();
            }
        }
    }
}
