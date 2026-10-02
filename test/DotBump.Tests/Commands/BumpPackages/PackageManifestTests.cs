// Copyright © Roby Van Damme.

using DotBump.Commands.BumpPackages.DataModel;
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

    public class SetVersion
    {
        [Fact]
        public void With_Paths_Differing_Only_By_Case_Tracks_Both_As_Changed()
        {
            var manifest = new PackageManifest();
            manifest.Add(CreateEntry("/repo/A.csproj", "MyPackage", "1.0.0"));
            manifest.Add(CreateEntry("/repo/a.csproj", "MyPackage", "1.0.0"));

            manifest.SetVersion("MyPackage", "1.1.0");

            manifest.ChangedFiles.ShouldBe(["/repo/A.csproj", "/repo/a.csproj"], ignoreOrder: true);
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

    private static PackageVersionEntry CreateEntry(string filePath, string packageId, string version)
    {
        return new PackageVersionEntry
        {
            PackageId = packageId,
            OriginalVersion = version,
            Version = version,
            FilePath = filePath,
            SourceKind = PackageSourceKind.Project,
            ElementName = "PackageReference",
            VersionStart = 0,
            VersionLength = version.Length,
        };
    }
}
