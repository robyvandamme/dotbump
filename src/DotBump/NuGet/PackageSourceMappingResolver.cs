// Copyright © Roby Van Damme.

using DotBump.NuGet.DataModel.NuGetConfiguration;
using DotBump.NuGet.DataModel.PackageResolution;
using DotBump.NuGet.Interfaces;

namespace DotBump.NuGet;

/// <summary>
/// Applies the <c>packageSourceMapping</c> section of a NuGet configuration to package identifiers.
/// The matching rules follow NuGet: exact package id patterns have the highest precedence, followed
/// by the longest matching prefix pattern, with <c>*</c> having the lowest precedence. All package
/// sources that declare the winning pattern are returned.
/// </summary>
internal sealed class PackageSourceMappingResolver : IPackageSourceMappingResolver
{
    /// <summary>
    /// Resolves the package sources that may be queried for each package identifier.
    /// </summary>
    /// <param name="packageIds">The package identifiers to resolve.</param>
    /// <param name="nuGetConfiguration">The NuGet configuration containing the package source mapping.</param>
    /// <returns>The package source mapping result, including any warnings.</returns>
    public PackageSourceMappingResult Resolve(
        IReadOnlyCollection<string> packageIds,
        NuGetConfig nuGetConfiguration)
    {
        ArgumentNullException.ThrowIfNull(packageIds);
        ArgumentNullException.ThrowIfNull(nuGetConfiguration);

        var distinctPackageIds = packageIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var matches = new Dictionary<string, PackageSourceMappingMatch>(StringComparer.OrdinalIgnoreCase);
        var warnings = new List<string>();

        if (nuGetConfiguration.PackageSourceMappings.Count == 0)
        {
            var allSources = nuGetConfiguration.PackageSources.ToList();
            foreach (var packageId in distinctPackageIds)
            {
                matches[packageId] = new PackageSourceMappingMatch(allSources, null);
            }

            return new PackageSourceMappingResult(matches, warnings);
        }

        var knownSourceKeys = new HashSet<string>(
            nuGetConfiguration.PackageSources.Select(o => o.Key),
            StringComparer.OrdinalIgnoreCase);

        var reportedUnknownSourceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in nuGetConfiguration.PackageSourceMappings)
        {
            if (!knownSourceKeys.Contains(mapping.SourceKey)
                && reportedUnknownSourceKeys.Add(mapping.SourceKey))
            {
                warnings.Add(
                    $"The packageSourceMapping entry for package source '{mapping.SourceKey}' does not match " +
                    "any package source in packageSources; it is ignored.");
            }
        }

        foreach (var packageId in distinctPackageIds)
        {
            var winningPattern = FindWinningPattern(packageId, nuGetConfiguration.PackageSourceMappings);
            if (winningPattern == null)
            {
                warnings.Add(
                    $"Package '{packageId}' does not match any packageSourceMapping pattern; skipping it.");
                matches[packageId] = new PackageSourceMappingMatch([], null);
                continue;
            }

            var sources = nuGetConfiguration.PackageSources
                .Where(source => winningPattern.SourceKeys.Contains(source.Key))
                .ToList();

            if (sources.Count == 0)
            {
                var undefinedSourceKeys = string.Join(
                    ", ",
                    winningPattern.SourceKeys.OrderBy(o => o, StringComparer.OrdinalIgnoreCase));
                warnings.Add(
                    $"Package '{packageId}' is only mapped to undefined package source(s) " +
                    $"'{undefinedSourceKeys}'; skipping it.");
            }

            matches[packageId] = new PackageSourceMappingMatch(sources, winningPattern.Pattern);
        }

        return new PackageSourceMappingResult(matches, warnings);
    }

    private static WinningPattern? FindWinningPattern(
        string packageId,
        IReadOnlyList<PackageSourceMappingEntry> mappings)
    {
        string? exactPattern = null;
        var exactSourceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in mappings)
        {
            foreach (var pattern in mapping.Patterns)
            {
                if (pattern.Contains('*') || !pattern.Equals(packageId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                exactPattern ??= pattern;
                exactSourceKeys.Add(mapping.SourceKey);
            }
        }

        if (exactSourceKeys.Count > 0)
        {
            return new WinningPattern(exactPattern!, exactSourceKeys);
        }

        var winningPattern = (string?)null;
        var winningPrefixLength = -1;
        var prefixSourceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in mappings)
        {
            foreach (var pattern in mapping.Patterns)
            {
                var wildcardIndex = pattern.IndexOf('*');
                if (wildcardIndex < 0)
                {
                    continue;
                }

                // NuGet treats a '*' as the end of the prefix pattern, even when other characters
                // follow it, so only the part before the first '*' participates in the match.
                var prefix = pattern[..wildcardIndex];
                if (!packageId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (prefix.Length > winningPrefixLength)
                {
                    winningPrefixLength = prefix.Length;
                    winningPattern = pattern;
                    prefixSourceKeys.Clear();
                }

                if (prefix.Length == winningPrefixLength)
                {
                    prefixSourceKeys.Add(mapping.SourceKey);
                }
            }
        }

        if (winningPattern == null)
        {
            return null;
        }

        return new WinningPattern(winningPattern, prefixSourceKeys);
    }

    private sealed record WinningPattern(string Pattern, HashSet<string> SourceKeys);
}
