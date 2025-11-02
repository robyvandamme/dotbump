// Copyright © 2025 Roby Van Damme.

using System.Net;
using System.Text.Json;
using DotBump.Commands.BumpTools.DataModel.NuGetService;
using DotBump.Commands.BumpTools.DataModel.Registrations;
using DotBump.Commands.BumpTools.Interfaces;
using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpTools;

internal class NuGetClient : INuGetClient, IDisposable
{
    private readonly JsonSerializerOptions _defaultOptions;
    private readonly JsonSerializerOptions _semanticVersionConverterOptions;

    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public NuGetClient(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var semanticVersionConverter = new SemanticVersionConverter(logger);
        _defaultOptions = new JsonSerializerOptions();
        _semanticVersionConverterOptions = new JsonSerializerOptions();
        _semanticVersionConverterOptions.Converters.Add(semanticVersionConverter);
    }

    public async Task<ServiceIndex> GetServiceIndexAsync(string source)
    {
        _logger.MethodStart(nameof(NuGetClient), nameof(GetServiceIndexAsync), source);

        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        try
        {
            var response = await _httpClient.GetStringAsync(new Uri(source)).ConfigureAwait(false);
            var serviceIndex = JsonSerializer.Deserialize<ServiceIndex>(response);
            if (serviceIndex != null)
            {
                _logger.MethodReturn(nameof(NuGetClient), nameof(GetServiceIndexesAsync), serviceIndex);
                return serviceIndex;
            }

            _logger.Warning("Unable to deserialize service index for {Source}", source);
            throw new DotBumpException($"Unable to deserialize service index for {source}");
        }
        catch (HttpRequestException e)
        {
            _logger.Error(e, "An error occurred connecting to {Source}", source);
            throw;
        }
    }

    public async Task<IReadOnlyCollection<ServiceIndex>> GetServiceIndexesAsync(IReadOnlyCollection<string> sources)
    {
        _logger.MethodStart(nameof(NuGetClient), nameof(GetServiceIndexesAsync), sources);

        ArgumentNullException.ThrowIfNull(sources);

        var list = new List<ServiceIndex>();
        foreach (var source in sources)
        {
            try
            {
                var response = await _httpClient.GetStringAsync(new Uri(source)).ConfigureAwait(false);
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
                _logger.Error(e, "An error occurred connecting to {Source}", source);
                throw;
            }
        }

        _logger.MethodReturn(nameof(NuGetClient), nameof(GetServiceIndexesAsync), list);

        return list;
    }

    public async Task<RegistrationIndex?> GetPackageInformationAsync(string baseUrl, string packageId)
    {
        _logger.MethodStart(nameof(NuGetClient), nameof(GetPackageInformationAsync), baseUrl, packageId);

        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);

        var packageUrl = new Uri(baseUrl + "/" + packageId + "/index.json");
        try
        {
            var result = await _httpClient.GetStringAsync(packageUrl).ConfigureAwait(false);
            var registrationIndex = JsonSerializer.Deserialize<RegistrationIndex>(result, _semanticVersionConverterOptions);
            if (registrationIndex != null)
            {
                _logger.MethodReturn(nameof(NuGetClient), nameof(GetServiceIndexesAsync), registrationIndex);
                return registrationIndex;
            }
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.Debug(
                    "The package {Package} was not found at the package url {PackageUrl}",
                    packageId,
                    packageUrl);
                return null;
            }
            else
            {
                throw;
            }
        }

        _logger.Debug("No service indexes found for package {PackageId} at {BaseUrl}", packageId, baseUrl);
        return null;
    }

    public async Task<RegistrationIndex?> GetPackageInformationAsync(
        IReadOnlyCollection<string> baseUrls,
        string packageId)
    {
        _logger.MethodStart(nameof(NuGetClient), nameof(GetServiceIndexesAsync), baseUrls, packageId);

        ArgumentNullException.ThrowIfNull(baseUrls);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);

        foreach (var url in baseUrls)
        {
            // can also use the "https://api.nuget.org/v3/registration5-semver1/{id-lower}/index.json and replace the id, same result... though
            var packageUrl = new Uri(url + "/" + packageId + "/index.json");
            var result = await _httpClient.GetStringAsync(packageUrl).ConfigureAwait(false);
            var registrationIndex = JsonSerializer.Deserialize<RegistrationIndex>(result, _semanticVersionConverterOptions);
            if (registrationIndex != null)
            {
                // NOTE: package information can be found at multiple service indexes, so it is possible we need to a bit more here.
                _logger.MethodReturn(nameof(NuGetClient), nameof(GetServiceIndexesAsync), registrationIndex);
                return registrationIndex;
            }
        }

        _logger.Debug("No service indexes found for package {PackageId} at {BaseUrls}", packageId, baseUrls);
        _logger.MethodReturn(nameof(NuGetClient), nameof(GetServiceIndexesAsync));

        return null;
    }

    public async Task<IEnumerable<CatalogPage>> GetRelevantDetailCatalogPagesAsync(
        IReadOnlyCollection<CatalogPage> catalogPages)
    {
        _logger.MethodStart(nameof(NuGetClient), nameof(GetRelevantDetailCatalogPagesAsync), catalogPages);

        ArgumentNullException.ThrowIfNull(catalogPages);

        var result = new List<CatalogPage>();

        foreach (var catalogPage in catalogPages)
        {
            var json = await _httpClient.GetStringAsync(new Uri(catalogPage.Id)).ConfigureAwait(false);
            var detailPage = JsonSerializer.Deserialize<CatalogPage>(json, _defaultOptions);
            if (detailPage != null)
            {
                result.Add(detailPage);
            }
        }

        _logger.MethodReturn(nameof(NuGetClient), nameof(GetRelevantDetailCatalogPagesAsync), result);

        return result;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
