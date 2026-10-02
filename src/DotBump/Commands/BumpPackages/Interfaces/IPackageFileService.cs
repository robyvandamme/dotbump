// Copyright © Roby Van Damme.

using DotBump.Commands.BumpPackages.DataModel;

namespace DotBump.Commands.BumpPackages.Interfaces;

internal interface IPackageFileService
{
    PackageManifest GetPackageManifest(string repositoryPath);

    void SavePackageManifest(PackageManifest manifest);
}
