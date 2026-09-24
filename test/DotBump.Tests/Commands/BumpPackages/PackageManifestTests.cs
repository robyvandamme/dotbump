// Copyright © Roby Van Damme.

using System.Xml.Linq;
using DotBump.Commands.BumpPackages.DataModel;
using Shouldly;

namespace DotBump.Tests.Commands.BumpPackages;

public class PackageManifestTests
{
    public class RegisterDocument
    {
        [Fact]
        public void With_Paths_Differing_Only_By_Case_Keeps_Both_Documents()
        {
            var manifest = new PackageManifest();
            var upperDocument = new XDocument(new XElement("Project", new XAttribute("name", "upper")));
            var lowerDocument = new XDocument(new XElement("Project", new XAttribute("name", "lower")));

            manifest.RegisterDocument("/repo/A.csproj", upperDocument);
            manifest.RegisterDocument("/repo/a.csproj", lowerDocument);

            manifest.GetDocument("/repo/A.csproj").ShouldBeSameAs(upperDocument);
            manifest.GetDocument("/repo/a.csproj").ShouldBeSameAs(lowerDocument);
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
        var element = XElement.Parse($"<PackageReference Include=\"{packageId}\" Version=\"{version}\" />");
        return new PackageVersionEntry
        {
            PackageId = packageId,
            OriginalVersion = version,
            Version = version,
            FilePath = filePath,
            SourceKind = PackageSourceKind.Project,
            ElementName = "PackageReference",
            Element = element,
            VersionAttribute = element.Attribute("Version"),
        };
    }
}
