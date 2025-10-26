// Copyright © 2025 Roby Van Damme.

using System.Text.Json;
using DotBump.Commands.BumpTools.DataModel.NuGetService;
using DotBump.Commands.BumpTools.DataModel.Registrations;
using DotBump.Commands.BumpTools.Interfaces;

namespace DotBump.Tests.Commands.BumpTools.Fakes;

internal class FakeNuGetServiceClient : INuGetServiceClient
{
    public async Task<List<ServiceIndex>> GetServiceIndexesAsync(ICollection<string> sources)
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

    public Task<RegistrationIndex?> GetPackageInformationAsync(List<string> baseUrls, string packageId)
    {
        if (packageId.Equals("dotmarkdown", StringComparison.OrdinalIgnoreCase))
        {
            return GetDotMarkdownPackageInformation();
        }

        return Task.FromResult<RegistrationIndex?>(null);
    }

    private async Task<RegistrationIndex?> GetDotMarkdownPackageInformation()
    {
        var filePath = Directory.GetCurrentDirectory() + "/Data/NuGet/DotMarkdown/package-registration.json";
        var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        var index = JsonSerializer.Deserialize<RegistrationIndex>(json);
        return index;
    }
}
