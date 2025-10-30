// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpTools;
using DotBump.Commands.BumpTools.DataModel.Registrations;
using DotBump.Common;
using DotBump.Tests.Commands.BumpTools.Fakes;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.Commands.BumpTools;

public class NuGetReleaseServiceTests
{
    [Fact]
    public async Task Get_Something()
    {
        var client = new FakeNuGetClient();
        var result = await client.GetServiceIndexesAsync(new Mock<IReadOnlyCollection<string>>().Object);
        result.ShouldNotBeEmpty();
    }

    public class TryGetNewMinorOrPatchVersionFromCatalogPages
    {
        public class Given_Inline_Single_Page
        {
            private static readonly Lazy<RegistrationIndex?> s_lazyRegistrationIndex =
                new(() =>
                {
                    var client = new FakeNuGetClient();
                    return client.GetPackageInformationAsync(new List<string>(), "dotmarkdown").Result;
                });

            private RegistrationIndex? RegistrationIndex => s_lazyRegistrationIndex.Value;

            [Fact]
            public void When_No_New_Version_Then_Return_Null()
            {
                var service = new NuGetReleaseService(new Mock<ILogger>().Object);
                var result = service.TryGetNewMinorOrPatchVersionFromCatalogPages(
                    RegistrationIndex!.CatalogPages,
                    new SemanticVersion("0.3.0")); // 0.3.0 is the latest version in the file.
                result.ShouldBeNull();
            }

            [Fact]
            public void When_New_Version_Then_Return_Matching_Version()
            {
                var service = new NuGetReleaseService(new Mock<ILogger>().Object);
                var result = service.TryGetNewMinorOrPatchVersionFromCatalogPages(
                    RegistrationIndex!.CatalogPages,
                    new SemanticVersion("0.1.0")); // 0.3.0 is the latest version in the file.
                result.ShouldBe(new SemanticVersion("0.3.0"));
            }
        }

        public class Inline_Two_Pages
        {
            private static readonly Lazy<RegistrationIndex?> s_lazyRegistrationIndex =
                new(() =>
                {
                    var client = new FakeNuGetClient();
                    return client.GetPackageInformationAsync(new List<string>(), "moq").Result;
                });

            /// <summary>
            /// Gets the Moq RegistrationIndex.
            /// NOTE: this one has none-semantic versions in the list.....
            /// First page is 3.1.416.3/4.7.49.
            /// Second page is 4.7.58/4.20.72.
            /// So for the minor + patch update we will always have 4.20.72 as the result.
            /// </summary>
            private RegistrationIndex? RegistrationIndex => s_lazyRegistrationIndex.Value;

            [Fact]
            public void No_New_Version_Returns_Null()
            {
                var service = new NuGetReleaseService(new Mock<ILogger>().Object);
                var result = service.TryGetNewMinorOrPatchVersionFromCatalogPages(
                    RegistrationIndex!.CatalogPages,
                    new SemanticVersion("4.20.72")); // highest version in the page
                result.ShouldBeNull();
            }

            [Fact]
            public void New_Version_Returns_Matching_Version()
            {
                var service = new NuGetReleaseService(new Mock<ILogger>().Object);
                var result = service.TryGetNewMinorOrPatchVersionFromCatalogPages(
                    RegistrationIndex!.CatalogPages,
                    new SemanticVersion("4.5.6-alpha")); // first semantic version available
                result.ShouldBe(new SemanticVersion("4.20.72")); // highest version in the page
            }
        }
    }
}
