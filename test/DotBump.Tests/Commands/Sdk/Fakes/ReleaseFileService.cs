// Copyright © 2025 Roby Van Damme.

using System.Text.Json;
using DotBump.Commands.Sdk;
using DotBump.Commands.Sdk.DataModel;
using DotBump.Common;

namespace DotBump.Tests.Commands.Sdk.Fakes;

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
