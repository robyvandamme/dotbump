// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpTools.DataModel.Catalog;
using DotBump.Commands.BumpTools.DataModel.NuGetService;
using DotBump.Commands.BumpTools.DataModel.Registrations;

namespace DotBump.Commands.BumpTools.Interfaces;

internal interface INuGetServiceClient
{
    Task<IReadOnlyCollection<ServiceIndex>> GetServiceIndexesAsync(IEnumerable<string> sources);

    Task<RegistrationIndex?> GetPackageInformationAsync(IEnumerable<string> baseUrls, string packageId);

    Task<IEnumerable<NuGetCatalogPage>> GetRelevantDetailCatalogPagesAsync(
        IEnumerable<CatalogPage> catalogPages);
}
