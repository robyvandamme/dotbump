// Copyright © Roby Van Damme.

using DotBump.Commands.BumpPackages.DataModel;

namespace DotBump.Commands.BumpPackages.Interfaces;

internal interface IPackageFileService
{
    PackageManifest GetPackageManifest(
        string repositoryPath,
        IReadOnlyCollection<string>? excludedPaths = null);

    void SavePackageManifest(PackageManifest manifest);
}
