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

        [Fact]
        public void With_Major_Type_And_Higher_Major_Returns_Highest_Major()
        {
            var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
            var result = service.TryFindNewVersionInCatalogPages(
                CreateCatalogPages("1.9.0", "2.0.0", "2.1.5"),
                new SemanticVersion("1.5.0"),
                BumpType.Major);

            result.ShouldBe(new SemanticVersion("2.1.5"));
        }

        [Fact]
        public void With_Major_Type_And_No_Higher_Major_Returns_New_Minor()
        {
            var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
            var result = service.TryFindNewVersionInCatalogPages(
                CreateCatalogPages("1.6.0", "1.9.2"),
                new SemanticVersion("1.5.0"),
                BumpType.Major);

            result.ShouldBe(new SemanticVersion("1.9.2"));
        }

        [Fact]
        public void With_Major_Type_And_No_Newer_Version_Returns_Null()
        {
            var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
            var result = service.TryFindNewVersionInCatalogPages(
                CreateCatalogPages("2.0.0", "2.1.5"),
                new SemanticVersion("2.1.5"),
                BumpType.Major);

            result.ShouldBeNull();
        }

        [Fact]
        public void With_Major_Type_And_Prerelease_Only_Higher_Major_Returns_Null()
        {
            var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
            var result = service.TryFindNewVersionInCatalogPages(
                CreateCatalogPages("2.0.0-preview.1"),
                new SemanticVersion("1.5.0"),
                BumpType.Major);

            result.ShouldBeNull();
        }

        [Fact]
        public void With_Major_Type_And_Prerelease_Current_Prefers_Stable_Version()
        {
            var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
            var result = service.TryFindNewVersionInCatalogPages(
                CreateCatalogPages("1.6.0", "2.0.0-preview.1"),
                new SemanticVersion("1.5.0-preview.1"),
                BumpType.Major);

            result.ShouldBe(new SemanticVersion("1.6.0"));
        }

        [Fact]
        public void With_Major_Type_And_PreRelease_True_And_Higher_PreRelease_Returns_PreRelease()
        {
            var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
            var result = service.TryFindNewVersionInCatalogPages(
                CreateCatalogPages("2.0.0", "2.1.0-preview.1"),
                new SemanticVersion("1.5.0"),
                BumpType.Major,
                allowPreRelease: true,
                preferHighestVersion: true);

            result.ShouldBe(new SemanticVersion("2.1.0-preview.1"));
        }

        [Fact]
        public void With_Major_Type_And_PreRelease_True_And_Higher_Stable_Returns_Stable()
        {
            var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
            var result = service.TryFindNewVersionInCatalogPages(
                CreateCatalogPages("2.0.0-preview.1", "2.1.0"),
                new SemanticVersion("1.5.0"),
                BumpType.Major,
                allowPreRelease: true,
                preferHighestVersion: true);

            result.ShouldBe(new SemanticVersion("2.1.0"));
        }

        private static List<CatalogPage> CreateCatalogPages(params string[] versions)
        {
            var packages = versions.Select(version => new Package
            {
                Id = $"https://example.com/{version}",
                CatalogEntry = new PackageDetails { Version = version, Listed = true },
            }).ToList();

            var lowest = versions.OrderBy(version => new SemanticVersion(version)).First();
            var highest = versions.OrderBy(version => new SemanticVersion(version)).Last();

            return
            [
                new CatalogPage
                {
                    Id = "https://example.com/page1",
                    Lower = lowest,
                    Upper = highest,
                    Count = versions.Length,
                    Items = packages,
                },
            ];
        }
    }

    public class TryFindNewReleaseCatalogPages
    {
        [Fact]
        public void With_Major_Type_Includes_Higher_Major_Pages()
        {
            var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
            var index = CreateRegistrationIndex();

            var result = service.TryFindNewReleaseCatalogPages(
                index,
                new SemanticVersion("1.5.0"),
                BumpType.Major);

            result.Select(page => page.Id).ShouldBe(new[] { "page-1", "page-2" });
        }

        [Fact]
        public void With_Minor_Type_Excludes_Higher_Major_Pages()
        {
            var service = new NuGetReleaseFinder(new Mock<ILogger>().Object);
            var index = CreateRegistrationIndex();

            var result = service.TryFindNewReleaseCatalogPages(
                index,
                new SemanticVersion("1.5.0"),
                BumpType.Minor);

            result.Select(page => page.Id).ShouldBe(new[] { "page-1" });
        }

        private static RegistrationIndex CreateRegistrationIndex()
        {
            return new RegistrationIndex
            {
                Count = 2,
                CatalogPages =
                [
                    new CatalogPage { Id = "page-1", Lower = "1.0.0", Upper = "1.9.9" },
                    new CatalogPage { Id = "page-2", Lower = "2.0.0", Upper = "2.5.0" },
                ],
            };
        }
    }
}
