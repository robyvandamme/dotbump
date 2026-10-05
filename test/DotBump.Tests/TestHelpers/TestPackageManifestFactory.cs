// Copyright © Roby Van Damme.

using DotBump.Commands.BumpPackages.DataModel;

namespace DotBump.Tests.TestHelpers;

internal static class TestPackageManifestFactory
{
    public static PackageManifest CreateManifest(params (string Id, string Version)[] packages)
    {
        var manifest = new PackageManifest();

        foreach (var (id, version) in packages)
        {
            manifest.Add(
                new PackageVersionEntry
                {
                    PackageId = id,
                    OriginalVersion = version,
                    Version = version,
                    FilePath = $"{id}.csproj",
                    SourceKind = PackageSourceKind.Project,
                    ElementName = "PackageReference",
                    VersionStart = 0,
                    VersionLength = version.Length,
                });
        }

        return manifest;
    }
}
