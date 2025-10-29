// Copyright © 2025 Roby Van Damme.

using System.Text.Json;
using DotBump.Commands.BumpTools.DataModel.Catalog;
using DotBump.Commands.BumpTools.DataModel.NuGetService;
using DotBump.Commands.BumpTools.DataModel.Registrations;
using DotBump.Commands.BumpTools.Interfaces;
using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpTools;

internal class NuGetServiceClient(ILogger logger) : INuGetServiceClient
{
    private static readonly JsonSerializerOptions s_jsonSerializerOptions =
        new() { PropertyNameCaseInsensitive = false, };

    private readonly SemanticVersionConverter _semanticVersionConverter = new(logger);

    public async Task<IReadOnlyCollection<ServiceIndex>> GetServiceIndexesAsync(IReadOnlyCollection<string> sources)
    {
        logger.MethodStart(nameof(NuGetServiceClient), nameof(GetServiceIndexesAsync), sources);

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
                    logger.Debug("Unable to deserialize service index for {Source}", source);
                }
            }
            catch (HttpRequestException e)
            {
                // TODO: how to propagate this to the console? Throw? That is the easiest solution for now...
                // But not sure that is the best option? Well.... if we can not connect to a source we do need to inform somehow...
                // throwing is what I am doing so far, so consistency is throw?
                // If not use a warning instead
                logger.Error(e, "An error occurred connecting to {Source}", source);
            }
        }

        logger.MethodReturn(nameof(NuGetServiceClient), nameof(GetServiceIndexesAsync), list);

        return list;
    }

    public async Task<RegistrationIndex?> GetPackageInformationAsync(
        IReadOnlyCollection<string> baseUrls,
        string packageId)
    {
        logger.MethodStart(nameof(NuGetServiceClient), nameof(GetServiceIndexesAsync), baseUrls, packageId);

        ArgumentNullException.ThrowIfNull(baseUrls);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);

        foreach (var url in baseUrls)
        {
            using var client = new HttpClient();
            // can also use the "https://api.nuget.org/v3/registration5-semver1/{id-lower}/index.json and replace the id, same result... though
            var packageUrl = new Uri(url + "/" + packageId + "/index.json"); 
            var result = await client.GetStringAsync(packageUrl).ConfigureAwait(false);
            var options = s_jsonSerializerOptions;
            options.Converters.Add(_semanticVersionConverter);
            var registrationIndex = JsonSerializer.Deserialize<RegistrationIndex>(result, options);
            if (registrationIndex != null)
            {
                // NOTE: package information can be found at multiple service indexes, so it is possible we need to a bit more here.
                logger.MethodReturn(nameof(NuGetServiceClient), nameof(GetServiceIndexesAsync), registrationIndex);
                return registrationIndex;
            }
        }

        logger.Debug("No service indexes found for package {PackageId} at {BaseUrls}", packageId, baseUrls);
        logger.MethodReturn(nameof(NuGetServiceClient), nameof(GetServiceIndexesAsync));

        return null;
    }

    public async Task<IEnumerable<NuGetCatalogPage>> GetRelevantDetailCatalogPagesAsync(
        IReadOnlyCollection<CatalogPage> catalogPages)
    {
        logger.MethodStart(nameof(NuGetServiceClient), nameof(GetRelevantDetailCatalogPagesAsync), catalogPages);

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

        logger.MethodReturn(nameof(NuGetServiceClient), nameof(GetRelevantDetailCatalogPagesAsync), result);

        return result;
    }
}
