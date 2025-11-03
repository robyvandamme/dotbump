// Copyright © 2025 Roby Van Damme.

using System.Net;
using DotBump.Commands.BumpTools.DataModel.NuGetClientConfiguration;
using DotBump.Commands.BumpTools.Interfaces;
using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpTools;

internal class NuGetClientFactory(ILogger logger) : INuGetClientFactory
{
    public NuGetClient CreateNuGetClient(NuGetClientConfig config)
    {
        logger.MethodStart(nameof(NuGetClientFactory), nameof(CreateNuGetClient), config);

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

            logger.MethodReturn(nameof(NuGetClientFactory), nameof(CreateNuGetClient));
            return nugetClient;
        }
        else
        {
            logger.Debug("No credential found in config for {URL}", config.Url);

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "dotBumpNuGetClient/1.0");

            var nugetClient = new NuGetClient(httpClient, logger);

            logger.MethodReturn(nameof(NuGetClientFactory), nameof(CreateNuGetClient));
            return nugetClient;
        }
    }
}
