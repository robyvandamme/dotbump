// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpTools.DataModel.NuGetService;
using DotBump.Commands.BumpTools.DataModel.Registrations;
using DotBump.Commands.BumpTools.Interfaces;
using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpTools;

internal class NuGetReleaseService(ILogger logger) : INuGetReleaseService
{
    /// <summary>
    /// Gets the RegistrationsBaseUrl's for the current list of service indexes.
    /// </summary>
    /// <param name="serviceIndexes">The list of Nuget Services indices.</param>
    /// <returns>List of url's.</returns>
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

    /// <summary>
    /// Gets the RegistrationsBaseUr for the current service index.
    /// </summary>
    /// <param name="serviceIndex">The NuGet service index..</param>
    /// <returns>The base URL.</returns>
    public string GetRegistrationsUrl(ServiceIndex serviceIndex)
    {
        logger.MethodStart(nameof(NuGetReleaseService), nameof(GetRegistrationsUrl), serviceIndex);

        ArgumentNullException.ThrowIfNull(serviceIndex);

        var registrationResource = serviceIndex.Resources.FirstOrDefault(o => o.Type.Equals(
            "RegistrationsBaseUrl",
            StringComparison.OrdinalIgnoreCase));

        if (registrationResource != null)
        {
            logger.MethodReturn(nameof(NuGetReleaseService), nameof(GetRegistrationsUrl), registrationResource.Id);
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

        var results = index.CatalogPages.FindAll(o => o.UpperSemanticVersion > currentVersion
                                                      && o.LowerSemanticVersion.Major <= currentVersion.Major);

        return results;
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
                versions.AddRange(catalogPage.Items.Select(o => o.CatalogEntry.SemanticVersion));
            }
        }
        else
        {
            versions.AddRange(catalogPages.First().Items.Select(o => o.CatalogEntry.SemanticVersion));
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
