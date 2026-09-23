// Copyright © Roby Van Damme.

using System.ComponentModel.DataAnnotations;
using DotBump.Commands;
using DotBump.Commands.BumpTools;
using DotBump.Commands.BumpTools.DataModel.LocalTools;
using DotBump.Commands.BumpTools.Interfaces;
using DotBump.Common;
using DotBump.NuGet;
using DotBump.NuGet.DataModel.NuGetClientConfiguration;
using DotBump.NuGet.DataModel.NuGetConfiguration;
using DotBump.NuGet.DataModel.NuGetService;
using DotBump.NuGet.DataModel.Registrations;
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
        public async Task
            When_Tool_Is_PreRelease_And_Stable_Version_Available_Prefers_Stable_Version_Over_Higher_PreRelease()
        {
            // Feed 1 (e.g. nuget.org) has stable 1.0.0 (higher than tool's 1.0.0-preview.1)
            // Feed 2 (preview feed) has 1.1.0-preview.1 (higher SemVer than 1.0.0, but pre-release)
            // The tool should exit pre-release and bump to the stable 1.0.0 release.
            var manifest = CreateManifest("mytool", "1.0.0-preview.1");
            var nugetConfig = new NuGetConfig
            {
                PackageSources =
                [
                    new PackageSource
                    {
                        Key = "stable-feed", Value = "https://feeds.example.com/stable", ProtocolVersion = "3",
                    },
                    new PackageSource
                    {
                        Key = "preview-feed", Value = "https://feeds.example.com/preview", ProtocolVersion = "3",
                    },
                ],
            };

            var handler = CreateHandler(
                manifest,
                nugetConfig,
                feedPackages: new Dictionary<string, (string Url, string[] Versions)>
                {
                    ["stable-feed"] = ("https://feeds.example.com/stable", ["1.0.0"]),
                    ["preview-feed"] = ("https://feeds.example.com/preview", ["1.1.0-preview.1"]),
                });

            var report = await handler.HandleAsync(BumpType.Minor, "nuget.config");

            report.HasChanges.ShouldBeTrue();
            manifest.Tools["mytool"].Version.ShouldBe("1.0.0");
        }

        [Fact]
        public async Task When_Tool_Is_PreRelease_And_Feed_Order_Is_Reversed_Still_Prefers_Stable_Version()
        {
            // When the feed order is reversed (preview feed first, stable feed second),
            // the stable 1.0.0 release must still be preferred over 1.1.0-preview.1.
            var manifest = CreateManifest("mytool", "1.0.0-preview.1");
            var nugetConfig = new NuGetConfig
            {
                PackageSources =
                [
                    new PackageSource
                    {
                        Key = "preview-feed", Value = "https://feeds.example.com/preview", ProtocolVersion = "3",
                    },
                    new PackageSource
                    {
                        Key = "stable-feed", Value = "https://feeds.example.com/stable", ProtocolVersion = "3",
                    },
                ],
            };

            var handler = CreateHandler(
                manifest,
                nugetConfig,
                feedPackages: new Dictionary<string, (string Url, string[] Versions)>
                {
                    ["preview-feed"] = ("https://feeds.example.com/preview", ["1.1.0-preview.1"]),
                    ["stable-feed"] = ("https://feeds.example.com/stable", ["1.0.0"]),
                });

            var report = await handler.HandleAsync(BumpType.Minor, "nuget.config");

            report.HasChanges.ShouldBeTrue();
            manifest.Tools["mytool"].Version.ShouldBe("1.0.0");
        }

        [Fact]
        public async Task
            When_Tool_Is_PreRelease_And_Only_PreReleases_Available_Bumps_To_Highest_PreRelease_Across_Feeds()
        {
            // When no stable version is available in any feed, it upgrades to the highest pre-release.
            var manifest = CreateManifest("mytool", "1.0.0-preview.1");
            var nugetConfig = new NuGetConfig
            {
                PackageSources =
                [
                    new PackageSource
                    {
                        Key = "feed1", Value = "https://feeds.example.com/feed1", ProtocolVersion = "3",
                    },
                    new PackageSource
                    {
                        Key = "feed2", Value = "https://feeds.example.com/feed2", ProtocolVersion = "3",
                    },
                ],
            };

            var handler = CreateHandler(
                manifest,
                nugetConfig,
                feedPackages: new Dictionary<string, (string Url, string[] Versions)>
                {
                    ["feed1"] = ("https://feeds.example.com/feed1", ["1.0.0-preview.2"]),
                    ["feed2"] = ("https://feeds.example.com/feed2", ["1.1.0-preview.1"]),
                });

            var report = await handler.HandleAsync(BumpType.Minor, "nuget.config");

            report.HasChanges.ShouldBeTrue();
            manifest.Tools["mytool"].Version.ShouldBe("1.1.0-preview.1");
        }

        [Fact]
        public async Task When_Tool_Is_Stable_PreRelease_In_Second_Feed_Is_Excluded()
        {
            // When the tool starts on stable 1.0.0, pre-release 1.0.2-preview.1 in the second feed
            // must be ignored, and stable 1.0.1 in the first feed must be chosen for Patch bump.
            var manifest = CreateManifest("mytool", "1.0.0");
            var nugetConfig = new NuGetConfig
            {
                PackageSources =
                [
                    new PackageSource
                    {
                        Key = "stable-feed", Value = "https://feeds.example.com/stable", ProtocolVersion = "3",
                    },
                    new PackageSource
                    {
                        Key = "preview-feed", Value = "https://feeds.example.com/preview", ProtocolVersion = "3",
                    },
                ],
            };

            var handler = CreateHandler(
                manifest,
                nugetConfig,
                feedPackages: new Dictionary<string, (string Url, string[] Versions)>
                {
                    ["stable-feed"] = ("https://feeds.example.com/stable", ["1.0.1"]),
                    ["preview-feed"] = ("https://feeds.example.com/preview", ["1.0.2-preview.1"]),
                });

            var report = await handler.HandleAsync(BumpType.Patch, "nuget.config");

            report.HasChanges.ShouldBeTrue();
            manifest.Tools["mytool"].Version.ShouldBe("1.0.1");
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

        private static IBumpToolsHandler CreateHandler(
            ToolsManifest manifest,
            NuGetConfig nugetConfig,
            Dictionary<string, (string Url, string[] Versions)> feedPackages)
        {
            var loggerMock = new Mock<ILogger>().Object;

            var toolFileServiceMock = new Mock<IToolFileService>();
            toolFileServiceMock.Setup(s => s.GetToolsManifest()).Returns(manifest);

            var nugetConfigFileServiceMock = new Mock<INuGetConfigFileService>();
            nugetConfigFileServiceMock.Setup(s => s.GetNuGetConfiguration(It.IsAny<string>())).Returns(nugetConfig);

            var validatorMock = new Mock<INuGetConfigValidator>();
            validatorMock.Setup(v => v.Validate(It.IsAny<NuGetConfig>())).Returns(new List<ValidationResult>());

            var releaseFinder = new NuGetReleaseFinder(loggerMock);

            var clientFactoryMock = new Mock<INuGetClientFactory>();
            foreach (var (feedName, (feedUrl, versions)) in feedPackages)
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

                var regIndex = CreateRegistrationIndex(versions);
                clientMock
                    .Setup(c => c.GetPackageInformationAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(regIndex);

                clientFactoryMock
                    .Setup(f => f.CreateNuGetClient(It.Is<NuGetClientConfig>(cfg => cfg.Url == feedUrl)))
                    .Returns(clientMock.Object);
            }

            return new BumpToolsHandler(
                toolFileServiceMock.Object,
                nugetConfigFileServiceMock.Object,
                clientFactoryMock.Object,
                releaseFinder,
                validatorMock.Object,
                loggerMock);
        }

        private static RegistrationIndex CreateRegistrationIndex(params string[] versions)
        {
            var packages = versions.Select(v => new Package
            {
                Id = $"https://example.com/{v}",
                CatalogEntry = new PackageDetails { Version = v, Listed = true, },
            }).ToList();

            var lowest = versions.OrderBy(v => new SemanticVersion(v)).First();
            var highest = versions.OrderBy(v => new SemanticVersion(v)).Last();

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
}
