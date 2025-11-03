// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpTools.DataModel.NuGetService;
using DotBump.Commands.BumpTools.DataModel.Registrations;
using DotBump.Commands.BumpTools.Interfaces;
using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpTools;

internal class NuGetReleaseService(ILogger logger) : INuGetReleaseService
{
    public IReadOnlyCollection<string> GetRegistrationsUrls(IReadOnlyCollection<ServiceIndex> serviceIndexes)
    {
        logger.MethodStart(nameof(NuGetReleaseService), nameof(GetRegistrationsUrls), serviceIndexes);

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

        logger.MethodReturn(nameof(NuGetReleaseService), nameof(GetRegistrationsUrls), baseUrls);

        return baseUrls;
    }

    public string GetRegistrationsBaseUrl(ServiceIndex serviceIndex)
    {
        logger.MethodStart(nameof(NuGetReleaseService), nameof(GetRegistrationsBaseUrl), serviceIndex);

        ArgumentNullException.ThrowIfNull(serviceIndex);

        var registrationResource = serviceIndex.Resources.FirstOrDefault(o => o.Type.Equals(
            "RegistrationsBaseUrl",
            StringComparison.OrdinalIgnoreCase));

        if (registrationResource != null)
        {
            logger.MethodReturn(nameof(NuGetReleaseService), nameof(GetRegistrationsBaseUrl), registrationResource.Id);
            return registrationResource.Id;
        }

        throw new DotBumpException($"No RegistrationsBaseUrl found in the service index.");
    }

    public List<CatalogPage> TryFindNewReleaseCatalogPages(
        RegistrationIndex index,
        SemanticVersion currentVersion)
    {
        ArgumentNullException.ThrowIfNull(index);
        ArgumentNullException.ThrowIfNull(currentVersion);

        if (index.CatalogPages != null)
        {
            var results = index.CatalogPages.FindAll(o => o.UpperSemanticVersion > currentVersion
                                                          && o.LowerSemanticVersion.Major <= currentVersion.Major);

            return results;
        }

        return new List<CatalogPage>();
    }

    public SemanticVersion? TryGetNewMinorOrPatchVersionFromCatalogPages(
        ICollection<CatalogPage> catalogPages,
        SemanticVersion currentVersion)
    {
        ArgumentNullException.ThrowIfNull(catalogPages);
        ArgumentNullException.ThrowIfNull(currentVersion);

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
}
