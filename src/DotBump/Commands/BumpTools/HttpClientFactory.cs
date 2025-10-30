// Copyright © 2025 Roby Van Damme.

using System.Net;
using DotBump.Commands.BumpTools.DataModel.NuGetClientConfiguration;
using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpTools;

internal class HttpClientFactory(ILogger logger)
{
    public HttpClient Create(NuGetClientConfig config)
    {
        logger.MethodStart(nameof(HttpClientFactory), nameof(Create), config);

        if (config.Credential != null)
        {
            logger.Debug("Credential found in config for {URL}", config.Url);

            var handler = new HttpClientHandler
            {
                Credentials = new NetworkCredential(config.Credential.UserName, config.Credential.Password),
            };
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", "dotBumpNuGetClient/1.0");
            logger.MethodReturn(nameof(HttpClientFactory), nameof(Create));
            return client;
        }
        else
        {
            logger.Debug("No credential found in config for {URL}", config.Url);

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "dotBumpNuGetClient/1.0");
            logger.MethodReturn(nameof(HttpClientFactory), nameof(Create));
            return client;
        }
    }
}
