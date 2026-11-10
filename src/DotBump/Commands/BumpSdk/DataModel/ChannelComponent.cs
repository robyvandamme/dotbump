// Copyright © Roby Van Damme.

using System.Text.Json.Serialization;
using DotBump.Common;

namespace DotBump.Commands.BumpSdk.DataModel;

/// <summary>
/// A component version published as part of a channel release, such as the runtime or ASP.NET Core
/// runtime version.
/// </summary>
internal record ChannelComponent([property: JsonPropertyName("version")] string? Version)
{
    [JsonIgnore]
    public SemanticVersion? SemanticVersion { get; } =
        string.IsNullOrWhiteSpace(Version) ? null : new SemanticVersion(Version);
}
