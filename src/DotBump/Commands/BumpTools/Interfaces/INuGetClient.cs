// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpTools.DataModel.NuGetService;
using DotBump.Commands.BumpTools.DataModel.Registrations;

namespace DotBump.Commands.BumpTools.Interfaces;

internal interface INuGetClient
{
    Task<ServiceIndex> GetServiceIndexAsync(string packageSourceUrl);

    Task<RegistrationIndex?> GetPackageInformationAsync(string registrationBaseUrl, string packageId);

    Task<IReadOnlyCollection<CatalogPage>> GetRelevantCatalogPagesAsync(IReadOnlyCollection<CatalogPage> catalogPages);
}
