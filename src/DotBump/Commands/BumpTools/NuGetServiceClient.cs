// Copyright © 2025 Roby Van Damme.

using System.Text.Json;
using DotBump.Commands.BumpTools.DataModel.Catalog;
using DotBump.Commands.BumpTools.DataModel.NuGetService;
using DotBump.Commands.BumpTools.DataModel.Registrations;
using DotBump.Commands.BumpTools.Interfaces;
using Serilog;

namespace DotBump.Commands.BumpTools;

internal class NuGetServiceClient : INuGetServiceClient
{
    private static readonly JsonSerializerOptions s_jsonSerializerOptions =
        new() { PropertyNameCaseInsensitive = false, };

    private readonly ILogger _logger;

    // TODO: How to initialize multiple Http Clients for the different sources and then dispose them? Or not relevant?
    // private readonly HttpClient _httpClient;
    public NuGetServiceClient(ILogger logger)
    {
        _logger = logger;

        // _httpClient = new HttpClient();
        // _httpClient.DefaultRequestHeaders.Add("User-Agent", " NuGetServiceClient/1.0");
    }

    public async Task<List<ServiceIndex>> GetServiceIndexesAsync(ICollection<string> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        var list = new List<ServiceIndex>();
        foreach (var source in sources)
        {
            using var client = new HttpClient();
            try
            {
                var response = await client.GetStringAsync(new Uri(source)).ConfigureAwait(false);

                var serviceIndex = JsonSerializer.Deserialize<ServiceIndex>(response);
                if (serviceIndex != null)
                {
                    list.Add(serviceIndex);
                }
                else
                {
                    _logger.Debug("Unable to deserialize service index for {Source}", source);
                }
            }
            catch (HttpRequestException e)
            {
                // TODO: how to propagate this to the console? Throw? That is the easiest solution for now...
                _logger.Error(e, "An error occurred connecting to {Source}", source);
            }
        }

        return list;
    }

    public async Task<RegistrationIndex?> GetPackageInformationAsync(List<string> baseUrls, string packageId)
    {
        ArgumentNullException.ThrowIfNull(baseUrls);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);

        foreach (var url in baseUrls)
        {
            using var client = new HttpClient();
            var packageUrl = new Uri(url + "/" + packageId + "/index.json");
            var result = await client.GetStringAsync(packageUrl).ConfigureAwait(false);
            var options = s_jsonSerializerOptions;
            var registrationIndex = JsonSerializer.Deserialize<RegistrationIndex>(result, options);
            if (registrationIndex != null)
            {
                return registrationIndex;
            }
        }

        return null;
    }

    public async Task<IEnumerable<NuGetCatalogPage>> GetRelevantDetailCatalogPagesAsync(
        IEnumerable<CatalogPage> catalogPages)
    {
        ArgumentNullException.ThrowIfNull(catalogPages);

        var result = new List<NuGetCatalogPage>();
        using var client = new HttpClient();
        foreach (var catalogPage in catalogPages)
        {
            var json = await client.GetStringAsync(new Uri(catalogPage.Id)).ConfigureAwait(false);
            var options = s_jsonSerializerOptions;
            var detailPage = JsonSerializer.Deserialize<NuGetCatalogPage>(json, options);
            if (detailPage != null)
            {
                result.Add(detailPage);
            }
        }

        return result;
    }
}
