// Copyright © 2025 Roby Van Damme.

using System.Text.Json;
using DotBump.Commands.BumpTools.DataModel.NuGetService;
using DotBump.Commands.BumpTools.Interfaces;
using Serilog;

namespace DotBump.Commands.BumpTools;

internal class NuGetServiceClient : INuGetServiceClient
{
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
}
