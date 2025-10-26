// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpTools.DataModel.NuGetService;
using DotBump.Commands.BumpTools.DataModel.Registrations;
using DotBump.Commands.BumpTools.Interfaces;
using DotBump.Common;

namespace DotBump.Commands.BumpTools;

internal class NuGetReleaseService : INuGetReleaseService
{
    /// <summary>
    /// Gets the RegistrationsBaseUrl's for the current list of service indexes.
    /// </summary>
    /// <param name="serviceIndexes">The list of Nuget Services indices.</param>
    /// <returns>List of url's.</returns>
    public List<string> GetRegistrationsUrls(List<ServiceIndex> serviceIndexes)
    {
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

        return baseUrls;
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
}
