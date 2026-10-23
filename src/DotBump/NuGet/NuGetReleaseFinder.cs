// Copyright © Roby Van Damme.

using DotBump.Commands;
using DotBump.Common;
using DotBump.NuGet.DataModel.NuGetService;
using DotBump.NuGet.DataModel.Registrations;
using DotBump.NuGet.Interfaces;
using Serilog;

namespace DotBump.NuGet;

internal class NuGetReleaseFinder(ILogger logger) : INuGetReleaseFinder
{
    /// <summary>
    /// The registration resource types that include SemVer 2.0.0 packages, in order of preference.
    /// The unversioned <c>RegistrationsBaseUrl</c> on nuget.org points at the SemVer 1.0.0 hive, which
    /// omits versions with a dot-separated pre-release label (for example <c>1.0.0-rc.1</c>).
    /// </summary>
    private static readonly string[] s_semVer2RegistrationTypes =
    [
        "RegistrationsBaseUrl/Versioned",
        "RegistrationsBaseUrl/3.6.0",
    ];

    public string GetRegistrationsBaseUrl(ServiceIndex serviceIndex)
    {
        logger.MethodStart(nameof(NuGetReleaseFinder), nameof(GetRegistrationsBaseUrl), serviceIndex);

        ArgumentNullException.ThrowIfNull(serviceIndex);

        var registrationResource = serviceIndex.Resources.FirstOrDefault(o => s_semVer2RegistrationTypes
            .Contains(o.Type, StringComparer.OrdinalIgnoreCase));

        // Fall back to the SemVer 1.0.0 resources for feeds that only expose those (for example GitHub Packages).
        registrationResource ??= serviceIndex.Resources.FirstOrDefault(o => o.Type.Equals(
            "RegistrationsBaseUrl",
            StringComparison.OrdinalIgnoreCase));

        registrationResource ??= serviceIndex.Resources.FirstOrDefault(o => o.Type.StartsWith(
            "RegistrationsBaseUrl/",
            StringComparison.OrdinalIgnoreCase));

        if (registrationResource != null)
        {
            logger.MethodReturn(nameof(NuGetReleaseFinder), nameof(GetRegistrationsBaseUrl), registrationResource.Id);
            return registrationResource.Id;
        }

        throw new DotBumpException($"No RegistrationsBaseUrl found in the service index.");
    }

    public List<CatalogPage> TryFindNewReleaseCatalogPages(
        RegistrationIndex index,
        SemanticVersion currentVersion,
        BumpType bumpType)
    {
        ArgumentNullException.ThrowIfNull(index);
        ArgumentNullException.ThrowIfNull(currentVersion);

        if (index.CatalogPages != null)
        {
            // A major bump may cross the current major, so pages from higher major versions must be included.
            var results = index.CatalogPages.FindAll(o => o.UpperSemanticVersion > currentVersion
                                                          && (bumpType == BumpType.Major
                                                              || o.LowerSemanticVersion.Major <= currentVersion.Major));

            return results;
        }

        return new List<CatalogPage>();
    }

    /// <summary>
    /// Tries to find a new version in the provided catalog pages using the provided bump type.
    /// If the current version is a pre-release version then pre-release versions will be taken into account, otherwise
    /// pre-release versions will be excluded.
    /// </summary>
    /// <param name="catalogPages">The list of catalog pages.</param>
    /// <param name="currentVersion">The current version.</param>
    /// <param name="bumpType">The bump type.</param>
    /// <returns>A new version if one is found.</returns>
    public SemanticVersion? TryFindNewVersionInCatalogPages(
        ICollection<CatalogPage> catalogPages,
        SemanticVersion currentVersion,
        BumpType bumpType)
    {
        ArgumentNullException.ThrowIfNull(currentVersion);

        return TryFindNewVersionInCatalogPages(
            catalogPages,
            currentVersion,
            bumpType,
            currentVersion.IsPreRelease,
            false);
    }

    /// <summary>
    /// Tries to find a new version in the provided catalog pages using the provided bump type and pre-release allowance.
    /// </summary>
    /// <param name="catalogPages">The list of catalog pages.</param>
    /// <param name="currentVersion">The current version.</param>
    /// <param name="bumpType">The bump type.</param>
    /// <param name="allowPreRelease">Whether to include pre-release versions.</param>
    /// <returns>A new version if one is found.</returns>
    public SemanticVersion? TryFindNewVersionInCatalogPages(
        ICollection<CatalogPage> catalogPages,
        SemanticVersion currentVersion,
        BumpType bumpType,
        bool allowPreRelease)
    {
        return TryFindNewVersionInCatalogPages(
            catalogPages,
            currentVersion,
            bumpType,
            allowPreRelease,
            false);
    }

    /// <summary>
    /// Tries to find a new version in the provided catalog pages using the provided bump type, pre-release allowance
    /// and selection preference.
    /// </summary>
    /// <param name="catalogPages">The list of catalog pages.</param>
    /// <param name="currentVersion">The current version.</param>
    /// <param name="bumpType">The bump type.</param>
    /// <param name="allowPreRelease">Whether to include pre-release versions.</param>
    /// <param name="preferHighestVersion">Whether the highest eligible version should win instead of preferring stable.</param>
    /// <returns>A new version if one is found.</returns>
    public SemanticVersion? TryFindNewVersionInCatalogPages(
        ICollection<CatalogPage> catalogPages,
        SemanticVersion currentVersion,
        BumpType bumpType,
        bool allowPreRelease,
        bool preferHighestVersion)
    {
        ArgumentNullException.ThrowIfNull(catalogPages);
        ArgumentNullException.ThrowIfNull(currentVersion);
        if (bumpType != BumpType.Patch && bumpType != BumpType.Minor && bumpType != BumpType.Major)
        {
            throw new ArgumentOutOfRangeException(nameof(bumpType));
        }

        var versions = new List<SemanticVersion>();
        if (catalogPages.Count == 0)
        {
            return null;
        }

        if (catalogPages.Count > 1)
        {
            foreach (var catalogPage in catalogPages)
            {
                if (catalogPage.Items != null)
                {
                    // If listed is null (like in the GitHub feed, use true
                    var listedItems = catalogPage.Items.Where(o => o.CatalogEntry.Listed ?? true);
                    versions.AddRange(listedItems.Select(o => o.CatalogEntry.SemanticVersion));
                }
            }
        }
        else
        {
            // If listed is null (like in the GitHub feed, use true
            var packages = catalogPages.First().Items;
            if (packages != null)
            {
                var listedItems = packages.Where(o => o.CatalogEntry.Listed ?? true);
                versions.AddRange(listedItems.Select(o => o.CatalogEntry.SemanticVersion));
            }
        }

        return bumpType switch
        {
            BumpType.Minor => TryFindNewMinorOrPatchVersion(
                currentVersion,
                versions,
                allowPreRelease,
                preferHighestVersion),
            BumpType.Patch => TryFindNewPatchVersion(
                currentVersion,
                versions,
                allowPreRelease,
                preferHighestVersion),
            BumpType.Major => TryFindNewMajorVersion(
                currentVersion,
                versions,
                allowPreRelease,
                preferHighestVersion),
            _ => throw new ArgumentOutOfRangeException(nameof(bumpType)),
        };
    }

    private static SemanticVersion? SelectNewestVersion(
        IEnumerable<SemanticVersion> availableNewVersions,
        bool allowPreRelease,
        bool preferHighestVersion)
    {
        var candidates = availableNewVersions.ToList();

        if (preferHighestVersion)
        {
            return allowPreRelease
                ? candidates.OrderByDescending(o => o).FirstOrDefault()
                : candidates.Where(o => !o.IsPreRelease).OrderByDescending(o => o).FirstOrDefault();
        }

        var newestStable = candidates
            .Where(o => !o.IsPreRelease)
            .OrderByDescending(o => o)
            .FirstOrDefault();
        if (newestStable != null)
        {
            return newestStable;
        }

        return allowPreRelease
            ? candidates.OrderByDescending(o => o).FirstOrDefault()
            : null;
    }

    private SemanticVersion? TryFindNewMinorOrPatchVersion(
        SemanticVersion currentVersion,
        List<SemanticVersion> versions,
        bool allowPreRelease,
        bool preferHighestVersion)
    {
        logger.MethodStart(nameof(NuGetReleaseFinder), nameof(TryFindNewMinorOrPatchVersion));

        var result = SelectNewestVersion(
            versions.Where(o => o.Major == currentVersion.Major && o > currentVersion),
            allowPreRelease,
            preferHighestVersion);

        logger.MethodReturn(nameof(NuGetReleaseFinder), nameof(TryFindNewMinorOrPatchVersion), result);
        return result;
    }

    private SemanticVersion? TryFindNewPatchVersion(
        SemanticVersion currentVersion,
        List<SemanticVersion> versions,
        bool allowPreRelease,
        bool preferHighestVersion)
    {
        logger.MethodStart(nameof(NuGetReleaseFinder), nameof(TryFindNewPatchVersion));

        var result = SelectNewestVersion(
            versions.Where(o => o.Major == currentVersion.Major
                                && o.Minor == currentVersion.Minor
                                && o > currentVersion),
            allowPreRelease,
            preferHighestVersion);

        logger.MethodReturn(nameof(NuGetReleaseFinder), nameof(TryFindNewPatchVersion), result);
        return result;
    }

    private SemanticVersion? TryFindNewMajorVersion(
        SemanticVersion currentVersion,
        List<SemanticVersion> versions,
        bool allowPreRelease,
        bool preferHighestVersion)
    {
        logger.MethodStart(nameof(NuGetReleaseFinder), nameof(TryFindNewMajorVersion));

        // A major bump removes the major-version cap, so newer minor and patch versions are also valid targets.
        var result = SelectNewestVersion(
            versions.Where(o => o > currentVersion),
            allowPreRelease,
            preferHighestVersion);

        logger.MethodReturn(nameof(NuGetReleaseFinder), nameof(TryFindNewMajorVersion), result);
        return result;
    }
}
