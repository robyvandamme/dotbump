// Copyright © Roby Van Damme.

using DotBump.Commands.BumpPackages.DataModel;
using Shouldly;

namespace DotBump.Tests.Commands.BumpPackages;

public class PackageVariantTests
{
    public class HighestOriginalVersion
    {
        [Fact]
        public void With_Valid_Versions_Returns_Highest_Original_Version()
        {
            var variant = CreateVariant(
                CreateEntry("1.2.0", "1.2.0"),
                CreateEntry("1.5.0", "1.5.0"),
                CreateEntry("1.3.0", "1.3.0"));

            var highestVersion = variant.HighestOriginalVersion;

            highestVersion.ShouldNotBeNull().Version.ShouldBe("1.5.0");
        }

        [Fact]
        public void With_Mixed_Valid_And_Invalid_Versions_Returns_Highest_Valid_Version()
        {
            var variant = CreateVariant(
                CreateEntry("1.2.3.beta", "1.2.3.beta"),
                CreateEntry("1.4.0", "1.4.0"),
                CreateEntry("not-a-version", "not-a-version"));

            var highestVersion = variant.HighestOriginalVersion;

            highestVersion.ShouldNotBeNull().Version.ShouldBe("1.4.0");
        }

        [Fact]
        public void With_No_Valid_Versions_Returns_Null()
        {
            var variant = CreateVariant(
                CreateEntry("1.2.3.beta", "1.2.3.beta"),
                CreateEntry("not-a-version", "not-a-version"));

            variant.HighestOriginalVersion.ShouldBeNull();
        }

        [Fact]
        public void With_Current_Version_Different_From_Original_Uses_Original_Version()
        {
            var variant = CreateVariant(CreateEntry("1.0.0", "2.0.0"));

            variant.HighestOriginalVersion.ShouldNotBeNull().Version.ShouldBe("1.0.0");
        }
    }

    public class HighestCurrentVersion
    {
        [Fact]
        public void With_Valid_Versions_Returns_Highest_Current_Version()
        {
            var variant = CreateVariant(
                CreateEntry("1.0.0", "1.2.0"),
                CreateEntry("1.1.0", "1.5.0"),
                CreateEntry("1.3.0", "1.3.0"));

            var highestVersion = variant.HighestCurrentVersion;

            highestVersion.ShouldNotBeNull().Version.ShouldBe("1.5.0");
        }

        [Fact]
        public void With_No_Valid_Versions_Returns_Null()
        {
            var variant = CreateVariant(
                CreateEntry("1.2.3.beta", "1.2.3.beta"),
                CreateEntry("not-a-version", "not-a-version"));

            variant.HighestCurrentVersion.ShouldBeNull();
        }
    }

    private static PackageVariant CreateVariant(params PackageVersionEntry[] entries)
    {
        return new PackageVariant("MyPackage", null, entries);
    }

    private static PackageVersionEntry CreateEntry(string originalVersion, string currentVersion)
    {
        return new PackageVersionEntry
        {
            PackageId = "MyPackage",
            OriginalVersion = originalVersion,
            Version = currentVersion,
            FilePath = "MyPackage.csproj",
            SourceKind = PackageSourceKind.Project,
            ElementName = "PackageReference",
            VersionStart = 0,
            VersionLength = currentVersion.Length,
        };
    }
}
