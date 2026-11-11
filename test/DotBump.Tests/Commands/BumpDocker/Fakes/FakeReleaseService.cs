// Copyright © Roby Van Damme.

using System.Collections.ObjectModel;
using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Commands.BumpSdk.Interfaces;
using DotBump.Common;

namespace DotBump.Tests.Commands.BumpDocker.Fakes;

internal sealed class FakeReleaseService(params Release[] releases) : IReleaseService
{
    private readonly Dictionary<string, ChannelReleases> _channelReleases = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Exception> _channelReleaseExceptions = new(StringComparer.Ordinal);

    public FakeReleaseService WithChannelReleases(string channel, params ChannelRelease[] channelReleases)
    {
        _channelReleases[channel] = new ChannelReleases(new Collection<ChannelRelease>(channelReleases));
        return this;
    }

    public FakeReleaseService WithChannelReleasesException(string channel, Exception exception)
    {
        _channelReleaseExceptions[channel] = exception;
        return this;
    }

    public Task<IEnumerable<Release>> GetReleasesAsync()
    {
        return Task.FromResult<IEnumerable<Release>>(releases);
    }

    public Task<ChannelReleases> GetChannelReleasesAsync(Uri channelReleasesUri)
    {
        var channel = GetChannel(channelReleasesUri);

        if (_channelReleaseExceptions.TryGetValue(channel, out var exception))
        {
            throw exception;
        }

        return _channelReleases.TryGetValue(channel, out var channelReleases)
            ? Task.FromResult(channelReleases)
            : throw new DotBumpException($"No channel releases configured for {channelReleasesUri}.");
    }

    private static string GetChannel(Uri channelReleasesUri)
    {
        return channelReleasesUri.Segments[^2].TrimEnd('/');
    }
}
