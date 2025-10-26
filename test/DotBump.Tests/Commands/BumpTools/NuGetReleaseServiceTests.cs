// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpTools;
using DotBump.Commands.BumpTools.DataModel.Registrations;
using DotBump.Common;
using DotBump.Tests.Commands.BumpTools.Fakes;
using Shouldly;

namespace DotBump.Tests.Commands.BumpTools;

public class NuGetReleaseServiceTests
{
    [Fact]
    public async Task Get_Something()
    {
        var client = new FakeNuGetServiceClient();
        var result = await client.GetServiceIndexesAsync(new List<string>());
        result.ShouldNotBeEmpty();
    }

    public class TryGetNewMinorOrPatchVersionFromCatalogPages
    {
        public class Given_Inline_Single_Page
        {
            private static readonly Lazy<RegistrationIndex?> s_lazyRegistrationIndex =
                new(() =>
                {
                    var client = new FakeNuGetServiceClient();
                    return client.GetPackageInformationAsync(new List<string>(), "dotmarkdown").Result;
                });

            private RegistrationIndex? RegistrationIndex => s_lazyRegistrationIndex.Value;

            [Fact]
            public void When_No_New_Version_Then_Return_Null()
            {
                var service = new NuGetReleaseService();
                var result = service.TryGetNewMinorOrPatchVersionFromCatalogPages(
                    RegistrationIndex!.CatalogPages,
                    new SemanticVersion("0.3.0")); // 0.3.0 is the latest version in the file.
                result.ShouldBeNull();
            }

            [Fact]
            public void When_New_Version_Then_Return_Matching_Version()
            {
                var service = new NuGetReleaseService();
                var result = service.TryGetNewMinorOrPatchVersionFromCatalogPages(
                    RegistrationIndex!.CatalogPages,
                    new SemanticVersion("0.1.0")); // 0.3.0 is the latest version in the file.
                result.ShouldBe(new SemanticVersion("0.3.0"));
            }
        }
    }
}
