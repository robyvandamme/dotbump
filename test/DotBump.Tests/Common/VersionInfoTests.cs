// Copyright © Roby Van Damme.

using System.Reflection;
using DotBump.Common;
using Shouldly;

namespace DotBump.Tests.Common;

public class VersionInfoTests
{
    [Fact]
    public void Throws_When_Assembly_Is_Null()
    {
        Should.Throw<ArgumentNullException>(() => new VersionInfo(null!));
    }

    [Fact]
    public void Reads_Version_Metadata_From_Assembly()
    {
        var versionInfo = new VersionInfo(typeof(VersionInfo).Assembly);

        versionInfo.AssemblyVersion.ShouldNotBeNullOrWhiteSpace();
        versionInfo.AssemblyFileVersionInfo.ShouldNotBeNullOrWhiteSpace();
        versionInfo.ProductVersion.ShouldNotBeNullOrWhiteSpace();
        versionInfo.Version.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData("1.2.3+commit", "1.2.3")]
    public void Parses_Product_Version(string? productVersion, string? expectedVersion)
    {
        VersionInfo.ParseProductVersion(productVersion).ShouldBe(expectedVersion);
    }

    [Fact]
    public void Removes_Build_Metadata_From_Assembly_Product_Version()
    {
        var versionInfo = new VersionInfo(Assembly.GetExecutingAssembly());

        versionInfo.ProductVersion.ShouldNotBeNullOrWhiteSpace();
        versionInfo.Version.ShouldBe(
            VersionInfo.ParseProductVersion(versionInfo.ProductVersion));
    }
}
