// Copyright © Roby Van Damme.

using DotBump.Commands.BumpPackages.DataModel;
using DotBump.Tests.TestHelpers;
using Shouldly;

namespace DotBump.Tests.Commands.BumpPackages;

public class PackageManifestTests
{
    public class RegisterFileText
    {
        [Fact]
        public void With_Paths_Differing_Only_By_Case_Keeps_Both_Texts()
        {
            var manifest = new PackageManifest();

            manifest.RegisterFileText("/repo/A.csproj", "upper");
            manifest.RegisterFileText("/repo/a.csproj", "lower");

            manifest.GetFileText("/repo/A.csproj").ShouldBe("upper");
            manifest.GetFileText("/repo/a.csproj").ShouldBe("lower");
        }
    }

    public class GetVariants
    {
        [Fact]
        public void With_Same_Id_And_Condition_Returns_Single_Variant()
        {
            var manifest = new PackageManifest();
            manifest.Add(CreateEntry("/repo/A.csproj", "MyPackage", "1.0.0", "net10.0"));
            manifest.Add(CreateEntry("/repo/B.csproj", "mypackage", "1.2.0", "net10.0"));

            var variant = manifest.GetVariants().ShouldHaveSingleItem();

            variant.ShouldSatisfyAllConditions(
                () => variant.PackageId.ShouldBe("MyPackage"),
                () => variant.Condition.ShouldBe("net10.0"),
                () => variant.Occurrences.Count.ShouldBe(2));
        }

        [Fact]
        public void With_Same_Id_And_Different_Conditions_Returns_Separate_Variants()
        {
            var manifest = new PackageManifest();
            manifest.Add(CreateEntry("/repo/A.csproj", "MyPackage", "10.0.14", "net10.0"));
            manifest.Add(CreateEntry("/repo/A.csproj", "MyPackage", "11.0.0-rc.3", "net11.0"));
            manifest.Add(CreateEntry("/repo/A.csproj", "MyPackage", "12.0.0"));

            var variants = manifest.GetVariants();

            variants.Count.ShouldBe(3);
            variants.Select(variant => variant.Condition).ShouldBe(
                ["net10.0", "net11.0", null],
                ignoreOrder: true);
        }

        [Fact]
        public void With_Null_And_Empty_Condition_Returns_Single_Variant()
        {
            var manifest = new PackageManifest();
            manifest.Add(CreateEntry("/repo/A.csproj", "MyPackage", "1.0.0"));
            manifest.Add(CreateEntry("/repo/B.csproj", "MyPackage", "1.2.0", string.Empty));

            var variant = manifest.GetVariants().ShouldHaveSingleItem();

            variant.Condition.ShouldBeNull();
            variant.Occurrences.Count.ShouldBe(2);
        }

        [Fact]
        public void With_No_Packages_Returns_Empty_List()
        {
            var manifest = new PackageManifest();

            manifest.GetVariants().ShouldBeEmpty();
        }
    }

    public class SetVersion
    {
        [Fact]
        public void With_Paths_Differing_Only_By_Case_Tracks_Both_As_Changed()
        {
            var manifest = new PackageManifest();
            manifest.Add(CreateEntry("/repo/A.csproj", "MyPackage", "1.0.0"));
            manifest.Add(CreateEntry("/repo/a.csproj", "MyPackage", "1.0.0"));

            manifest.SetVersion(manifest.GetVariants().ShouldHaveSingleItem(), "1.1.0");

            manifest.ChangedFiles.ShouldBe(["/repo/A.csproj", "/repo/a.csproj"], ignoreOrder: true);
        }

        [Fact]
        public void With_Conditional_Variants_Updates_Only_The_Targeted_Variant()
        {
            var manifest = new PackageManifest();
            manifest.Add(CreateEntry("/repo/A.csproj", "MyPackage", "10.0.14", "net10.0"));
            manifest.Add(CreateEntry("/repo/A.csproj", "MyPackage", "11.0.0-rc.3", "net11.0"));

            var target = manifest.GetVariants().Single(variant => variant.Condition == "net10.0");
            manifest.SetVersion(target, "10.0.15");

            manifest.Packages.ShouldSatisfyAllConditions(
                () => manifest.Packages.Single(package => package.Condition == "net10.0").Version.ShouldBe("10.0.15"),
                () => manifest.Packages.Single(package => package.Condition == "net11.0").Version.ShouldBe("11.0.0-rc.3"));
        }
    }

    public class GetPackageIds
    {
        [Fact]
        public void With_Ids_Differing_Only_By_Case_Returns_Single_Id()
        {
            var manifest = new PackageManifest();
            manifest.Add(CreateEntry("/repo/A.csproj", "MyPackage", "1.0.0"));
            manifest.Add(CreateEntry("/repo/B.csproj", "mypackage", "1.0.0"));

            manifest.GetPackageIds().ShouldBe(["MyPackage"]);
        }
    }

    private static PackageVersionEntry CreateEntry(
        string filePath,
        string packageId,
        string version,
        string? condition = null)
    {
        return new PackageVersionEntry
        {
            PackageId = packageId,
            OriginalVersion = version,
            Version = version,
            FilePath = filePath,
            SourceKind = PackageSourceKind.Project,
            ElementName = "PackageReference",
            Condition = condition,
            VersionStart = 0,
            VersionLength = version.Length,
        };
    }
}
