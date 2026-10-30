// Copyright © Roby Van Damme.

using System.ComponentModel.DataAnnotations;
using DotBump.Commands;
using DotBump.Commands.BumpPackages;
using DotBump.Commands.BumpPackages.DataModel;
using DotBump.Commands.BumpPackages.Interfaces;
using DotBump.Common;
using DotBump.NuGet.DataModel.NuGetConfiguration;
using DotBump.NuGet.DataModel.PackageResolution;
using DotBump.NuGet.Interfaces;
using DotBump.Tests.TestHelpers;
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
            var manifest = TestPackageManifestFactory.CreateManifest(("MyPackage", "1.0.0"), ("MyPackage", "1.2.0"));
            var fileService = CreateFileService(manifest);
            var resolver = CreateResolver(("MyPackage", "1.3.0"));
            var handler = CreateHandler(fileService, resolver);

            var report = await handler.HandleAsync(BumpType.Minor, "./repo", "nuget.config");

            report.HasChanges.ShouldBeTrue();
            manifest.Packages.ShouldAllBe(package => package.Version == "1.3.0");
            fileService.Verify(s => s.SavePackageManifest(manifest), Times.Once);
        }

        [Fact]
        public async Task With_Sdk_Reference_Resolves_And_Bumps()
        {
            var manifest = TestPackageManifestFactory.CreateManifest(("Aspire.AppHost.Sdk", "13.0.1"));
            var fileService = CreateFileService(manifest);
            var resolver = CreateResolver(("Aspire.AppHost.Sdk", "13.6.4"));
            var handler = CreateHandler(fileService, resolver);

            var report = await handler.HandleAsync(BumpType.Minor, "./repo", "nuget.config");

            report.ShouldSatisfyAllConditions(
                () => report.HasChanges.ShouldBeTrue(),
                () => report.Results.ShouldContain(result =>
                    result.Id == "Aspire.AppHost.Sdk"
                    && result.OldVersion == "13.0.1"
                    && result.NewVersion == "13.6.4"));
            manifest.Packages.Single().Version.ShouldBe("13.6.4");
            fileService.Verify(s => s.SavePackageManifest(manifest), Times.Once);
        }

        [Fact]
        public async Task With_Major_Bump_Type_Forwards_Type_And_Applies_Version()
        {
            var manifest = TestPackageManifestFactory.CreateManifest(("MyPackage", "1.0.0"));
            var fileService = CreateFileService(manifest);
            var resolver = CreateResolver(("MyPackage", "2.0.0"));
            var handler = CreateHandler(fileService, resolver);

            var report = await handler.HandleAsync(BumpType.Major, "./repo", "nuget.config");

            report.HasChanges.ShouldBeTrue();
            manifest.Packages.Single().Version.ShouldBe("2.0.0");
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
            var manifest = TestPackageManifestFactory.CreateManifest(("MyPackage", "1.0.0"));
            var fileService = CreateFileService(manifest);
            var resolver = new Mock<IPackageVersionResolver>();
            resolver
                .Setup(r => r.ResolveAsync(
                    It.IsAny<IReadOnlyCollection<PackageToBump>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<NuGetConfig>(),
                    It.IsAny<bool>()))
                .ReturnsAsync(new Dictionary<PackageToBump, SemanticVersion>());
            var handler = CreateHandler(fileService, resolver);

            await handler.HandleAsync(BumpType.Major, "./repo", "nuget.config", preRelease: true);

            resolver.Verify(
                r => r.ResolveAsync(
                    It.IsAny<IReadOnlyCollection<PackageToBump>>(),
                    BumpType.Major,
                    It.IsAny<NuGetConfig>(),
                    true),
                Times.Once);
        }

        [Fact]
        public async Task With_Occurrences_At_Different_Versions_Uses_Highest_As_Reference()
        {
            var manifest = TestPackageManifestFactory.CreateManifest(("MyPackage", "1.0.0"), ("MyPackage", "1.2.0"));
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
                    It.IsAny<NuGetConfig>(),
                    It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task With_Per_Tfm_Conditions_Resolves_Each_Variant_Independently()
        {
            const string net10Condition = "'$(TargetFramework)' == 'net10.0'";
            const string net11Condition = "'$(TargetFramework)' == 'net11.0'";
            var manifest = TestPackageManifestFactory.CreateManifestWithConditions(
                ("Microsoft.AspNetCore.OpenApi", "10.0.14", net10Condition),
                ("Microsoft.AspNetCore.OpenApi", "11.0.0-rc.3.26522.148", net11Condition));
            var fileService = CreateFileService(manifest);
            var resolver = CreateResolverForVersions(
                ("Microsoft.AspNetCore.OpenApi", "10.0.14", "10.0.15"),
                ("Microsoft.AspNetCore.OpenApi", "11.0.0-rc.3.26522.148", "11.0.0-rc.4"));
            var handler = CreateHandler(fileService, resolver);

            var report = await handler.HandleAsync(BumpType.Minor, "./repo", "nuget.config");

            report.HasChanges.ShouldBeTrue();
            manifest.Packages.ShouldSatisfyAllConditions(
                () => manifest.Packages.Single(package => package.Condition == net10Condition).Version.ShouldBe("10.0.15"),
                () => manifest.Packages.Single(package => package.Condition == net11Condition).Version.ShouldBe("11.0.0-rc.4"));
            resolver.Verify(
                r => r.ResolveAsync(
                    It.Is<IReadOnlyCollection<PackageToBump>>(packages =>
                        packages.Count == 2
                        && packages.Any(package =>
                            package.PackageId == "Microsoft.AspNetCore.OpenApi"
                            && package.CurrentVersion.Version == "10.0.14")
                        && packages.Any(package =>
                            package.PackageId == "Microsoft.AspNetCore.OpenApi"
                            && package.CurrentVersion.Version == "11.0.0-rc.3.26522.148")),
                    BumpType.Minor,
                    It.IsAny<NuGetConfig>(),
                    It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task With_Different_Conditions_And_Same_Reference_Version_Resolves_Once_And_Bumps_Both()
        {
            const string net10Condition = "'$(TargetFramework)' == 'net10.0'";
            const string net11Condition = "'$(TargetFramework)' == 'net11.0'";
            var manifest = TestPackageManifestFactory.CreateManifestWithConditions(
                ("MyPackage", "1.0.0", net10Condition),
                ("mypackage", "1.0.0", net11Condition));
            var fileService = CreateFileService(manifest);
            var resolver = CreateResolver(("MyPackage", "1.1.0"));
            var handler = CreateHandler(fileService, resolver);

            await handler.HandleAsync(BumpType.Minor, "./repo", "nuget.config");

            manifest.Packages.ShouldAllBe(package => package.Version == "1.1.0");
            resolver.Verify(
                r => r.ResolveAsync(
                    It.Is<IReadOnlyCollection<PackageToBump>>(packages => packages.Count == 1),
                    BumpType.Minor,
                    It.IsAny<NuGetConfig>(),
                    It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task With_Resolved_Version_Lower_Than_An_Occurrence_Does_Not_Downgrade()
        {
            var manifest = TestPackageManifestFactory.CreateManifest(("MyPackage", "1.0.0"), ("MyPackage", "1.2.0"));
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
        public async Task With_Resolved_Version_Differing_Only_In_Casing_Does_Not_Bump_Or_Save()
        {
            var manifest = TestPackageManifestFactory.CreateManifest(("MyPackage", "1.0.0-RC1"));
            var fileService = CreateFileService(manifest);
            var resolver = CreateResolver(("MyPackage", "1.0.0-rc1"));
            var handler = CreateHandler(fileService, resolver);

            var report = await handler.HandleAsync(BumpType.Patch, "./repo", "nuget.config");

            report.HasChanges.ShouldBeFalse();
            manifest.Packages.Single().Version.ShouldBe("1.0.0-RC1");
            fileService.Verify(s => s.SavePackageManifest(It.IsAny<PackageManifest>()), Times.Never);
        }

        [Fact]
        public async Task With_No_Resolved_Version_Does_Not_Save()
        {
            var manifest = TestPackageManifestFactory.CreateManifest(("MyPackage", "1.0.0"));
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
            var manifest = TestPackageManifestFactory.CreateManifest(("MyPackage", "1.0.0"));
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
                    It.IsAny<NuGetConfig>(),
                    It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task With_Invalid_Semantic_Version_Skips_And_Reports_Warning()
        {
            var manifest = TestPackageManifestFactory.CreateManifest(("MyPackage", "1.2.3.beta"));
            var fileService = CreateFileService(manifest);
            var resolver = CreateResolver();
            var handler = CreateHandler(fileService, resolver);

            var report = await handler.HandleAsync(BumpType.Minor, "./repo", "nuget.config");

            resolver.Verify(
                r => r.ResolveAsync(
                    It.Is<IReadOnlyCollection<PackageToBump>>(packages => packages.Count == 0),
                    BumpType.Minor,
                    It.IsAny<NuGetConfig>(),
                    It.IsAny<bool>()),
                Times.Once);
            report.Warnings.ShouldContain(warning =>
                warning.Contains("MyPackage", StringComparison.Ordinal)
                && warning.Contains("1.2.3.beta", StringComparison.Ordinal));
        }

        [Fact]
        public async Task With_Mixed_Valid_And_Unparseable_Versions_Warns_And_Updates_Both()
        {
            var manifest = TestPackageManifestFactory.CreateManifest(
                ("MyPackage", "1.0"),
                ("MyPackage", "1.5.0"));
            var fileService = CreateFileService(manifest);
            var resolver = CreateResolver(("MyPackage", "1.6.0"));
            var handler = CreateHandler(fileService, resolver);

            var report = await handler.HandleAsync(BumpType.Minor, "./repo", "nuget.config");

            manifest.Packages.ShouldAllBe(package => package.Version == "1.6.0");
            report.Warnings.ShouldContain(warning =>
                warning.Contains("MyPackage", StringComparison.Ordinal)
                && warning.Contains("'1.0'", StringComparison.Ordinal)
                && warning.Contains("overwritten", StringComparison.Ordinal));
        }

        [Fact]
        public async Task With_Four_Part_Version_Resolves_And_Bumps()
        {
            var manifest = TestPackageManifestFactory.CreateManifest(("MyPackage", "1.2.0.556"));
            var fileService = CreateFileService(manifest);
            var resolver = CreateResolver(("MyPackage", "1.2.0.586"));
            var handler = CreateHandler(fileService, resolver);

            var report = await handler.HandleAsync(BumpType.Minor, "./repo", "nuget.config");

            report.HasChanges.ShouldBeTrue();
            manifest.Packages.Single().Version.ShouldBe("1.2.0.586");
            report.Warnings.ShouldNotContain(warning =>
                warning.Contains("cannot be parsed", StringComparison.Ordinal));
        }

        [Fact]
        public async Task With_Read_Warnings_Reports_Warnings()
        {
            const string readWarning =
                "Skipping 'Other.Package' in 'Other.csproj' because version '1.0.*' is not supported.";
            var manifest = TestPackageManifestFactory.CreateManifest(("MyPackage", "1.0.0"));
            manifest.AddWarning(readWarning);
            var fileService = CreateFileService(manifest);
            var resolver = CreateResolver(("MyPackage", "1.1.0"));
            var handler = CreateHandler(fileService, resolver);

            var report = await handler.HandleAsync(BumpType.Minor, "./repo", "nuget.config");

            report.Warnings.ShouldContain(readWarning);
        }

        [Fact]
        public async Task With_Validation_Errors_Still_Reports_Read_Warnings()
        {
            const string readWarning =
                "Skipping 'Other.Package' in 'Other.csproj' because version '1.0.*' is not supported.";
            var manifest = TestPackageManifestFactory.CreateManifest(("MyPackage", "1.0.0"));
            manifest.AddWarning(readWarning);
            var fileService = CreateFileService(manifest);
            var resolver = CreateResolver();
            var handler = CreateHandler(fileService, resolver, [new ValidationResult("bad config")]);

            var report = await handler.HandleAsync(BumpType.Minor, "./repo", "nuget.config");

            report.ShouldSatisfyAllConditions(
                () => report.Errors.ShouldContain("bad config"),
                () => report.Warnings.ShouldContain(readWarning));
        }

        [Fact]
        public async Task With_Excluded_Paths_Forwards_Them_To_The_File_Service()
        {
            var manifest = TestPackageManifestFactory.CreateManifest(("MyPackage", "1.0.0"));
            var fileService = CreateFileService(manifest);
            var resolver = CreateResolver(("MyPackage", "1.1.0"));
            var handler = CreateHandler(fileService, resolver);
            string[] excludedPaths = ["./excluded"];

            await handler.HandleAsync(BumpType.Minor, "./repo", "nuget.config", excludedPaths);

            fileService.Verify(
                s => s.GetPackageManifest(
                    "./repo",
                    It.Is<IReadOnlyCollection<string>>(paths => paths.Contains("./excluded"))),
                Times.Once);
        }

        [Fact]
        public async Task With_Skipped_Package_Does_Not_Resolve_And_Reports_Warning()
        {
            var manifest = TestPackageManifestFactory.CreateManifest(("Skip.Me", "1.0.0"), ("Keep.Me", "1.0.0"));
            var fileService = CreateFileService(manifest);
            var resolver = CreateResolver(("Skip.Me", "2.0.0"), ("Keep.Me", "1.1.0"));
            var handler = CreateHandler(fileService, resolver);

            var report = await handler.HandleAsync(
                BumpType.Minor,
                "./repo",
                "nuget.config",
                skipPackages: ["Skip.Me"]);

            report.Warnings.ShouldContain(warning =>
                warning.Contains("Skip.Me", StringComparison.Ordinal)
                && warning.Contains("--skip-package", StringComparison.Ordinal));
            manifest.Packages.ShouldSatisfyAllConditions(
                () => manifest.Packages.Single(package => package.PackageId == "Skip.Me").Version.ShouldBe("1.0.0"),
                () => manifest.Packages.Single(package => package.PackageId == "Keep.Me").Version.ShouldBe("1.1.0"));
            resolver.Verify(
                r => r.ResolveAsync(
                    It.Is<IReadOnlyCollection<PackageToBump>>(packages =>
                        packages.Count == 1 && packages.Single().PackageId == "Keep.Me"),
                    It.IsAny<BumpType>(),
                    It.IsAny<NuGetConfig>(),
                    It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task With_Skipped_Package_Wildcard_Matches_Case_Insensitively()
        {
            var manifest = TestPackageManifestFactory.CreateManifest(
                ("Microsoft.Extensions.Logging", "10.0.0"),
                ("Newtonsoft.Json", "12.0.1"));
            var fileService = CreateFileService(manifest);
            var resolver = CreateResolver(
                ("Microsoft.Extensions.Logging", "11.0.0"),
                ("Newtonsoft.Json", "13.0.3"));
            var handler = CreateHandler(fileService, resolver);

            var report = await handler.HandleAsync(
                BumpType.Major,
                "./repo",
                "nuget.config",
                skipPackages: ["microsoft.*"]);

            report.Warnings.ShouldContain(warning =>
                warning.Contains("Microsoft.Extensions.Logging", StringComparison.Ordinal)
                && warning.Contains("microsoft.*", StringComparison.Ordinal));
            manifest.Packages.ShouldSatisfyAllConditions(
                () => manifest.Packages.Single(package => package.PackageId == "Microsoft.Extensions.Logging").Version
                    .ShouldBe("10.0.0"),
                () => manifest.Packages.Single(package => package.PackageId == "Newtonsoft.Json").Version
                    .ShouldBe("13.0.3"));
            resolver.Verify(
                r => r.ResolveAsync(
                    It.Is<IReadOnlyCollection<PackageToBump>>(packages =>
                        packages.Count == 1 && packages.Single().PackageId == "Newtonsoft.Json"),
                    It.IsAny<BumpType>(),
                    It.IsAny<NuGetConfig>(),
                    It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task With_Skipped_Package_And_Invalid_Version_Reports_Only_Skip_Warning()
        {
            var manifest = TestPackageManifestFactory.CreateManifest(("MyPackage", "1.2.3.beta"));
            var fileService = CreateFileService(manifest);
            var resolver = CreateResolver();
            var handler = CreateHandler(fileService, resolver);

            var report = await handler.HandleAsync(
                BumpType.Minor,
                "./repo",
                "nuget.config",
                skipPackages: ["MyPackage"]);

            report.Warnings.ShouldSatisfyAllConditions(
                () => report.Warnings.ShouldContain(warning =>
                    warning.Contains("--skip-package", StringComparison.Ordinal)),
                () => report.Warnings.ShouldNotContain(warning =>
                    warning.Contains("cannot be parsed", StringComparison.Ordinal)));
            resolver.Verify(
                r => r.ResolveAsync(
                    It.Is<IReadOnlyCollection<PackageToBump>>(packages => packages.Count == 0),
                    It.IsAny<BumpType>(),
                    It.IsAny<NuGetConfig>(),
                    It.IsAny<bool>()),
                Times.Once);
        }

        private static Mock<IPackageFileService> CreateFileService(PackageManifest manifest)
        {
            var fileService = new Mock<IPackageFileService>();
            fileService
                .Setup(s => s.GetPackageManifest(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<string>>()))
                .Returns(manifest);
            return fileService;
        }

        private static Mock<IPackageVersionResolver> CreateResolver(params (string Id, string Version)[] resolved)
        {
            var resolver = new Mock<IPackageVersionResolver>();
            resolver
                .Setup(r => r.ResolveAsync(
                    It.IsAny<IReadOnlyCollection<PackageToBump>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<NuGetConfig>(),
                    It.IsAny<bool>()))
                .Returns((IReadOnlyCollection<PackageToBump> packages, BumpType _, NuGetConfig _, bool _) =>
                {
                    var bestVersions = new Dictionary<PackageToBump, SemanticVersion>();
                    foreach (var package in packages)
                    {
                        var match = resolved.FirstOrDefault(item => string.Equals(
                            package.PackageId,
                            item.Id,
                            StringComparison.OrdinalIgnoreCase));
                        if (match.Id != null)
                        {
                            bestVersions[package] = new SemanticVersion(match.Version);
                        }
                    }

                    return Task.FromResult<IReadOnlyDictionary<PackageToBump, SemanticVersion>>(bestVersions);
                });
            return resolver;
        }

        private static Mock<IPackageVersionResolver> CreateResolverForVersions(
            params (string Id, string CurrentVersion, string ResolvedVersion)[] resolved)
        {
            var resolver = new Mock<IPackageVersionResolver>();
            resolver
                .Setup(r => r.ResolveAsync(
                    It.IsAny<IReadOnlyCollection<PackageToBump>>(),
                    It.IsAny<BumpType>(),
                    It.IsAny<NuGetConfig>(),
                    It.IsAny<bool>()))
                .Returns((IReadOnlyCollection<PackageToBump> packages, BumpType _, NuGetConfig _, bool _) =>
                {
                    var bestVersions = new Dictionary<PackageToBump, SemanticVersion>();
                    foreach (var package in packages)
                    {
                        var match = resolved.FirstOrDefault(item =>
                            string.Equals(package.PackageId, item.Id, StringComparison.OrdinalIgnoreCase)
                            && new SemanticVersion(item.CurrentVersion) == package.CurrentVersion);
                        if (match.Id != null)
                        {
                            bestVersions[package] = new SemanticVersion(match.ResolvedVersion);
                        }
                    }

                    return Task.FromResult<IReadOnlyDictionary<PackageToBump, SemanticVersion>>(bestVersions);
                });
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
