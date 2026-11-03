// Copyright © Roby Van Damme.

using System.Text.Json;
using DotBump.Commands.BumpSdk;
using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Commands.BumpSdk.Interfaces;
using DotBump.Common;

namespace DotBump.Tests.Commands.BumpSdk.Fakes;

internal class ReleaseFileService : IReleaseService
{
    private readonly string _filePath = Directory.GetCurrentDirectory() + "/Data/releases-index.json";

    public async Task<IEnumerable<Release>> GetReleasesAsync()
    {
        var json = await File.ReadAllTextAsync(_filePath).ConfigureAwait(false);
        var releaseIndex = JsonSerializer.Deserialize<ReleaseIndex>(json);
        if (releaseIndex != null)
        {
            return releaseIndex.ReleasesIndex;
        }

        throw new DotBumpException("ReleaseIndex is empty. Please check the source URL.");
    }

    public async Task<ChannelReleases> GetChannelReleasesAsync(Uri channelReleasesUri)
    {
        ArgumentNullException.ThrowIfNull(channelReleasesUri);

        // ../../<channel>/releases.json -> Data/releases-<channel>.json, so a test that bumps a
        // different channel than the fixture provides fails loudly instead of using wrong data.
        var channelVersion = channelReleasesUri.Segments[^2].TrimEnd('/');
        var path = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Data",
            $"releases-{channelVersion}.json");
        if (!File.Exists(path))
        {
            throw new DotBumpException($"No test data for channel releases '{channelReleasesUri}'.");
        }

        var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
        var channelReleases = JsonSerializer.Deserialize<ChannelReleases>(json);
        if (channelReleases?.Releases != null)
        {
            return channelReleases;
        }

        throw new DotBumpException("Channel releases are empty. Please check the source URL.");
    }
}
