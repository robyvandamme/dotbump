// Copyright © Roby Van Damme.

using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Commands.BumpSdk.Interfaces;

namespace DotBump.Tests.Commands.BumpDocker.Fakes;

internal sealed class FakeReleaseService(params Release[] releases) : IReleaseService
{
    public Task<IEnumerable<Release>> GetReleasesAsync()
    {
        return Task.FromResult<IEnumerable<Release>>(releases);
    }
}
