// Copyright © Roby Van Damme.

using DotBump.Commands;
using DotBump.Common;
using DotBump.NuGet;
using DotBump.NuGet.DataModel.Registrations;
using DotBump.Tests.NuGet.Fakes;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.NuGet;

public class NuGetReleaseFinderTests
{
    public class TryFindNewVersionInCatalogPages
    {
        // NOTE: this one does not cover all cases, so maybe look for a different one
        private static readonly Lazy<RegistrationIndex?> s_lazyDotMarkdownRegistrationIndex =
            new(() =>
            {
                var client = new FakeNuGetClient(new Mock<ILogger>().Object);
                return client.GetPackageInformationAsync(string.Empty, "dotmarkdown").Result;
            });

        /// <summary>
        /// Gets the Moq RegistrationIndex.
        /// NOTE: this one has none-semantic versions in the list.....
        /// First page is 3.1.416.3/4.7.49.
        /// Second page is 4.7.58/4.20.72.
        /// So for the minor + patch update we will always have 4.20.72 as the result.
        /// </summary>
        private static readonly Lazy<RegistrationIndex?> s_lazyMoqRegistrationIndex =
            new(() =>
            {
                var client = new FakeNuGetClient(new Mock<ILogger>().Object);
                return client.GetPackageInformationAsync(string.Empty, "moq").Result;
            });

        // NOTE: only covers a small set of cases, so might make sense to add another GitHub one.
        private static readonly Lazy<RegistrationIndex?> s_lazyDotBumpRegistrationIndex =
            new(() =>
            {
                var client = new FakeNuGetClient(new Mock<ILogger>().Object);
                return client.GetPackageInformationAsync(string.Empty, "dotbump").Result;
            });

        private RegistrationIndex? DotMarkdownRegistrationIndex => s_lazyDotMarkdownRegistrationIndex.Value;

        private RegistrationIndex? MoqRegistrationIndex => s_lazyMoqRegistrationIndex.Value;

        private RegistrationIndex? DotBumpRegistrationIndex => s_lazyDotBumpRegistrationIndex.Value;

        [Fact]
        public void With_Single_Page_And_Latest_Version_And_Minor_Type_Returns_Null()
        {
            var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
            var result = service.TryFindNewVersionInCatalogPages(
                DotMarkdownRegistrationIndex!.CatalogPages!,
                new SemanticVersion("0.3.0"),
                BumpType.Minor); // 0.3.0 is the latest version in the file.
            result.ShouldBeNull();
        }

        [Fact]
        public void With_Single_Page_And_Minor_Type_Returns_New_Patch()
        {
            var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
            var result = service.TryFindNewVersionInCatalogPages(
                DotMarkdownRegistrationIndex!.CatalogPages!,
                new SemanticVersion("0.1.0"),
                BumpType.Minor); // 0.3.0 is the latest version in the file.
            result.ShouldBe(new SemanticVersion("0.3.0"));
        }

        [Fact]
        public void With_Multi_Page_And_Latest_Version_And_Minor_Type_Returns_Null()
        {
            var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
            var minorTypeResult = service.TryFindNewVersionInCatalogPages(
                MoqRegistrationIndex!.CatalogPages!,
                new SemanticVersion("4.20.72"), // highest version in the page
                BumpType.Minor);
            minorTypeResult.ShouldBeNull();
        }

        [Fact]
        public void With_Multi_Page_And_Latest_Version_And_Patch_Type_Returns_Null()
        {
            var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
            var patchTypeResult = service.TryFindNewVersionInCatalogPages(
                MoqRegistrationIndex!.CatalogPages!,
                new SemanticVersion("4.20.72"), // highest version in the page
                BumpType.Patch);
            patchTypeResult.ShouldBeNull();
        }

        [Fact]
        public void With_Multi_Page_And_Minor_Type_Returns_New_Minor()
        {
            var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
            var catalogPages = MoqRegistrationIndex!.CatalogPages!;
            var result = service.TryFindNewVersionInCatalogPages(
                catalogPages,
                new SemanticVersion("4.7.0"),
                BumpType.Minor);
            result.ShouldBe(new SemanticVersion("4.20.72")); // highest version in the page
        }

        [Fact]
        public void With_Multi_Page_And_Minor_Type_Returns_New_Patch()
        {
            var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
            var catalogPages = MoqRegistrationIndex!.CatalogPages!;
            var result = service.TryFindNewVersionInCatalogPages(
                catalogPages,
                new SemanticVersion("4.20.0"),
                BumpType.Minor);
            result.ShouldBe(new SemanticVersion("4.20.72")); // highest version in the page
        }

        [Fact]
        public void With_Multi_Page_And_Patch_Type_Returns_New_Patch()
        {
            var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
            var catalogPages = MoqRegistrationIndex!.CatalogPages!;
            var result = service.TryFindNewVersionInCatalogPages(
                catalogPages,
                new SemanticVersion("4.7.0"),
                BumpType.Patch);
            result.ShouldBe(new SemanticVersion("4.7.145"));
        }

        [Fact]
        public void With_GitHub_Package_And_Latest_Version_And_Minor_Type_Returns_Null()
        {
            var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
            var result = service.TryFindNewVersionInCatalogPages(
                DotBumpRegistrationIndex!.CatalogPages!,
                new SemanticVersion("0.1.1-beta.8"),
                BumpType.Minor);
            result.ShouldBeNull();
        }

        [Fact]
        public void With_GitHub_Package_And_Latest_Version_And_Patch_Type_Returns_Null()
        {
            var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
            var result = service.TryFindNewVersionInCatalogPages(
                DotBumpRegistrationIndex!.CatalogPages!,
                new SemanticVersion("0.1.1-beta.8"),
                BumpType.Patch);
            result.ShouldBeNull();
        }

        [Fact]
        public void With_PreRelease_And_Minor_Type_Returns_New_PreRelease_Patch()
        {
            var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
            var catalogPages = DotBumpRegistrationIndex!.CatalogPages!;
            var result = service.TryFindNewVersionInCatalogPages(
                catalogPages,
                new SemanticVersion("0.1.1-beta.7"),
                BumpType.Minor);
            result.ShouldBe(new SemanticVersion("0.1.1-beta.8"));
        }

        [Fact]
        public void With_PreRelease_And_Patch_Type_Returns_New_PreRelease_Patch()
        {
            var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
            var catalogPages = DotBumpRegistrationIndex!.CatalogPages!;
            var result = service.TryFindNewVersionInCatalogPages(
                catalogPages,
                new SemanticVersion("0.1.1-beta.7"),
                BumpType.Patch);
            result.ShouldBe(new SemanticVersion("0.1.1-beta.8"));
        }

        [Fact]
        public void With_AllowPreRelease_True_Returns_PreRelease()
        {
            var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
            var catalogPages = DotBumpRegistrationIndex!.CatalogPages!;
            var result = service.TryFindNewVersionInCatalogPages(
                catalogPages,
                new SemanticVersion("0.1.0"),
                BumpType.Minor,
                allowPreRelease: true);
            result.ShouldBe(new SemanticVersion("0.1.1-beta.8"));
        }

        [Fact]
        public void With_AllowPreRelease_False_Returns_Null()
        {
            var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
            var catalogPages = DotBumpRegistrationIndex!.CatalogPages!;
            var result = service.TryFindNewVersionInCatalogPages(
                catalogPages,
                new SemanticVersion("0.1.0"),
                BumpType.Minor,
                allowPreRelease: false);
            result.ShouldBeNull();
        }

        [Fact]
        public void With_Both_Eligible_Returns_Stable_Version()
        {
            var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
            var versions = new[] { "1.0.0", "1.1.0-preview.1" };
            var packages = versions.Select(v => new Package
            {
                Id = $"https://example.com/{v}",
                CatalogEntry = new PackageDetails { Version = v, Listed = true },
            }).ToList();

            var catalogPages = new List<CatalogPage>
            {
                new()
                {
                    Id = "https://example.com/page1",
                    Lower = "1.0.0",
                    Upper = "1.1.0-preview.1",
                    Count = versions.Length,
                    Items = packages,
                },
            };

            var result = service.TryFindNewVersionInCatalogPages(
                catalogPages,
                new SemanticVersion("1.0.0-preview.1"),
                BumpType.Minor,
                allowPreRelease: true);

            result.ShouldBe(new SemanticVersion("1.0.0"));
        }
    }
}
