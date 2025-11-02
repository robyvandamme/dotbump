// Copyright © 2025 Roby Van Damme.

using System.Text.Json;
using DotBump.Commands.BumpTools.DataModel.Catalog;
using DotBump.Commands.BumpTools.DataModel.NuGetService;
using DotBump.Commands.BumpTools.DataModel.Registrations;
using DotBump.Commands.BumpTools.Interfaces;
using DotBump.Common;
using Serilog;

namespace DotBump.Tests.Commands.BumpTools.Fakes;

internal class FakeNuGetClient : INuGetClient
{
    private readonly JsonSerializerOptions _defaultOptions;
    private readonly JsonSerializerOptions _semanticVersionConverterOptions;

    public FakeNuGetClient(ILogger logger)
    {
        var semanticVersionConverter = new SemanticVersionConverter(logger);
        _defaultOptions = new JsonSerializerOptions();
        _semanticVersionConverterOptions = new JsonSerializerOptions();
        _semanticVersionConverterOptions.Converters.Add(semanticVersionConverter);
    }

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

        if (packageId.Equals("dotBump", StringComparison.OrdinalIgnoreCase))
        {
            return GetDotBumpPackageInformation();
        }

        return Task.FromResult<RegistrationIndex?>(null);
    }

    public Task<IEnumerable<NuGetCatalogPage>> GetRelevantDetailCatalogPagesAsync(
        IReadOnlyCollection<CatalogPage> catalogPages)
    {
        throw new NotImplementedException();
    }

    public Task<RegistrationIndex?> GetPackageInformationAsync(string baseUrl, string packageId)
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
        var index = JsonSerializer.Deserialize<RegistrationIndex>(json, _semanticVersionConverterOptions);
        return index;
    }

    private async Task<RegistrationIndex?> GetMoqPackageInformation()
    {
        var filePath = Directory.GetCurrentDirectory() + "/Data/NuGet/Moq/package-registration.json";
        var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        var index = JsonSerializer.Deserialize<RegistrationIndex>(json, _semanticVersionConverterOptions);
        return index;
    }

    private async Task<RegistrationIndex?> GetDotBumpPackageInformation()
    {
        var filePath = Directory.GetCurrentDirectory() + "/Data/NuGet/DotBumpGitHub/package-registration.json";
        var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        var index = JsonSerializer.Deserialize<RegistrationIndex>(json, _semanticVersionConverterOptions);
        return index;
    }
}
