// Copyright © Roby Van Damme.

using DotBump.NuGet.DataModel.NuGetConfiguration;
using DotBump.NuGet.DataModel.PackageResolution;

namespace DotBump.NuGet.Interfaces;

internal interface IPackageSourceMappingResolver
{
    /// <summary>
    /// Resolves the package sources that may be queried for each package identifier.
    /// </summary>
    /// <param name="packageIds">The package identifiers to resolve.</param>
    /// <param name="nuGetConfiguration">The NuGet configuration containing the package source mapping.</param>
    /// <returns>The package source mapping result, including any warnings.</returns>
    PackageSourceMappingResult Resolve(
        IReadOnlyCollection<string> packageIds,
        NuGetConfig nuGetConfiguration);
}
