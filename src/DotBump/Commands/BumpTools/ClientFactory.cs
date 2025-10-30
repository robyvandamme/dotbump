// Copyright © 2025 Roby Van Damme.

using System.Net;
using DotBump.Commands.BumpTools.DataModel.NuGetClientConfiguration;
using DotBump.Commands.BumpTools.Interfaces;
using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpTools;

internal class ClientFactory(ILogger logger) : IClientFactory
{
    /// <summary>
    /// Creates an HttpClient using a given <see cref="NuGetClientConfig"/>.
    /// Not used at the moment, but keep around in case I change my mind.
    /// </summary>
    /// <param name="config"><see cref="NuGetClientConfig"/>The config to use.</param>
    /// <returns>An <see cref="HttpClient"/> instance.</returns>
    [Obsolete("Not used at the moment.")]
    public HttpClient CreateHttpClient(NuGetClientConfig config)
    {
        logger.MethodStart(nameof(ClientFactory), nameof(CreateHttpClient), config);

        if (config.Credential != null)
        {
            logger.Debug("Credential found in config for {URL}", config.Url);

            var handler = new HttpClientHandler
            {
                Credentials = new NetworkCredential(config.Credential.UserName, config.Credential.Password),
            };
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", "dotBumpNuGetClient/1.0");
            logger.MethodReturn(nameof(ClientFactory), nameof(CreateHttpClient));
            return client;
        }
        else
        {
            logger.Debug("No credential found in config for {URL}", config.Url);

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "dotBumpNuGetClient/1.0");
            logger.MethodReturn(nameof(ClientFactory), nameof(CreateHttpClient));
            return client;
        }
    }

    public NuGetClient CreateNuGetClient(NuGetClientConfig config)
    {
        logger.MethodStart(nameof(ClientFactory), nameof(CreateNuGetClient), config);

        if (config.Credential != null)
        {
            logger.Debug("Credential found in config for {URL}", config.Url);

            var handler = new HttpClientHandler
            {
                Credentials = new NetworkCredential(config.Credential.UserName, config.Credential.Password),
            };
            var httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "dotBumpNuGetClient/1.0");

            var nugetClient = new NuGetClient(httpClient, logger);

            logger.MethodReturn(nameof(ClientFactory), nameof(CreateHttpClient));
            return nugetClient;
        }
        else
        {
            logger.Debug("No credential found in config for {URL}", config.Url);

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "dotBumpNuGetClient/1.0");

            var nugetClient = new NuGetClient(httpClient, logger);

            logger.MethodReturn(nameof(ClientFactory), nameof(CreateHttpClient));
            return nugetClient;
        }
    }
}
