// Copyright © Roby Van Damme.

using DotBump.Commands.BumpPackages.DataModel;

namespace DotBump.Tests.TestHelpers;

internal static class TestPackageManifestFactory
{
    public static PackageManifest CreateManifest(params (string Id, string Version)[] packages)
    {
        return CreateManifestWithConditions(
            packages
                .Select(package => (package.Id, package.Version, (string?)null))
                .ToArray());
    }

    public static PackageManifest CreateManifestWithConditions(
        params (string Id, string Version, string? Condition)[] packages)
    {
        var manifest = new PackageManifest();

        foreach (var (id, version, condition) in packages)
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
                    Condition = condition,
                    VersionStart = 0,
                    VersionLength = version.Length,
                });
        }

        return manifest;
    }
}
