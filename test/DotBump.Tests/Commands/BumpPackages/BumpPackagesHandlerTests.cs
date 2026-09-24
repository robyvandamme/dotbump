// Copyright © Roby Van Damme.

using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;
using DotBump.Commands;
using DotBump.Commands.BumpPackages;
using DotBump.Commands.BumpPackages.DataModel;
using DotBump.Commands.BumpPackages.Interfaces;
using DotBump.Common;
using DotBump.NuGet.DataModel.NuGetConfiguration;
using DotBump.NuGet.DataModel.PackageResolution;
using DotBump.NuGet.Interfaces;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.Commands.BumpPackages;

public class BumpPackagesHandlerTests
{
    public class HandleAsync
    {
        [Fact]
        public async Task With_Resolved_Version_Updates_All_Occurrences_And_Saves()
        {
            var manifest = CreateManifest(("MyPackage", "1.0.0"), ("MyPackage", "1.2.0"));
            var fileService = CreateFileService(manifest);
            var resolver = CreateResolver(("MyPackage", "1.3.0"));
            var handler = CreateHandler(fileService, resolver);

            var report = await handler.HandleAsync(BumpType.Minor, "./repo", "nuget.config");

            report.HasChanges.ShouldBeTrue();
            manifest.Packages.ShouldAllBe(package => package.Version == "1.3.0");
            fileService.Verify(s => s.SavePackageManifest(manifest), Times.Once);
        }

        [Fact]
        public async Task With_Occurrences_At_Different_Versions_Uses_Highest_As_Reference()
        {
            var manifest = CreateManifest(("MyPackage", "1.0.0"), ("MyPackage", "1.2.0"));
            var fileService = CreateFileService(manifest);
            var resolver = CreateResolver(("MyPackage", "1.3.0"));
            var handler = CreateHandler(fileService, resolver);

            await handler.HandleAsync(BumpType.Minor, "./repo", "nuget.config");

            resolver.Verify(
                r => r.ResolveAsync(
                    It.Is<IReadOnlyCollection<PackageToBump>>(packages =>
                        packages.Count == 1
                        && packages.Single().PackageId == "MyPackage"
                        && packages.Single().CurrentVersion.Version == "1.2.0"),
                    BumpType.Minor,
                    It.IsAny<NuGetConfig>()),
                Times.Once);
        }

        [Fact]
        public async Task With_Resolved_Version_Lower_Than_An_Occurrence_Does_Not_Downgrade()
        {
            var manifest = CreateManifest(("MyPackage", "1.0.0"), ("MyPackage", "1.2.0"));
            var fileService = CreateFileService(manifest);
            var resolver = CreateResolver(("MyPackage", "1.0.4"));
            var handler = CreateHandler(fileService, resolver);

            var report = await handler.HandleAsync(BumpType.Patch, "./repo", "nuget.config");

            report.HasChanges.ShouldBeFalse();
            manifest.Packages.ShouldSatisfyAllConditions(
                () => manifest.Packages.Count(package => package.Version == "1.0.0").ShouldBe(1),
                () => manifest.Packages.Count(package => package.Version == "1.2.0").ShouldBe(1));
            fileService.Verify(s => s.SavePackageManifest(It.IsAny<PackageManifest>()), Times.Never);
        }

        [Fact]
        public async Task With_No_Resolved_Version_Does_Not_Save()
        {
            var manifest = CreateManifest(("MyPackage", "1.0.0"));
            var fileService = CreateFileService(manifest);
            var resolver = CreateResolver();
            var handler = CreateHandler(fileService, resolver);

            var report = await handler.HandleAsync(BumpType.Minor, "./repo", "nuget.config");

            report.HasChanges.ShouldBeFalse();
            fileService.Verify(s => s.SavePackageManifest(It.IsAny<PackageManifest>()), Times.Never);
        }

        [Fact]
        public async Task With_Validation_Errors_Reports_Errors_And_Does_Not_Resolve_Or_Save()
        {
            var manifest = CreateManifest(("MyPackage", "1.0.0"));
            var fileService = CreateFileService(manifest);
            var resolver = CreateResolver();
            var handler = CreateHandler(fileService, resolver, [new ValidationResult("bad config")]);

            var report = await handler.HandleAsync(BumpType.Minor, "./repo", "nuget.config");

            report.Errors.ShouldContain("bad config");
            fileService.Verify(s => s.SavePackageManifest(It.IsAny<PackageManifest>()), Times.Never);
            resolver.Verify(
                r => r.ResolveAsync(
                    It.IsAny<IReadOnlyCollection<PackageToBump>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<NuGetConfig>()),
                Times.Never);
        }

        [Fact]
        public async Task With_Invalid_Semantic_Version_Skips_Package()
        {
            var manifest = CreateManifest(("MyPackage", "1.2.3.4"));
            var fileService = CreateFileService(manifest);
            var resolver = CreateResolver();
            var handler = CreateHandler(fileService, resolver);

            await handler.HandleAsync(BumpType.Minor, "./repo", "nuget.config");

            resolver.Verify(
                r => r.ResolveAsync(
                    It.Is<IReadOnlyCollection<PackageToBump>>(packages => packages.Count == 0),
                    BumpType.Minor,
                    It.IsAny<NuGetConfig>()),
                Times.Once);
        }

        private static PackageManifest CreateManifest(params (string Id, string Version)[] packages)
        {
            var manifest = new PackageManifest();

            foreach (var (id, version) in packages)
            {
                var element = XElement.Parse($"<PackageReference Include=\"{id}\" Version=\"{version}\" />");
                manifest.Add(new PackageVersionEntry
                {
                    PackageId = id,
                    OriginalVersion = version,
                    Version = version,
                    FilePath = $"{id}.csproj",
                    SourceKind = PackageSourceKind.Project,
                    ElementName = "PackageReference",
                    Element = element,
                    VersionAttribute = element.Attribute("Version"),
                });
            }

            return manifest;
        }

        private static Mock<IPackageFileService> CreateFileService(PackageManifest manifest)
        {
            var fileService = new Mock<IPackageFileService>();
            fileService.Setup(s => s.GetPackageManifest(It.IsAny<string>())).Returns(manifest);
            return fileService;
        }

        private static Mock<IPackageVersionResolver> CreateResolver(params (string Id, string Version)[] resolved)
        {
            var resolver = new Mock<IPackageVersionResolver>();
            resolver
                .Setup(r => r.ResolveAsync(
                    It.IsAny<IReadOnlyCollection<PackageToBump>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<NuGetConfig>()))
                .ReturnsAsync(resolved.ToDictionary(item => item.Id, item => new SemanticVersion(item.Version)));
            return resolver;
        }

        private static IBumpPackagesHandler CreateHandler(
            Mock<IPackageFileService> fileService,
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

            return new BumpPackagesHandler(
                fileService.Object,
                nugetConfigFileServiceMock.Object,
                resolver.Object,
                validatorMock.Object,
                loggerMock);
        }
    }
}
