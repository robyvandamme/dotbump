// Copyright © 2025 Roby Van Damme.

using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace DotBump.Commands.BumpSdk.DataModel;

internal record ReleaseIndex(string Schema, Collection<Release> ReleasesIndex)
{
    [JsonPropertyName("$schema")]
    public string Schema { get; init; } = Schema;

    [JsonPropertyName("releases-index")]
    public Collection<Release> ReleasesIndex { get; init; } = ReleasesIndex;
}
