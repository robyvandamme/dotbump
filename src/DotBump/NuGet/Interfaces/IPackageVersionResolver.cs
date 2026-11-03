// Copyright © Roby Van Damme.

using DotBump.Commands;
using DotBump.Common;
using DotBump.NuGet.DataModel.NuGetConfiguration;
using DotBump.NuGet.DataModel.PackageResolution;

namespace DotBump.NuGet.Interfaces;

internal interface IPackageVersionResolver
{
    /// <summary>
    /// Resolves the best newer version for each package using the package sources selected by
    /// <c>packageSourceMapping</c>, or all configured package sources when no mapping is configured.
    /// </summary>
    /// <param name="packages">The packages and their current versions.</param>
    /// <param name="bumpType">The bump type.</param>
    /// <param name="nuGetConfiguration">The NuGet configuration with the package sources to query.</param>
    /// <param name="preRelease">Whether pre-release versions should be included for a major bump.</param>
    /// <returns>
    /// The resolved newer versions keyed by the resolved package input (package identifier and current
    /// version), together with any warnings raised while applying package source mapping. Inputs with
    /// equal keys are collapsed: package ids are compared case-insensitively and current versions use
    /// semantic equality, so for example <c>1.0.0</c> and <c>1.0.0.0</c> are the same input. Packages
    /// for which no newer version was found are not included.
    /// </returns>
    Task<PackageResolutionResult> ResolveAsync(
        IReadOnlyCollection<PackageToBump> packages,
        BumpType bumpType,
        NuGetConfig nuGetConfiguration,
        bool preRelease = false);
}
