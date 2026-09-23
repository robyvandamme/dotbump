// Copyright © Roby Van Damme.

using DotBump.NuGet.DataModel.NuGetService;
using DotBump.NuGet.DataModel.Registrations;

namespace DotBump.NuGet.Interfaces;

internal interface INuGetClient : IDisposable
{
    Task<ServiceIndex> GetServiceIndexAsync(string packageSourceUrl);

    Task<RegistrationIndex?> GetPackageInformationAsync(string registrationBaseUrl, string packageId);

    Task<IReadOnlyCollection<CatalogPage>> GetRelevantCatalogPagesAsync(IReadOnlyCollection<CatalogPage> catalogPages);
}
