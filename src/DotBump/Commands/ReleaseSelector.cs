// Copyright © Roby Van Damme.

using DotBump.Common;

namespace DotBump.Commands;

/// <summary>
/// Selects the release to bump to from a set of versioned candidates, applying the shared
/// minor/patch/major and pre-release rules.
/// </summary>
internal static class ReleaseSelector
{
    /// <summary>
    /// Finds the newest candidate that matches the bump type.
    /// </summary>
    /// <typeparam name="T">The candidate release type.</typeparam>
    /// <param name="currentVersion">The version currently in use.</param>
    /// <param name="candidates">The available releases.</param>
    /// <param name="versionSelector">Selects the version to compare for a candidate.</param>
    /// <param name="bumpType">The bump type.</param>
    /// <param name="preRelease">Whether pre-release versions may be selected for a major bump.</param>
    /// <returns>The selected candidate, or <see langword="null"/> when nothing newer is available.</returns>
    internal static T? TryFindNewest<T>(
        SemanticVersion currentVersion,
        IEnumerable<T> candidates,
        Func<T, SemanticVersion?> versionSelector,
        BumpType bumpType,
        bool preRelease)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(currentVersion);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(versionSelector);

        var available = candidates
            .Select(candidate => TryCreateCandidate(candidate, versionSelector(candidate)))
            .OfType<VersionedCandidate<T>>()
            .ToList();

        return bumpType switch
        {
            BumpType.Minor => FindNewest(
                available,
                candidate => candidate.Version.Major == currentVersion.Major
                    && candidate.Version > currentVersion),
            BumpType.Patch => FindNewest(
                available,
                candidate => candidate.Version.Major == currentVersion.Major
                    && candidate.Version.Minor == currentVersion.Minor
                    && candidate.Version.Patch >= currentVersion.Patch
                    && candidate.Version > currentVersion),
            BumpType.Major when preRelease => FindNewest(
                available,
                candidate => candidate.Version > currentVersion),
            BumpType.Major => FindNewestStable(available, currentVersion),
            _ => throw new ArgumentOutOfRangeException(nameof(bumpType), bumpType, null),
        };
    }

    private static T? FindNewest<T>(
        IEnumerable<VersionedCandidate<T>> candidates,
        Func<VersionedCandidate<T>, bool> predicate)
        where T : class
    {
        return candidates
            .Where(predicate)
            .OrderByDescending(candidate => candidate.Version)
            .Select(candidate => candidate.Candidate)
            .FirstOrDefault();
    }

    private static T? FindNewestStable<T>(
        IEnumerable<VersionedCandidate<T>> candidates,
        SemanticVersion currentVersion)
        where T : class
    {
        var newerCandidates = candidates
            .Where(candidate => candidate.Version > currentVersion)
            .ToList();

        var newestStable = newerCandidates
            .Where(candidate => !candidate.Version.IsPreRelease)
            .OrderByDescending(candidate => candidate.Version)
            .Select(candidate => candidate.Candidate)
            .FirstOrDefault();

        if (newestStable != null)
        {
            return newestStable;
        }

        return currentVersion.IsPreRelease
            ? newerCandidates
                .OrderByDescending(candidate => candidate.Version)
                .Select(candidate => candidate.Candidate)
                .FirstOrDefault()
            : null;
    }

    private static VersionedCandidate<T>? TryCreateCandidate<T>(
        T candidate,
        SemanticVersion? version)
        where T : class
    {
        return version is { IsValid: true } ? new VersionedCandidate<T>(candidate, version) : null;
    }

    private readonly record struct VersionedCandidate<T>(T Candidate, SemanticVersion Version)
        where T : class;
}
