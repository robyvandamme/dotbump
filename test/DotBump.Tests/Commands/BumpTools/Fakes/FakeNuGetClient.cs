// Copyright © 2025 Roby Van Damme.

using System.Text.Json;
using DotBump.Commands.BumpTools.DataModel.NuGetService;
using DotBump.Commands.BumpTools.DataModel.Registrations;
using DotBump.Commands.BumpTools.Interfaces;
using DotBump.Common;
using Serilog;

namespace DotBump.Tests.Commands.BumpTools.Fakes;

internal class FakeNuGetClient : INuGetClient
{
    private readonly JsonSerializerOptions _defaultOptions;

    public FakeNuGetClient(ILogger logger)
    {
        _defaultOptions = new JsonSerializerOptions();
    }

    public async Task<ServiceIndex> GetServiceIndexAsync(string packageSourceUrl)
    {
        var filePath = Directory.GetCurrentDirectory() + "/Data/NuGet/nuget-service-index.json";
        var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        var serviceIndex = JsonSerializer.Deserialize<ServiceIndex>(json);
        return serviceIndex ?? throw new DotBumpException();
    }

    public Task<IReadOnlyCollection<CatalogPage>> GetRelevantCatalogPagesAsync(
        IReadOnlyCollection<CatalogPage> catalogPages)
    {
        return Task.FromResult<IReadOnlyCollection<CatalogPage>>(new List<CatalogPage>());
    }

    public Task<RegistrationIndex?> GetPackageInformationAsync(string registrationBaseUrl, string packageId)
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

    private async Task<RegistrationIndex?> GetDotMarkdownPackageInformation()
    {
        var filePath = Directory.GetCurrentDirectory() + "/Data/NuGet/DotMarkdown/package-registration.json";
        var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        var index = JsonSerializer.Deserialize<RegistrationIndex>(json, _defaultOptions);
        return index;
    }

    private async Task<RegistrationIndex?> GetMoqPackageInformation()
    {
        var filePath = Directory.GetCurrentDirectory() + "/Data/NuGet/Moq/package-registration.json";
        var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        var index = JsonSerializer.Deserialize<RegistrationIndex>(json, _defaultOptions);
        return index;
    }

    private async Task<RegistrationIndex?> GetDotBumpPackageInformation()
    {
        var filePath = Directory.GetCurrentDirectory() + "/Data/NuGet/DotBumpGitHub/package-registration.json";
        var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        var index = JsonSerializer.Deserialize<RegistrationIndex>(json, _defaultOptions);
        return index;
    }
}
