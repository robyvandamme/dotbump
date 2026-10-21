// Copyright © Roby Van Damme.

using DotBump.Commands.BumpFrameworks;
using DotBump.Commands.BumpFrameworks.DataModel;
using DotBump.Commands.BumpFrameworks.Interfaces;
using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Commands.BumpSdk.Interfaces;
using DotBump.Common;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.Commands.BumpFrameworks;

public class BumpFrameworksHandlerTests
{
    public class HandleAsync
    {
        [Fact]
        public async Task With_Valid_Sdk_Sets_Target_Framework_And_Saves()
        {
            var manifest = CreateManifest("net8.0", string.Empty);
            var (handler, fileService, _) = CreateHandler(manifest, "10.0.100");

            var report = await handler.HandleAsync("./repo", "./global.json");

            report.HasChanges.ShouldBeTrue();
            manifest.Frameworks.Single().Framework.ShouldBe("net10.0");
            fileService.Verify(s => s.SaveFrameworkManifest(manifest), Times.Once);
        }

        [Fact]
        public async Task With_Suffix_Preserves_Suffix()
        {
            var manifest = CreateManifest("net8.0-windows", "-windows");
            var (handler, _, _) = CreateHandler(manifest, "11.0.100");

            await handler.HandleAsync("./repo", "./global.json");

            manifest.Frameworks.Single().Framework.ShouldBe("net11.0-windows");
        }

        [Fact]
        public async Task With_Project_Higher_Than_Sdk_Aligns_Down()
        {
            var manifest = CreateManifest("net11.0", string.Empty);
            var (handler, _, _) = CreateHandler(manifest, "10.0.100");

            await handler.HandleAsync("./repo", "./global.json");

            manifest.Frameworks.Single().Framework.ShouldBe("net10.0");
        }

        [Fact]
        public async Task With_No_Changes_Does_Not_Save()
        {
            var manifest = CreateManifest("net10.0", string.Empty);
            var (handler, fileService, _) = CreateHandler(manifest, "10.0.100");

            var report = await handler.HandleAsync("./repo", "./global.json");

            report.HasChanges.ShouldBeFalse();
            fileService.Verify(s => s.SaveFrameworkManifest(It.IsAny<FrameworkManifest>()), Times.Never);
        }

        [Fact]
        public async Task With_Invalid_Sdk_Version_Throws_DotBumpException()
        {
            var manifest = CreateManifest("net8.0", string.Empty);
            var (handler, fileService, _) = CreateHandler(manifest, "not-a-version");

            await Should.ThrowAsync<DotBumpException>(() => handler.HandleAsync("./repo", "./global.json"));

            fileService.Verify(
                s => s.GetFrameworkManifest(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<string>>()),
                Times.Never);
        }

        [Fact]
        public async Task With_Excluded_Paths_Forwards_Them_To_The_File_Service()
        {
            var manifest = CreateManifest("net8.0", string.Empty);
            var (handler, fileService, _) = CreateHandler(manifest, "10.0.100");
            string[] excludedPaths = ["./excluded"];

            await handler.HandleAsync("./repo", "./global.json", excludedPaths);

            fileService.Verify(
                s => s.GetFrameworkManifest(
                    "./repo",
                    It.Is<IReadOnlyCollection<string>>(paths => paths.Contains("./excluded"))),
                Times.Once);
        }

        [Fact]
        public async Task With_Read_Warnings_Reports_Warnings()
        {
            var manifest = CreateManifest("net8.0", string.Empty);
            manifest.AddWarning("Skipping 'Library.csproj' because TargetFrameworks is not supported yet.");
            var (handler, _, _) = CreateHandler(manifest, "10.0.100");

            var report = await handler.HandleAsync("./repo", "./global.json");

            report.Warnings.ShouldContain(
                "Skipping 'Library.csproj' because TargetFrameworks is not supported yet.");
        }

        private static FrameworkManifest CreateManifest(string framework, string suffix)
        {
            var manifest = new FrameworkManifest();
            manifest.Add(new FrameworkEntry("App.csproj", "/repo/App.csproj", framework, suffix, 0, framework.Length));
            return manifest;
        }

        private static (BumpFrameworksHandler Handler, Mock<IFrameworkFileService> FileService, Mock<ISdkFileService> SdkFileService)
            CreateHandler(FrameworkManifest manifest, string sdkVersion)
        {
            var fileService = new Mock<IFrameworkFileService>();
            fileService
                .Setup(s => s.GetFrameworkManifest(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<string>>()))
                .Returns(manifest);

            var sdkFileService = new Mock<ISdkFileService>();
            sdkFileService
                .Setup(s => s.GetCurrentSdkVersionFromFile(It.IsAny<string>()))
                .Returns(new Sdk(sdkVersion, "latestFeature"));

            var handler = new BumpFrameworksHandler(fileService.Object, sdkFileService.Object, Mock.Of<ILogger>());
            return (handler, fileService, sdkFileService);
        }
    }
}
