// Copyright © 2025 Roby Van Damme.

using System.Text.Json;
using DotBump.Commands.BumpTools.DataModel.Catalog;
using DotBump.Commands.BumpTools.DataModel.NuGetService;
using DotBump.Commands.BumpTools.DataModel.Registrations;
using DotBump.Commands.BumpTools.Interfaces;

namespace DotBump.Tests.Commands.BumpTools.Fakes;

internal class FakeNuGetServiceClient : INuGetServiceClient
{
    public async Task<IReadOnlyCollection<ServiceIndex>> GetServiceIndexesAsync(ICollection<string> sources)
    {
        var result = new List<ServiceIndex>();
        var filePath = Directory.GetCurrentDirectory() + "/Data/NuGet/nuget-service-index.json";
        var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        var serviceIndex = JsonSerializer.Deserialize<ServiceIndex>(json);
        if (serviceIndex != null)
        {
            result.Add(serviceIndex);
        }

        return result;
    }

    public async Task<IReadOnlyCollection<ServiceIndex>> GetServiceIndexesAsync(IReadOnlyCollection<string> sources)
    {
        var result = new List<ServiceIndex>();
        var filePath = Directory.GetCurrentDirectory() + "/Data/NuGet/nuget-service-index.json";
        var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        var serviceIndex = JsonSerializer.Deserialize<ServiceIndex>(json);
        if (serviceIndex != null)
        {
            result.Add(serviceIndex);
        }

        return result;
    }

    public Task<RegistrationIndex?> GetPackageInformationAsync(IReadOnlyCollection<string> baseUrls, string packageId)
    {
        if (packageId.Equals("dotmarkdown", StringComparison.OrdinalIgnoreCase))
        {
            return GetDotMarkdownPackageInformation();
        }

        if (packageId.Equals("moq", StringComparison.OrdinalIgnoreCase))
        {
            return GetMoqPackageInformation();
        }

        return Task.FromResult<RegistrationIndex?>(null);
    }

    public Task<IEnumerable<NuGetCatalogPage>> GetRelevantDetailCatalogPagesAsync(IReadOnlyCollection<CatalogPage> catalogPages)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<NuGetCatalogPage>> GetRelevantDetailCatalogPagesAsync(IEnumerable<CatalogPage> catalogPages)
    {
        throw new NotImplementedException();
    }

    private async Task<RegistrationIndex?> GetDotMarkdownPackageInformation()
    {
        var filePath = Directory.GetCurrentDirectory() + "/Data/NuGet/DotMarkdown/package-registration.json";
        var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        var index = JsonSerializer.Deserialize<RegistrationIndex>(json);
        return index;
    }

    private async Task<RegistrationIndex?> GetMoqPackageInformation()
    {
        var filePath = Directory.GetCurrentDirectory() + "/Data/NuGet/Moq/package-registration.json";
        var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        var index = JsonSerializer.Deserialize<RegistrationIndex>(json);
        return index;
    }
}
