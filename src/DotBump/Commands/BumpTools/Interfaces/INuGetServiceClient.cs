// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpTools.DataModel.Catalog;
using DotBump.Commands.BumpTools.DataModel.NuGetService;
using DotBump.Commands.BumpTools.DataModel.Registrations;

namespace DotBump.Commands.BumpTools.Interfaces;

internal interface INuGetServiceClient
{
    Task<IReadOnlyCollection<ServiceIndex>> GetServiceIndexesAsync(IReadOnlyCollection<string> sources);

    Task<RegistrationIndex?> GetPackageInformationAsync(IReadOnlyCollection<string> baseUrls, string packageId);

    Task<IEnumerable<NuGetCatalogPage>> GetRelevantDetailCatalogPagesAsync(
        IReadOnlyCollection<CatalogPage> catalogPages);
}
