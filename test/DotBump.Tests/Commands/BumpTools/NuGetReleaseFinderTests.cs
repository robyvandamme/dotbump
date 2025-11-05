// Copyright © 2025 Roby Van Damme.

using DotBump.Commands;
using DotBump.Commands.BumpTools;
using DotBump.Commands.BumpTools.DataModel.Registrations;
using DotBump.Common;
using DotBump.Tests.Commands.BumpTools.Fakes;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.Commands.BumpTools;

public class NuGetReleaseFinderTests
{
    public class TryFindNewVersionInCatalogPages
    {
        public class OnePageInlineDetailsNuGet
        {
            // NOTE: this one does not cover all cases, so maybe look for a different one
            private static readonly Lazy<RegistrationIndex?> s_lazyRegistrationIndex =
                new(() =>
                {
                    var client = new FakeNuGetClient(new Mock<ILogger>().Object);
                    return client.GetPackageInformationAsync(string.Empty, "dotmarkdown").Result;
                });

            private RegistrationIndex? RegistrationIndex => s_lazyRegistrationIndex.Value;

            [Fact]

            public void No_New_Version_Found_Returns_Null()
            {
                var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
                var result = service.TryFindNewVersionInCatalogPages(
                    RegistrationIndex!.CatalogPages!,
                    new SemanticVersion("0.3.0"),
                    BumpType.Minor); // 0.3.0 is the latest version in the file.
                result.ShouldBeNull();
            }

            [Fact]

            public void Finds_New_Patch_For_Minor_Type()
            {
                var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
                var result = service.TryFindNewVersionInCatalogPages(
                    RegistrationIndex!.CatalogPages!,
                    new SemanticVersion("0.1.0"),
                    BumpType.Minor); // 0.3.0 is the latest version in the file.
                result.ShouldBe(new SemanticVersion("0.3.0"));
            }
        }

        public class TwoPageInlineDetailsNuGet
        {
            private static readonly Lazy<RegistrationIndex?> s_lazyRegistrationIndex =
                new(() =>
                {
                    var client = new FakeNuGetClient(new Mock<ILogger>().Object);
                    return client.GetPackageInformationAsync(string.Empty, "moq").Result;
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
            public void No_New_Version_Returns_Null_For_Minor_Type()
            {
                var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
                var minorTypeResult = service.TryFindNewVersionInCatalogPages(
                    RegistrationIndex!.CatalogPages!,
                    new SemanticVersion("4.20.72"), // highest version in the page
                    BumpType.Minor);
                minorTypeResult.ShouldBeNull();
            }

            [Fact]
            public void No_New_Version_Returns_Null_For_Patch_Type()
            {
                var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
                var patchTypeResult = service.TryFindNewVersionInCatalogPages(
                    RegistrationIndex!.CatalogPages!,
                    new SemanticVersion("4.20.72"), // highest version in the page
                    BumpType.Patch);
                patchTypeResult.ShouldBeNull();
            }

            [Fact]
            public void Finds_New_Minor_For_Minor_Type()
            {
                var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
                var catalogPages = RegistrationIndex!.CatalogPages;
                if (catalogPages != null)
                {
                    var result = service.TryFindNewVersionInCatalogPages(
                        catalogPages,
                        new SemanticVersion("4.7.0"),
                        BumpType.Minor);
                    result.ShouldBe(new SemanticVersion("4.20.72")); // highest version in the page
                }
            }

            [Fact]
            public void Finds_New_Patch_For_Minor_Type()
            {
                var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
                var catalogPages = RegistrationIndex!.CatalogPages;
                if (catalogPages != null)
                {
                    var result = service.TryFindNewVersionInCatalogPages(
                        catalogPages,
                        new SemanticVersion("4.20.0"),
                        BumpType.Minor);
                    result.ShouldBe(new SemanticVersion("4.20.72")); // highest version in the page
                }
            }

            [Fact]
            public void Finds_New_Patch_For_Patch_Type()
            {
                var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
                var catalogPages = RegistrationIndex!.CatalogPages;
                if (catalogPages != null)
                {
                    var result = service.TryFindNewVersionInCatalogPages(
                        catalogPages,
                        new SemanticVersion("4.7.0"),
                        BumpType.Patch);
                    result.ShouldBe(new SemanticVersion("4.7.145"));
                }
            }
        }

        public class OnePageInlineDetailsGitHub
        {
            // NOTE: only covers a small set of cases, so might make sense to add another GitHub one.
            private static readonly Lazy<RegistrationIndex?> s_lazyRegistrationIndex =
                new(() =>
                {
                    var client = new FakeNuGetClient(new Mock<ILogger>().Object);
                    return client.GetPackageInformationAsync(string.Empty, "dotbump").Result;
                });

            private RegistrationIndex? RegistrationIndex => s_lazyRegistrationIndex.Value;

            [Fact]
            public void No_New_Version_Returns_Null_For_Minor_Type()
            {
                var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
                var result = service.TryFindNewVersionInCatalogPages(
                    RegistrationIndex!.CatalogPages!,
                    new SemanticVersion("0.1.1-beta.8"),
                    BumpType.Minor);
                result.ShouldBeNull();
            }

            [Fact]
            public void No_New_Version_Returns_Null_For_Patch_Type()
            {
                var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
                var result = service.TryFindNewVersionInCatalogPages(
                    RegistrationIndex!.CatalogPages!,
                    new SemanticVersion("0.1.1-beta.8"),
                    BumpType.Patch);
                result.ShouldBeNull();
            }

            [Fact]
            public void Finds_New_PreRelease_Patch_For_Minor_Type()
            {
                var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
                var catalogPages = RegistrationIndex!.CatalogPages;
                if (catalogPages != null)
                {
                    var result = service.TryFindNewVersionInCatalogPages(
                        catalogPages,
                        new SemanticVersion("0.1.1-beta.7"),
                        BumpType.Minor);
                    result.ShouldBe(new SemanticVersion("0.1.1-beta.8"));
                }
            }

            [Fact]
            public void Finds_New_PreRelease_Patch_For_Patch_Type()
            {
                var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
                var catalogPages = RegistrationIndex!.CatalogPages;
                if (catalogPages != null)
                {
                    var result = service.TryFindNewVersionInCatalogPages(
                        catalogPages,
                        new SemanticVersion("0.1.1-beta.7"),
                        BumpType.Patch);
                    result.ShouldBe(new SemanticVersion("0.1.1-beta.8"));
                }
            }
        }
    }
}
