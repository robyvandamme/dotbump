// Copyright © Roby Van Damme.

using DotBump.Common;

namespace DotBump.NuGet.DataModel.PackageResolution;

/// <summary>
/// Represents the outcome of resolving newer versions for a set of packages, including any warnings
/// raised while applying package source mapping.
/// </summary>
/// <param name="Versions">The resolved newer versions, keyed by the package input.</param>
/// <param name="Warnings">The warnings raised while resolving the versions.</param>
internal record PackageResolutionResult(
    IReadOnlyDictionary<PackageToBump, SemanticVersion> Versions,
    IReadOnlyList<string> Warnings);
