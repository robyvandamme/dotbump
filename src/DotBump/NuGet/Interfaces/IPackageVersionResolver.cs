// Copyright © Roby Van Damme.

using DotBump.Commands;
using DotBump.Common;
using DotBump.NuGet.DataModel.NuGetConfiguration;
using DotBump.NuGet.DataModel.PackageResolution;

namespace DotBump.NuGet.Interfaces;

internal interface IPackageVersionResolver
{
    /// <summary>
    /// Resolves the best newer version for each package across all configured package sources.
    /// </summary>
    /// <param name="packages">The packages and their current versions.</param>
    /// <param name="bumpType">The bump type.</param>
    /// <param name="nuGetConfiguration">The NuGet configuration with the package sources to query.</param>
    /// <returns>
    /// A dictionary keyed by package identifier containing the resolved newer version.
    /// Packages for which no newer version was found are not included.
    /// </returns>
    Task<IReadOnlyDictionary<string, SemanticVersion>> ResolveAsync(
        IReadOnlyCollection<PackageToBump> packages,
        BumpType bumpType,
        NuGetConfig nuGetConfiguration);
}
