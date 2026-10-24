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
            var manifest = CreateManifest(("mytool", "1.0.0"));
            var fileService = CreateFileService(manifest);
            var resolver = new Mock<IPackageVersionResolver>();
            resolver
                .Setup(r => r.ResolveAsync(
                    It.IsAny<IReadOnlyCollection<PackageToBump>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<NuGetConfig>(),
                    It.IsAny<bool>()))
                .ReturnsAsync(new Dictionary<string, SemanticVersion> { ["mytool"] = new("1.1.0") });
            var handler = CreateHandler(fileService, resolver);

            var report = await handler.HandleAsync(BumpType.Minor, "nuget.config", "dotnet-tools.json");

            report.HasChanges.ShouldBeTrue();
            manifest.Tools["mytool"].Version.ShouldBe("1.1.0");
            fileService.Verify(s => s.SaveToolsManifest(manifest, "dotnet-tools.json"), Times.Once);
        }

        [Fact]
        public async Task With_Major_Bump_Type_Forwards_Type_And_Applies_Version()
        {
            var manifest = CreateManifest(("mytool", "1.0.0"));
            var fileService = CreateFileService(manifest);
            var resolver = new Mock<IPackageVersionResolver>();
            resolver
                .Setup(r => r.ResolveAsync(
                    It.IsAny<IReadOnlyCollection<PackageToBump>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<NuGetConfig>(),
                    It.IsAny<bool>()))
                .ReturnsAsync(new Dictionary<string, SemanticVersion> { ["mytool"] = new("2.0.0") });
            var handler = CreateHandler(fileService, resolver);

            var report = await handler.HandleAsync(BumpType.Major, "nuget.config", "dotnet-tools.json");

            report.HasChanges.ShouldBeTrue();
            manifest.Tools["mytool"].Version.ShouldBe("2.0.0");
            resolver.Verify(
                r => r.ResolveAsync(
                    It.IsAny<IReadOnlyCollection<PackageToBump>>(),
                    BumpType.Major,
                    It.IsAny<NuGetConfig>(),
                    It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task With_PreRelease_True_Forwards_PreRelease_To_Resolver()
        {
            var manifest = CreateManifest(("mytool", "1.0.0"));
            var fileService = CreateFileService(manifest);
            var resolver = new Mock<IPackageVersionResolver>();
            resolver
                .Setup(r => r.ResolveAsync(
                    It.IsAny<IReadOnlyCollection<PackageToBump>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<NuGetConfig>(),
                    It.IsAny<bool>()))
                .ReturnsAsync(new Dictionary<string, SemanticVersion>());
            var handler = CreateHandler(fileService, resolver);

            await handler.HandleAsync(BumpType.Major, "nuget.config", "dotnet-tools.json", preRelease: true);

            resolver.Verify(
                r => r.ResolveAsync(
                    It.IsAny<IReadOnlyCollection<PackageToBump>>(),
                    BumpType.Major,
                    It.IsAny<NuGetConfig>(),
                    true),
                Times.Once);
        }

        [Fact]
        public async Task With_No_Resolved_Version_Does_Not_Save()
        {
            var manifest = CreateManifest(("mytool", "1.0.0"));
            var fileService = CreateFileService(manifest);
            var resolver = new Mock<IPackageVersionResolver>();
            resolver
                .Setup(r => r.ResolveAsync(
                    It.IsAny<IReadOnlyCollection<PackageToBump>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<NuGetConfig>(),
                    It.IsAny<bool>()))
                .ReturnsAsync(new Dictionary<string, SemanticVersion>());
            var handler = CreateHandler(fileService, resolver);

            var report = await handler.HandleAsync(BumpType.Minor, "nuget.config", "dotnet-tools.json");

            report.HasChanges.ShouldBeFalse();
            fileService.Verify(s => s.SaveToolsManifest(It.IsAny<ToolsManifest>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task With_Validation_Errors_Reports_Errors_And_Does_Not_Resolve_Or_Save()
        {
            var manifest = CreateManifest(("mytool", "1.0.0"));
            var fileService = CreateFileService(manifest);
            var resolver = new Mock<IPackageVersionResolver>();
            var handler = CreateHandler(fileService, resolver, [new ValidationResult("bad config")]);

            var report = await handler.HandleAsync(BumpType.Minor, "nuget.config", "dotnet-tools.json");

            report.Errors.ShouldContain("bad config");
            fileService.Verify(s => s.SaveToolsManifest(It.IsAny<ToolsManifest>(), It.IsAny<string>()), Times.Never);
            resolver.Verify(
                r => r.ResolveAsync(
                    It.IsAny<IReadOnlyCollection<PackageToBump>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<NuGetConfig>(),
                    It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task With_Manifest_Resolves_Each_Tool_Using_Its_Current_Version()
        {
            var manifest = CreateManifest(("mytool", "1.0.0"));
            var fileService = CreateFileService(manifest);
            var resolver = new Mock<IPackageVersionResolver>();
            resolver
                .Setup(r => r.ResolveAsync(
                    It.IsAny<IReadOnlyCollection<PackageToBump>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<NuGetConfig>(),
                    It.IsAny<bool>()))
                .ReturnsAsync(new Dictionary<string, SemanticVersion>());
            var handler = CreateHandler(fileService, resolver);

            await handler.HandleAsync(BumpType.Minor, "nuget.config", "dotnet-tools.json");

            resolver.Verify(
                r => r.ResolveAsync(
                    It.Is<IReadOnlyCollection<PackageToBump>>(packages =>
                        packages.Count == 1
                        && packages.Single().PackageId == "mytool"
                        && packages.Single().CurrentVersion.Version == "1.0.0"),
                    BumpType.Minor,
                    It.IsAny<NuGetConfig>(),
                    It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task With_Skipped_Tool_Does_Not_Resolve_And_Reports_Warning()
        {
            var manifest = CreateManifest(("mytool", "1.0.0"), ("othertool", "1.0.0"));
            var fileService = CreateFileService(manifest);
            var resolver = new Mock<IPackageVersionResolver>();
            resolver
                .Setup(r => r.ResolveAsync(
                    It.IsAny<IReadOnlyCollection<PackageToBump>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<NuGetConfig>(),
                    It.IsAny<bool>()))
                .ReturnsAsync(new Dictionary<string, SemanticVersion> { ["othertool"] = new("1.1.0") });
            var handler = CreateHandler(fileService, resolver);

            var report = await handler.HandleAsync(
                BumpType.Minor,
                "nuget.config",
                "dotnet-tools.json",
                skipTools: ["mytool"]);

            report.Warnings.ShouldContain(warning =>
                warning.Contains("mytool", StringComparison.Ordinal)
                && warning.Contains("--skip-tool", StringComparison.Ordinal));
            manifest.Tools.ShouldSatisfyAllConditions(
                () => manifest.Tools["mytool"].Version.ShouldBe("1.0.0"),
                () => manifest.Tools["othertool"].Version.ShouldBe("1.1.0"));
            resolver.Verify(
                r => r.ResolveAsync(
                    It.Is<IReadOnlyCollection<PackageToBump>>(packages =>
                        packages.Count == 1 && packages.Single().PackageId == "othertool"),
                    It.IsAny<BumpType>(),
                    It.IsAny<NuGetConfig>(),
                    It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task With_Skipped_Tool_Wildcard_Matches_Case_Insensitively()
        {
            var manifest = CreateManifest(("DotNet-Tool", "1.0.0"), ("Other.Tool", "1.0.0"));
            var fileService = CreateFileService(manifest);
            var resolver = new Mock<IPackageVersionResolver>();
            resolver
                .Setup(r => r.ResolveAsync(
                    It.IsAny<IReadOnlyCollection<PackageToBump>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<NuGetConfig>(),
                    It.IsAny<bool>()))
                .ReturnsAsync(new Dictionary<string, SemanticVersion> { ["Other.Tool"] = new("1.1.0") });
            var handler = CreateHandler(fileService, resolver);

            var report = await handler.HandleAsync(
                BumpType.Minor,
                "nuget.config",
                "dotnet-tools.json",
                skipTools: ["dotnet-*"]);

            report.Warnings.ShouldContain(warning =>
                warning.Contains("DotNet-Tool", StringComparison.Ordinal)
                && warning.Contains("dotnet-*", StringComparison.Ordinal));
            manifest.Tools["DotNet-Tool"].Version.ShouldBe("1.0.0");
            resolver.Verify(
                r => r.ResolveAsync(
                    It.Is<IReadOnlyCollection<PackageToBump>>(packages =>
                        packages.Count == 1 && packages.Single().PackageId == "Other.Tool"),
                    It.IsAny<BumpType>(),
                    It.IsAny<NuGetConfig>(),
                    It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task With_All_Tools_Skipped_Does_Not_Save_And_Reports_Warning()
        {
            var manifest = CreateManifest(("mytool", "1.0.0"));
            var fileService = CreateFileService(manifest);
            var resolver = new Mock<IPackageVersionResolver>();
            resolver
                .Setup(r => r.ResolveAsync(
                    It.IsAny<IReadOnlyCollection<PackageToBump>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<NuGetConfig>(),
                    It.IsAny<bool>()))
                .ReturnsAsync(new Dictionary<string, SemanticVersion>());
            var handler = CreateHandler(fileService, resolver);

            var report = await handler.HandleAsync(
                BumpType.Minor,
                "nuget.config",
                "dotnet-tools.json",
                skipTools: ["mytool"]);

            report.HasChanges.ShouldBeFalse();
            report.Warnings.ShouldContain(warning =>
                warning.Contains("--skip-tool", StringComparison.Ordinal));
            fileService.Verify(
                s => s.SaveToolsManifest(It.IsAny<ToolsManifest>(), It.IsAny<string>()),
                Times.Never);
        }

        private static ToolsManifest CreateManifest(params (string Key, string Version)[] tools)
        {
            return new ToolsManifest
            {
                Version = 1,
                IsRoot = true,
                Tools = tools.ToDictionary(
                    tool => tool.Key,
                    tool => new ToolManifestEntry
                    {
                        Version = tool.Version,
                        Commands = [tool.Key],
                        RollForward = false,
                    }),
            };
        }

        private static Mock<IToolFileService> CreateFileService(ToolsManifest manifest)
        {
            var fileService = new Mock<IToolFileService>();
            fileService.Setup(s => s.GetToolsManifest(It.IsAny<string>())).Returns(manifest);
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
