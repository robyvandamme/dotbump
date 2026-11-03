// Copyright © Roby Van Damme.

using DotBump.NuGet.DataModel.NuGetConfiguration;

namespace DotBump.NuGet.DataModel.PackageResolution;

/// <summary>
/// Represents the package sources selected for a package, together with the packageSourceMapping
/// pattern that selected them.
/// </summary>
/// <param name="Sources">The package sources that may be queried for the package.</param>
/// <param name="MatchedPattern">
/// The packageSourceMapping pattern that matched the package, or <see langword="null"/> when no
/// package source mapping is configured.
/// </param>
internal record PackageSourceMappingMatch(
    IReadOnlyList<PackageSource> Sources,
    string? MatchedPattern);
