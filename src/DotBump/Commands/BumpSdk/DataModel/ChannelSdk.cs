// Copyright © Roby Van Damme.

using System.Text.Json.Serialization;
using DotBump.Common;

namespace DotBump.Commands.BumpSdk.DataModel;

/// <summary>
/// An SDK version shipped as part of a channel release.
/// </summary>
internal record ChannelSdk([property: JsonPropertyName("version")] string Version)
{
    public SemanticVersion SemanticVersion { get; } = new(Version);
}
