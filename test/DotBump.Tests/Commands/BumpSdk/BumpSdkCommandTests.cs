// Copyright © 2025 Roby Van Damme.

using System.Text.Json;
using DotBump.Commands.BumpSdk;
using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Tests.Commands.BumpSdk.Fakes;
using DotBump.Tests.TestHelpers;
using Moq;
using Serilog;
using Shouldly;
using Spectre.Console.Cli;
using Spectre.Console.Testing;

namespace DotBump.Tests.Commands.BumpSdk;

public class BumpSdkCommandTests
{
    public class ExecuteAsync
    {
        [Fact]
        public async Task Sdk_Version_Updated_Returns_0()
        {
            var json = new GlobalJson(new Sdk("8.0.405", "disable"));
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

            testConsole.Output.ShouldContain("Updated = True");
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
        public async Task Handles_Security_True_Correctly()
        {
            // The SDK version in the release index (8.0.406) is not a security update.
            var json = new GlobalJson(new Sdk("8.0.405", "disable"));
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
                new BumpSdkSettings { GlobalJsonPath = "./temp/global.json", Security = true });
            result.ShouldBe(0);

            var globalJson = sdkFileService.GetCurrentSdkVersionFromFile("./temp/global.json");
            globalJson.Version.ShouldBe("8.0.405");

            testConsole.Output.ShouldContain("Updated = False");
        }

        [Fact]
        public async Task Handles_Security_False_Correctly()
        {
            // The SDK version in the release index (8.0.406) is not a security update.
            var json = new GlobalJson(new Sdk("8.0.405", "disable"));
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
                new BumpSdkSettings { GlobalJsonPath = "./temp/global.json", Security = false });
            result.ShouldBe(0);

            var globalJson = sdkFileService.GetCurrentSdkVersionFromFile("./temp/global.json");
            globalJson.Version.ShouldBe("8.0.406");

            testConsole.Output.ShouldContain("Updated = True");
        }
    }
}
