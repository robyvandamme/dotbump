// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpTools.DataModel.NuGetService;
using DotBump.Commands.BumpTools.DataModel.Registrations;
using DotBump.Commands.BumpTools.Interfaces;
using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpTools;

internal class NuGetReleaseFinder(ILogger logger) : INuGetReleaseFinder
{
    public IReadOnlyCollection<string> GetRegistrationsUrls(IReadOnlyCollection<ServiceIndex> serviceIndexes)
    {
        logger.MethodStart(nameof(NuGetReleaseFinder), nameof(GetRegistrationsUrls), serviceIndexes);

        ArgumentNullException.ThrowIfNull(serviceIndexes);

        var baseUrls = new List<string>();

        foreach (var nuGetServiceIndex in serviceIndexes)
        {
            var registrationResource = nuGetServiceIndex.Resources.FirstOrDefault(o => o.Type.Equals(
                "RegistrationsBaseUrl",
                StringComparison.OrdinalIgnoreCase));
            if (registrationResource != null)
            {
                baseUrls.Add(registrationResource.Id);
            }
        }

        logger.MethodReturn(nameof(NuGetReleaseFinder), nameof(GetRegistrationsUrls), baseUrls);

        return baseUrls;
    }

    public string GetRegistrationsBaseUrl(ServiceIndex serviceIndex)
    {
        logger.MethodStart(nameof(NuGetReleaseFinder), nameof(GetRegistrationsBaseUrl), serviceIndex);

        ArgumentNullException.ThrowIfNull(serviceIndex);

        var registrationResource = serviceIndex.Resources.FirstOrDefault(o => o.Type.Equals(
            "RegistrationsBaseUrl",
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

        // TODO: is there a difference between minor and patch here? I assume there should be....
        if (index.CatalogPages != null)
        {
            var results = index.CatalogPages.FindAll(o => o.UpperSemanticVersion > currentVersion
                                                          && o.LowerSemanticVersion.Major <= currentVersion.Major);

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
    public SemanticVersion? TryFindVersionInCatalogPages(
        ICollection<CatalogPage> catalogPages,
        SemanticVersion currentVersion,
        BumpType bumpType)
    {
        ArgumentNullException.ThrowIfNull(catalogPages);
        ArgumentNullException.ThrowIfNull(currentVersion);
        if (bumpType != BumpType.Patch && bumpType != BumpType.Minor)
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

        if (bumpType == BumpType.Minor)
        {
            return TryFindNewMinorOrPatchVersion(currentVersion, versions);
        }

        return TryFindNewPatchVersion(currentVersion, versions);
    }

    private static SemanticVersion? TryFindNewMinorOrPatchVersion(
        SemanticVersion currentVersion,
        List<SemanticVersion> versions)
    {
        if (currentVersion.IsPreRelease)
        {
            var availableNewVersions =
                versions.Where(o => o.Major == currentVersion.Major && o > currentVersion);
            var newestVersion = availableNewVersions.OrderByDescending(o => o).FirstOrDefault();
            return newestVersion;
        }
        else
        {
            var availableNewVersions =
                versions.Where(o => o.Major == currentVersion.Major && o > currentVersion && o.IsPreRelease == false);
            var newestVersion = availableNewVersions.OrderByDescending(o => o).FirstOrDefault();
            return newestVersion;
        }
    }

    private static SemanticVersion? TryFindNewPatchVersion(
        SemanticVersion currentVersion,
        List<SemanticVersion> versions)
    {
        if (currentVersion.IsPreRelease)
        {
            var availableNewVersions =
                versions.Where(o => o.Major == currentVersion.Major
                                    && o.Minor == currentVersion.Minor
                                    && o > currentVersion);
            var newestVersion = availableNewVersions.OrderByDescending(o => o).FirstOrDefault();
            return newestVersion;
        }
        else
        {
            var availableNewVersions =
                versions.Where(o => o.Major == currentVersion.Major
                                    && o.Minor == currentVersion.Minor
                                    && o > currentVersion && o.IsPreRelease == false);
            var newestVersion = availableNewVersions.OrderByDescending(o => o).FirstOrDefault();
            return newestVersion;
        }
    }
}
