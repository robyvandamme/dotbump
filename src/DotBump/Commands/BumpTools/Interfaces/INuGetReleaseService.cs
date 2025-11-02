// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpTools.DataModel.NuGetService;
using DotBump.Commands.BumpTools.DataModel.Registrations;
using DotBump.Common;

namespace DotBump.Commands.BumpTools.Interfaces;

internal interface INuGetReleaseService
{
    /// <summary>
    /// Gets the RegistrationsBaseUrl's for the current list of service indexes.
    /// </summary>
    /// <param name="serviceIndexes">The list of Nuget Services indices.</param>
    /// <returns>List of url's.</returns>
    IReadOnlyCollection<string> GetRegistrationsUrls(IReadOnlyCollection<ServiceIndex> serviceIndexes);

    List<CatalogPage> TryFindNewReleaseCatalogPages(
        RegistrationIndex index,
        SemanticVersion currentVersion);

    SemanticVersion? TryGetNewMinorOrPatchVersionFromCatalogPages(
        ICollection<CatalogPage> catalogPages,
        SemanticVersion currentVersion);

    // SemanticVersion? TryGetNewMinorOrPatchVersionFromDetailCatalogPages(
    //     ICollection<NuGetCatalogPage> catalogPages,
    //     SemanticVersion currentVersion);

    /// <summary>
    /// Gets the RegistrationsBaseUr for the current service index.
    /// </summary>
    /// <param name="serviceIndex">The NuGet service index..</param>
    /// <returns>The base URL.</returns>
    string GetRegistrationsUrl(ServiceIndex serviceIndex);
}
