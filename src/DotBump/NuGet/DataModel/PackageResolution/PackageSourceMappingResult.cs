// Copyright © Roby Van Damme.

namespace DotBump.NuGet.DataModel.PackageResolution;

/// <summary>
/// Represents the result of applying package source mapping to a set of package identifiers.
/// </summary>
/// <param name="Matches">
/// The package source mapping matches, keyed by package identifier using case-insensitive comparison.
/// </param>
/// <param name="Warnings">The warnings raised while applying the package source mapping.</param>
internal record PackageSourceMappingResult(
    IReadOnlyDictionary<string, PackageSourceMappingMatch> Matches,
    IReadOnlyList<string> Warnings);
