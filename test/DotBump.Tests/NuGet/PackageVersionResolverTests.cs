// Copyright © Roby Van Damme.

using DotBump.Commands;
using DotBump.Common;
using DotBump.NuGet;
using DotBump.NuGet.DataModel.NuGetClientConfiguration;
using DotBump.NuGet.DataModel.NuGetConfiguration;
using DotBump.NuGet.DataModel.NuGetService;
using DotBump.NuGet.DataModel.PackageResolution;
using DotBump.NuGet.DataModel.Registrations;
using DotBump.NuGet.Interfaces;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.NuGet;

public class PackageVersionResolverTests
{
    public class ResolveAsync
    {
        [Fact]
        public async Task With_Package_Is_PreRelease_And_Stable_Available_Returns_Stable_Version()
        {
            // Feed 1 (e.g. nuget.org) has stable 1.0.0 (higher than the package's 1.0.0-preview.1).
            // Feed 2 (preview feed) has 1.1.0-preview.1 (higher SemVer than 1.0.0, but pre-release).
            // The package should exit pre-release and bump to the stable 1.0.0 release.
            var nugetConfig = CreateConfig(
                ("stable-feed", "https://feeds.example.com/stable"),
                ("preview-feed", "https://feeds.example.com/preview"));
            var resolver = CreateResolver(new Dictionary<string, (string Url, string[] Versions)>
            {
                ["stable-feed"] = ("https://feeds.example.com/stable", ["1.0.0"]),
                ["preview-feed"] = ("https://feeds.example.com/preview", ["1.1.0-preview.1"]),
            });

            var result = await resolver.ResolveAsync(
                [new PackageToBump("mytool", new SemanticVersion("1.0.0-preview.1"))],
                BumpType.Minor,
                nugetConfig);

            result["mytool"].Version.ShouldBe("1.0.0");
        }

        [Fact]
        public async Task With_Feed_Order_Reversed_Returns_Stable_Version()
        {
            var nugetConfig = CreateConfig(
                ("preview-feed", "https://feeds.example.com/preview"),
                ("stable-feed", "https://feeds.example.com/stable"));
            var resolver = CreateResolver(new Dictionary<string, (string Url, string[] Versions)>
            {
                ["preview-feed"] = ("https://feeds.example.com/preview", ["1.1.0-preview.1"]),
                ["stable-feed"] = ("https://feeds.example.com/stable", ["1.0.0"]),
            });

            var result = await resolver.ResolveAsync(
                [new PackageToBump("mytool", new SemanticVersion("1.0.0-preview.1"))],
                BumpType.Minor,
                nugetConfig);

            result["mytool"].Version.ShouldBe("1.0.0");
        }

        [Fact]
        public async Task With_Only_PreReleases_Available_Returns_Highest_PreRelease()
        {
            var nugetConfig = CreateConfig(
                ("feed1", "https://feeds.example.com/feed1"),
                ("feed2", "https://feeds.example.com/feed2"));
            var resolver = CreateResolver(new Dictionary<string, (string Url, string[] Versions)>
            {
                ["feed1"] = ("https://feeds.example.com/feed1", ["1.0.0-preview.2"]),
                ["feed2"] = ("https://feeds.example.com/feed2", ["1.1.0-preview.1"]),
            });

            var result = await resolver.ResolveAsync(
                [new PackageToBump("mytool", new SemanticVersion("1.0.0-preview.1"))],
                BumpType.Minor,
                nugetConfig);

            result["mytool"].Version.ShouldBe("1.1.0-preview.1");
        }

        [Fact]
        public async Task With_Stable_Package_Returns_Stable_Version()
        {
            // Starting on stable 1.0.0, pre-release 1.0.2-preview.1 in the second feed
            // must be ignored, and stable 1.0.1 in the first feed must be chosen for a Patch bump.
            var nugetConfig = CreateConfig(
                ("stable-feed", "https://feeds.example.com/stable"),
                ("preview-feed", "https://feeds.example.com/preview"));
            var resolver = CreateResolver(new Dictionary<string, (string Url, string[] Versions)>
            {
                ["stable-feed"] = ("https://feeds.example.com/stable", ["1.0.1"]),
                ["preview-feed"] = ("https://feeds.example.com/preview", ["1.0.2-preview.1"]),
            });

            var result = await resolver.ResolveAsync(
                [new PackageToBump("mytool", new SemanticVersion("1.0.0"))],
                BumpType.Patch,
                nugetConfig);

            result["mytool"].Version.ShouldBe("1.0.1");
        }

        [Fact]
        public async Task With_No_Newer_Version_Returns_Empty()
        {
            var nugetConfig = CreateConfig(("stable-feed", "https://feeds.example.com/stable"));
            var resolver = CreateResolver(new Dictionary<string, (string Url, string[] Versions)>
            {
                ["stable-feed"] = ("https://feeds.example.com/stable", ["1.0.0"]),
            });

            var result = await resolver.ResolveAsync(
                [new PackageToBump("mytool", new SemanticVersion("1.0.0"))],
                BumpType.Patch,
                nugetConfig);

            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task With_Multiple_Packages_Resolves_Each()
        {
            var nugetConfig = CreateConfig(("stable-feed", "https://feeds.example.com/stable"));
            var resolver = CreateResolver(new Dictionary<string, (string Url, string[] Versions)>
            {
                ["stable-feed"] = ("https://feeds.example.com/stable", ["1.0.1", "1.5.1"]),
            });

            var result = await resolver.ResolveAsync(
                [
                    new PackageToBump("package-a", new SemanticVersion("1.0.0")),
                    new PackageToBump("package-b", new SemanticVersion("1.5.0")),
                ],
                BumpType.Minor,
                nugetConfig);

            result.ShouldSatisfyAllConditions(
                () => result["package-a"].Version.ShouldBe("1.5.1"),
                () => result["package-b"].Version.ShouldBe("1.5.1"));
        }

        [Fact]
        public async Task With_Empty_Package_List_Returns_Empty()
        {
            var nugetConfig = CreateConfig(("stable-feed", "https://feeds.example.com/stable"));
            var resolver = CreateResolver(new Dictionary<string, (string Url, string[] Versions)>());

            var result = await resolver.ResolveAsync([], BumpType.Minor, nugetConfig);

            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task With_Empty_Config_Returns_Empty()
        {
            var resolver = CreateResolver(new Dictionary<string, (string Url, string[] Versions)>());

            var result = await resolver.ResolveAsync(
                [new PackageToBump("mytool", new SemanticVersion("1.0.0"))],
                BumpType.Minor,
                new NuGetConfig());

            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task With_Null_Packages_Throws_ArgumentNullException()
        {
            var resolver = CreateResolver(new Dictionary<string, (string Url, string[] Versions)>());

            await Should.ThrowAsync<ArgumentNullException>(() => resolver.ResolveAsync(null!, BumpType.Minor, new NuGetConfig()));
        }
    }

    private static NuGetConfig CreateConfig(params (string Key, string Url)[] sources)
    {
        return new NuGetConfig
        {
            PackageSources = sources
                .Select(source => new PackageSource
                {
                    Key = source.Key,
                    Value = source.Url,
                    ProtocolVersion = "3",
                })
                .ToList(),
        };
    }

    private static PackageVersionResolver CreateResolver(
        Dictionary<string, (string Url, string[] Versions)> feedPackages)
    {
        var loggerMock = new Mock<ILogger>().Object;
        var releaseFinder = new NuGetReleaseFinder(loggerMock);

        var clientFactoryMock = new Mock<INuGetClientFactory>();
        foreach (var (_, (feedUrl, versions)) in feedPackages)
        {
            var clientMock = new Mock<INuGetClient>();
            clientMock
                .Setup(c => c.GetServiceIndexAsync(It.IsAny<string>()))
                .ReturnsAsync(
                    new ServiceIndex
                    {
                        Version = "3.0.0",
                        Resources =
                        [
                            new Resource { Id = "https://example.com/reg", Type = "RegistrationsBaseUrl" }
                        ],
                    });

            var registrationIndex = CreateRegistrationIndex(versions);
            clientMock
                .Setup(c => c.GetPackageInformationAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(registrationIndex);

            clientFactoryMock
                .Setup(f => f.CreateNuGetClient(It.Is<NuGetClientConfig>(config => config.Url == feedUrl)))
                .Returns(clientMock.Object);
        }

        return new PackageVersionResolver(clientFactoryMock.Object, releaseFinder, loggerMock);
    }

    private static RegistrationIndex CreateRegistrationIndex(params string[] versions)
    {
        var packages = versions.Select(version => new Package
        {
            Id = $"https://example.com/{version}",
            CatalogEntry = new PackageDetails { Version = version, Listed = true, },
        }).ToList();

        var lowest = versions.OrderBy(version => new SemanticVersion(version)).First();
        var highest = versions.OrderBy(version => new SemanticVersion(version)).Last();

        return new RegistrationIndex
        {
            Count = 1,
            CatalogPages =
            [
                new CatalogPage
                {
                    Id = "https://example.com/page1",
                    Lower = lowest,
                    Upper = highest,
                    Count = versions.Length,
                    Items = packages,
                },
            ],
        };
    }
}
