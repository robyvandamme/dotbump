// Copyright © 2025 Roby Van Damme.

using System.Text.Json;
using DotBump.Commands.BumpSdk;
using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Common;

namespace DotBump.Tests.Commands.BumpSdk.Fakes;

public class ReleaseFileService : IReleaseService
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
}
