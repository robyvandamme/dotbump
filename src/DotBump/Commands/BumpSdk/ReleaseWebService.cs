// Copyright © Roby Van Damme.

using System.Text.Json;
using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Commands.BumpSdk.Interfaces;
using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpSdk;

internal class ReleaseWebService(ILogger logger, HttpClient httpClient) : IReleaseService
{
    private readonly Uri _releaseUri =
        new("https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json");

    public async Task<IEnumerable<Release>> GetReleasesAsync()
    {
        logger.MethodStart(nameof(ReleaseWebService), nameof(GetReleasesAsync));

        var json = await GetJsonAsync(_releaseUri).ConfigureAwait(false);

        var releaseIndex = JsonSerializer.Deserialize<ReleaseIndex>(json);
        if (releaseIndex?.ReleasesIndex != null)
        {
            logger.MethodReturn(nameof(ReleaseWebService), nameof(GetReleasesAsync), "release index");
            return releaseIndex.ReleasesIndex;
        }

        throw new DotBumpException("ReleaseIndex is empty. Please check the source URL.");
    }

    public async Task<ChannelReleases> GetChannelReleasesAsync(Uri channelReleasesUri)
    {
        logger.MethodStart(nameof(ReleaseWebService), nameof(GetChannelReleasesAsync));

        ArgumentNullException.ThrowIfNull(channelReleasesUri);

        var json = await GetJsonAsync(channelReleasesUri).ConfigureAwait(false);

        var channelReleases = JsonSerializer.Deserialize<ChannelReleases>(json);
        if (channelReleases?.Releases != null)
        {
            logger.MethodReturn(nameof(ReleaseWebService), nameof(GetChannelReleasesAsync), "channel releases");
            return channelReleases;
        }

        throw new DotBumpException(
            $"No release history returned from {channelReleasesUri}. Please check the source URL.");
    }

    private async Task<string> GetJsonAsync(Uri uri)
    {
        string json;

        try
        {
            logger.Debug("Fetching releases from {Uri}", uri.ToString());
            json = await httpClient.GetStringAsync(uri).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.Error(exception, "An error occurred fetching releases from {Uri}", uri.ToString());
            throw;
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new DotBumpException("No json release data returned from URL");
        }

        return json;
    }
}
