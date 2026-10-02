// Copyright © Roby Van Damme.

using System.ComponentModel.DataAnnotations;
using DotBump.Commands;
using DotBump.Commands.BumpTools;
using DotBump.Commands.BumpTools.DataModel.LocalTools;
using DotBump.Commands.BumpTools.Interfaces;
using DotBump.Common;
using DotBump.NuGet.DataModel.NuGetConfiguration;
using DotBump.NuGet.DataModel.PackageResolution;
using DotBump.NuGet.Interfaces;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.Commands.BumpTools;

public class BumpToolsHandlerTests
{
    public class HandleAsync
    {
        [Fact]
        public async Task With_Resolved_Version_Updates_Manifest_And_Saves()
        {
            var manifest = CreateManifest("mytool", "1.0.0");
            var fileService = CreateFileService(manifest);
            var resolver = new Mock<IPackageVersionResolver>();
            resolver
                .Setup(r => r.ResolveAsync(
                    It.IsAny<IReadOnlyCollection<PackageToBump>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<NuGetConfig>()))
                .ReturnsAsync(new Dictionary<string, SemanticVersion> { ["mytool"] = new("1.1.0") });
            var handler = CreateHandler(fileService, resolver);

            var report = await handler.HandleAsync(BumpType.Minor, "nuget.config");

            report.HasChanges.ShouldBeTrue();
            manifest.Tools["mytool"].Version.ShouldBe("1.1.0");
            fileService.Verify(s => s.SaveToolsManifest(manifest), Times.Once);
        }

        [Fact]
        public async Task With_No_Resolved_Version_Does_Not_Save()
        {
            var manifest = CreateManifest("mytool", "1.0.0");
            var fileService = CreateFileService(manifest);
            var resolver = new Mock<IPackageVersionResolver>();
            resolver
                .Setup(r => r.ResolveAsync(
                    It.IsAny<IReadOnlyCollection<PackageToBump>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<NuGetConfig>()))
                .ReturnsAsync(new Dictionary<string, SemanticVersion>());
            var handler = CreateHandler(fileService, resolver);

            var report = await handler.HandleAsync(BumpType.Minor, "nuget.config");

            report.HasChanges.ShouldBeFalse();
            fileService.Verify(s => s.SaveToolsManifest(It.IsAny<ToolsManifest>()), Times.Never);
        }

        [Fact]
        public async Task With_Validation_Errors_Reports_Errors_And_Does_Not_Resolve_Or_Save()
        {
            var manifest = CreateManifest("mytool", "1.0.0");
            var fileService = CreateFileService(manifest);
            var resolver = new Mock<IPackageVersionResolver>();
            var handler = CreateHandler(fileService, resolver, [new ValidationResult("bad config")]);

            var report = await handler.HandleAsync(BumpType.Minor, "nuget.config");

            report.Errors.ShouldContain("bad config");
            fileService.Verify(s => s.SaveToolsManifest(It.IsAny<ToolsManifest>()), Times.Never);
            resolver.Verify(
                r => r.ResolveAsync(
                    It.IsAny<IReadOnlyCollection<PackageToBump>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<NuGetConfig>()),
                Times.Never);
        }

        [Fact]
        public async Task With_Manifest_Resolves_Each_Tool_Using_Its_Current_Version()
        {
            var manifest = CreateManifest("mytool", "1.0.0");
            var fileService = CreateFileService(manifest);
            var resolver = new Mock<IPackageVersionResolver>();
            resolver
                .Setup(r => r.ResolveAsync(
                    It.IsAny<IReadOnlyCollection<PackageToBump>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<NuGetConfig>()))
                .ReturnsAsync(new Dictionary<string, SemanticVersion>());
            var handler = CreateHandler(fileService, resolver);

            await handler.HandleAsync(BumpType.Minor, "nuget.config");

            resolver.Verify(
                r => r.ResolveAsync(
                    It.Is<IReadOnlyCollection<PackageToBump>>(packages =>
                        packages.Count == 1
                        && packages.Single().PackageId == "mytool"
                        && packages.Single().CurrentVersion.Version == "1.0.0"),
                    BumpType.Minor,
                    It.IsAny<NuGetConfig>()),
                Times.Once);
        }

        private static ToolsManifest CreateManifest(string toolKey, string version)
        {
            return new ToolsManifest
            {
                Version = 1,
                IsRoot = true,
                Tools = new Dictionary<string, ToolManifestEntry>
                {
                    [toolKey] = new() { Version = version, Commands = [toolKey], RollForward = false, },
                },
            };
        }

        private static Mock<IToolFileService> CreateFileService(ToolsManifest manifest)
        {
            var fileService = new Mock<IToolFileService>();
            fileService.Setup(s => s.GetToolsManifest()).Returns(manifest);
            return fileService;
        }

        private static IBumpToolsHandler CreateHandler(
            Mock<IToolFileService> fileService,
            Mock<IPackageVersionResolver> resolver,
            List<ValidationResult>? validationErrors = null)
        {
            var loggerMock = new Mock<ILogger>().Object;

            var nugetConfigFileServiceMock = new Mock<INuGetConfigFileService>();
            nugetConfigFileServiceMock
                .Setup(s => s.GetNuGetConfiguration(It.IsAny<string>()))
                .Returns(new NuGetConfig());

            var validatorMock = new Mock<INuGetConfigValidator>();
            validatorMock
                .Setup(v => v.Validate(It.IsAny<NuGetConfig>()))
                .Returns(validationErrors ?? []);

            return new BumpToolsHandler(
                fileService.Object,
                nugetConfigFileServiceMock.Object,
                resolver.Object,
                validatorMock.Object,
                loggerMock);
        }
    }
}
