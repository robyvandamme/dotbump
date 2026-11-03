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
    /// <inheritdoc />
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
        return FindExactMatch(packageId, mappings) ?? FindPrefixMatch(packageId, mappings);
    }

    private static WinningPattern? FindExactMatch(
        string packageId,
        IReadOnlyList<PackageSourceMappingEntry> mappings)
    {
        string? exactPattern = null;
        var sourceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in mappings)
        {
            foreach (var pattern in mapping.Patterns)
            {
                if (!IsExactMatch(packageId, pattern))
                {
                    continue;
                }

                exactPattern ??= pattern;
                sourceKeys.Add(mapping.SourceKey);
            }
        }

        if (exactPattern == null)
        {
            return null;
        }

        return new WinningPattern(exactPattern, sourceKeys);
    }

    private static bool IsExactMatch(string packageId, string pattern)
    {
        return !pattern.Contains('*')
            && pattern.Equals(packageId, StringComparison.OrdinalIgnoreCase);
    }

    private static WinningPattern? FindPrefixMatch(
        string packageId,
        IReadOnlyList<PackageSourceMappingEntry> mappings)
    {
        var candidates = GetPrefixCandidates(packageId, mappings).ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        var longestPrefixLength = candidates.Max(o => o.Prefix.Length);
        var sourceKeys = new HashSet<string>(
            candidates
                .Where(o => o.Prefix.Length == longestPrefixLength)
                .Select(o => o.SourceKey),
            StringComparer.OrdinalIgnoreCase);

        var winningPattern = candidates
            .First(o => o.Prefix.Length == longestPrefixLength)
            .Prefix + "*";

        return new WinningPattern(winningPattern, sourceKeys);
    }

    private static IEnumerable<PrefixCandidate> GetPrefixCandidates(
        string packageId,
        IReadOnlyList<PackageSourceMappingEntry> mappings)
    {
        foreach (var mapping in mappings)
        {
            foreach (var pattern in mapping.Patterns)
            {
                var prefix = GetMatchingPrefix(packageId, pattern);
                if (prefix != null)
                {
                    yield return new PrefixCandidate(prefix, mapping.SourceKey);
                }
            }
        }
    }

    private static string? GetMatchingPrefix(string packageId, string pattern)
    {
        var wildcardIndex = pattern.IndexOf('*');
        if (wildcardIndex < 0)
        {
            return null;
        }

        // NuGet treats a '*' as the end of the prefix pattern, even when other characters
        // follow it, so only the part before the first '*' participates in the match.
        var prefix = pattern[..wildcardIndex];
        return packageId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? prefix : null;
    }

    private sealed record PrefixCandidate(string Prefix, string SourceKey);

    private sealed record WinningPattern(string Pattern, HashSet<string> SourceKeys);
}
