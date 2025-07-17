// Copyright © 2025 Roby Van Damme.

using System.Text.Json;
using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpSdk;

public class ReleaseWebService(ILogger logger) : IReleaseService
{
    private readonly Uri _releaseUri = new Uri("https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json");

    public async Task<IEnumerable<Release>> GetReleasesAsync()
    {
        logger.MethodStart(nameof(ReleaseWebService), nameof(GetReleasesAsync));

        string json;
        using var client = new HttpClient();
        try
        {
            logger.Debug("Fetching releases from {Uri}", _releaseUri.ToString());
            json = await client.GetStringAsync(_releaseUri).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.Error(exception, "An error occured fetching releases from {Uri}", _releaseUri.ToString());
            throw;
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new DotBumpException("No json release data returned from URL");
        }

        var releaseIndex = JsonSerializer.Deserialize<ReleaseIndex>(json);
        if (releaseIndex != null)
        {
            logger.MethodReturn(nameof(ReleaseWebService), nameof(GetReleasesAsync), "release index");
            return releaseIndex.ReleasesIndex;
        }

        throw new DotBumpException("ReleaseIndex is empty. Please check the source URL.");
    }
}
