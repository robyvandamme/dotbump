// Copyright © Roby Van Damme.

using DotBump.Commands.BumpLanguage;
using DotBump.Commands.BumpLanguage.DataModel;
using DotBump.Commands.BumpLanguage.Interfaces;
using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Commands.BumpSdk.Interfaces;
using DotBump.Common;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.Commands.BumpLanguage;

public class BumpLanguageHandlerTests
{
    public class HandleAsync
    {
        [Theory]
        [InlineData("10.0.100", "14.0")]
        [InlineData("9.0.100", "13.0")]
        [InlineData("8.0.100", "12.0")]
        [InlineData("7.0.100", "11.0")]
        [InlineData("6.0.100", "10.0")]
        [InlineData("5.0.100", "9.0")]
        public async Task With_Sdk_Version_Sets_Language_Version_And_Saves(string sdkVersion, string expectedVersion)
        {
            var manifest = CreateManifest("8.0");
            var (handler, fileService, _) = CreateHandler(manifest, sdkVersion);

            var report = await handler.HandleAsync("./repo", "./global.json");

            report.HasChanges.ShouldBeTrue();
            manifest.Entries.Single().Version.ShouldBe(expectedVersion);
            fileService.Verify(s => s.SaveLanguageManifest(manifest), Times.Once);
        }

        [Theory]
        [InlineData("14.0")]
        [InlineData("15.0")]
        [InlineData("14")]
        public async Task With_Current_Version_Not_Lower_Does_Not_Change_It(string currentVersion)
        {
            var manifest = CreateManifest(currentVersion);
            var (handler, fileService, _) = CreateHandler(manifest, "10.0.100");

            var report = await handler.HandleAsync("./repo", "./global.json");

            report.HasChanges.ShouldBeFalse();
            manifest.Entries.Single().Version.ShouldBe(currentVersion);
            fileService.Verify(s => s.SaveLanguageManifest(It.IsAny<LanguageManifest>()), Times.Never);
        }

        [Fact]
        public async Task With_No_Changes_Does_Not_Save()
        {
            var manifest = CreateManifest("14.0");
            var (handler, fileService, _) = CreateHandler(manifest, "10.0.100");

            var report = await handler.HandleAsync("./repo", "./global.json");

            report.HasChanges.ShouldBeFalse();
            fileService.Verify(s => s.SaveLanguageManifest(It.IsAny<LanguageManifest>()), Times.Never);
        }

        [Fact]
        public async Task With_Invalid_Sdk_Version_Throws_DotBumpException()
        {
            var manifest = CreateManifest("12.0");
            var (handler, fileService, _) = CreateHandler(manifest, "not-a-version");

            await Should.ThrowAsync<DotBumpException>(() => handler.HandleAsync("./repo", "./global.json"));

            fileService.Verify(
                s => s.GetLanguageManifest(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<string>>()),
                Times.Never);
        }

        [Fact]
        public async Task With_Legacy_Sdk_Version_Throws_DotBumpException_Before_Scanning_Or_Saving()
        {
            var manifest = CreateManifest("12.0");
            var (handler, fileService, _) = CreateHandler(manifest, "3.1.426");

            await Should.ThrowAsync<DotBumpException>(() => handler.HandleAsync("./repo", "./global.json"));

            fileService.Verify(
                s => s.GetLanguageManifest(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<string>>()),
                Times.Never);
            fileService.Verify(s => s.SaveLanguageManifest(It.IsAny<LanguageManifest>()), Times.Never);
        }

        [Fact]
        public async Task With_Excluded_Paths_Forwards_Them_To_The_File_Service()
        {
            var manifest = CreateManifest("12.0");
            var (handler, fileService, _) = CreateHandler(manifest, "10.0.100");
            string[] excludedPaths = ["./excluded"];

            await handler.HandleAsync("./repo", "./global.json", excludedPaths);

            fileService.Verify(
                s => s.GetLanguageManifest(
                    "./repo",
                    It.Is<IReadOnlyCollection<string>>(paths => paths.Contains("./excluded"))),
                Times.Once);
        }

        [Fact]
        public async Task With_Read_Warnings_Reports_Warnings()
        {
            var manifest = CreateManifest("12.0");
            manifest.AddWarning("Skipping language version '12.0' in 'App.csproj' because its value could not be located.");
            var (handler, _, _) = CreateHandler(manifest, "10.0.100");

            var report = await handler.HandleAsync("./repo", "./global.json");

            report.Warnings.ShouldContain(
                "Skipping language version '12.0' in 'App.csproj' because its value could not be located.");
        }

        private static LanguageManifest CreateManifest(string version)
        {
            var manifest = new LanguageManifest();
            manifest.Add(new LanguageEntry("App.csproj", "/repo/App.csproj", version, 0, version.Length));
            return manifest;
        }

        private static (
            BumpLanguageHandler Handler,
            Mock<ILanguageFileService> FileService,
            Mock<ISdkFileService> SdkFileService)
            CreateHandler(LanguageManifest manifest, string sdkVersion)
        {
            var fileService = new Mock<ILanguageFileService>();
            fileService
                .Setup(s => s.GetLanguageManifest(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<string>>()))
                .Returns(manifest);

            var sdkFileService = new Mock<ISdkFileService>();
            sdkFileService
                .Setup(s => s.GetCurrentSdkVersionFromFile(It.IsAny<string>()))
                .Returns(new Sdk(sdkVersion, "latestFeature"));

            var handler = new BumpLanguageHandler(fileService.Object, sdkFileService.Object, Mock.Of<ILogger>());
            return (handler, fileService, sdkFileService);
        }
    }
}
