// Copyright © Roby Van Damme.

using DotBump.NuGet.DataModel.NuGetConfiguration;
using DotBump.NuGet.DataModel.PackageResolution;

namespace DotBump.NuGet.Interfaces;

internal interface IPackageSourceMappingResolver
{
    PackageSourceMappingResult Resolve(
        IReadOnlyCollection<string> packageIds,
        NuGetConfig nuGetConfiguration);
}
