// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpTools.DataModel.NuGetService;
using DotBump.Commands.BumpTools.DataModel.Registrations;
using DotBump.Common;

namespace DotBump.Commands.BumpTools.Interfaces;

internal interface INuGetReleaseFinder
{
    string GetRegistrationsBaseUrl(ServiceIndex serviceIndex);

    List<CatalogPage> TryFindNewReleaseCatalogPages(
        RegistrationIndex index,
        SemanticVersion currentVersion,
        BumpType bumpType);

    SemanticVersion? TryFindVersionInCatalogPages(
        ICollection<CatalogPage> catalogPages,
        SemanticVersion currentVersion,
        BumpType bumpType);
}
