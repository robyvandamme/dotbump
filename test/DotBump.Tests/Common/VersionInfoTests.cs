// Copyright © Roby Van Damme.

using System.Reflection;
using DotBump.Common;
using Shouldly;

namespace DotBump.Tests.Common;

public class VersionInfoTests
{
    public class Constructor
    {
        [Fact]
        public void With_Null_Assembly_Throws_ArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(() => new VersionInfo(null!));
        }

        [Fact]
        public void With_Valid_Assembly_Sets_Version_Metadata()
        {
            var versionInfo = new VersionInfo(typeof(VersionInfo).Assembly);

            versionInfo.ShouldSatisfyAllConditions(
                () => versionInfo.AssemblyVersion.ShouldNotBeNullOrWhiteSpace(),
                () => versionInfo.AssemblyFileVersionInfo.ShouldNotBeNullOrWhiteSpace(),
                () => versionInfo.ProductVersion.ShouldNotBeNullOrWhiteSpace(),
                () => versionInfo.Version.ShouldNotBeNullOrWhiteSpace());
        }

        [Fact]
        public void With_Build_Metadata_Returns_Version_Without_Metadata()
        {
            var versionInfo = new VersionInfo(Assembly.GetExecutingAssembly());

            versionInfo.ShouldSatisfyAllConditions(
                () => versionInfo.ProductVersion.ShouldNotBeNullOrWhiteSpace(),
                () => versionInfo.Version.ShouldBe(
                    VersionInfo.ParseProductVersion(versionInfo.ProductVersion)));
        }
    }

    public class ParseProductVersion
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("1.2.3", "1.2.3")]
        [InlineData("1.2.3+commit", "1.2.3")]
        public void With_Product_Version_Returns_Expected_Version(string? productVersion, string? expectedVersion)
        {
            VersionInfo.ParseProductVersion(productVersion).ShouldBe(expectedVersion);
        }
    }
}
