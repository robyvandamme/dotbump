// Copyright © 2025 Roby Van Damme.

using System.Net;
using System.Text.Json;
using DotBump.Commands.BumpTools.DataModel.NuGetService;
using DotBump.Commands.BumpTools.DataModel.Registrations;
using DotBump.Commands.BumpTools.Interfaces;
using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpTools;

internal class NuGetClient(HttpClient httpClient, ILogger logger) : INuGetClient, IDisposable
{
    private readonly JsonSerializerOptions _defaultOptions = new();

    /// <summary>
    /// Gets the <see cref="ServiceIndex"/> for the supplied package source URL.
    /// </summary>
    /// <param name="packageSourceUrl">The NuGet package source URL.</param>
    /// <returns>The service index for the package source.</returns>
    /// <exception cref="DotBumpException">When the service index can not be deserialized.</exception>
    /// <exception cref="HttpRequestException">When an HttpRequestException occurs.</exception>
    public async Task<ServiceIndex> GetServiceIndexAsync(string packageSourceUrl)
    {
        logger.MethodStart(nameof(NuGetClient), nameof(GetServiceIndexAsync), packageSourceUrl);

        ArgumentException.ThrowIfNullOrWhiteSpace(packageSourceUrl);

        try
        {
            var response = await httpClient.GetStringAsync(new Uri(packageSourceUrl)).ConfigureAwait(false);
            var serviceIndex = JsonSerializer.Deserialize<ServiceIndex>(response);
            if (serviceIndex != null)
            {
                logger.MethodReturn(nameof(NuGetClient), nameof(GetServiceIndexAsync), serviceIndex);
                return serviceIndex;
            }

            logger.Warning("Unable to deserialize service index for {Source}", packageSourceUrl);
            throw new DotBumpException($"Unable to deserialize service index for {packageSourceUrl}");
        }
        catch (HttpRequestException e)
        {
            logger.Error(e, "An error occurred connecting to {Source}", packageSourceUrl);
            throw;
        }
    }

    /// <summary>
    /// Gets the package information from the package source.
    /// </summary>
    /// <param name="registrationBaseUrl">The RegistrationsBaseUrl from the <see cref="ServiceIndex"/>.</param>
    /// <param name="packageId">The package ID.</param>
    /// <returns>A package registration index if the package can be found at the URL.</returns>
    /// <exception cref="HttpRequestException">When an HttpRequestException occurs that is not caused by a 404 status code.</exception>
    public async Task<RegistrationIndex?> GetPackageInformationAsync(string registrationBaseUrl, string packageId)
    {
        logger.MethodStart(nameof(NuGetClient), nameof(GetPackageInformationAsync), registrationBaseUrl, packageId);

        ArgumentException.ThrowIfNullOrWhiteSpace(registrationBaseUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);

        var packageUrl = new Uri(registrationBaseUrl + "/" + packageId + "/index.json");
        try
        {
            var result = await httpClient.GetStringAsync(packageUrl).ConfigureAwait(false);
            var registrationIndex = JsonSerializer.Deserialize<RegistrationIndex>(result, _defaultOptions);
            if (registrationIndex != null)
            {
                logger.MethodReturn(nameof(NuGetClient), nameof(GetPackageInformationAsync), registrationIndex);
                return registrationIndex;
            }
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                logger.Debug(
                    "The package {Package} was not found at the package url {PackageUrl}",
                    packageId,
                    packageUrl);
                return null;
            }

            logger.Error(e, "An HTTP Request exception occured calling {PackageUrl}", packageUrl);
            throw;
        }

        logger.Debug("No service indexes found for package {PackageId} at {BaseUrl}", packageId, registrationBaseUrl);
        return null;
    }

    /// <summary>
    /// Gets the catalog pages when the releases are not in the <see cref="RegistrationIndex"/>.
    /// </summary>
    /// <param name="catalogPages">The list of catalog pages containing the relevant versions. </param>
    /// <returns>The detail catalog pages.</returns>
    public async Task<IReadOnlyCollection<CatalogPage>> GetRelevantCatalogPagesAsync(
        IReadOnlyCollection<CatalogPage> catalogPages)
    {
        logger.MethodStart(nameof(NuGetClient), nameof(GetRelevantCatalogPagesAsync), catalogPages);

        ArgumentNullException.ThrowIfNull(catalogPages);

        var result = new List<CatalogPage>();

        foreach (var catalogPage in catalogPages)
        {
            var json = await httpClient.GetStringAsync(new Uri(catalogPage.Id)).ConfigureAwait(false);
            var detailPage = JsonSerializer.Deserialize<CatalogPage>(json, _defaultOptions);
            if (detailPage != null)
            {
                result.Add(detailPage);
            }
        }

        logger.MethodReturn(nameof(NuGetClient), nameof(GetRelevantCatalogPagesAsync), result);

        return result;
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }
}
