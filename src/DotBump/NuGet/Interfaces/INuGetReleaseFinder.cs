// Copyright © Roby Van Damme.

using DotBump.Commands;
using DotBump.Common;
using DotBump.NuGet.DataModel.NuGetService;
using DotBump.NuGet.DataModel.Registrations;

namespace DotBump.NuGet.Interfaces;

internal interface INuGetReleaseFinder
{
    string GetRegistrationsBaseUrl(ServiceIndex serviceIndex);

    List<CatalogPage> TryFindNewReleaseCatalogPages(
        RegistrationIndex index,
        SemanticVersion currentVersion);

    SemanticVersion? TryFindNewVersionInCatalogPages(
        ICollection<CatalogPage> catalogPages,
        SemanticVersion currentVersion,
        BumpType bumpType);

    SemanticVersion? TryFindNewVersionInCatalogPages(
        ICollection<CatalogPage> catalogPages,
        SemanticVersion currentVersion,
        BumpType bumpType,
        bool allowPreRelease);
}
