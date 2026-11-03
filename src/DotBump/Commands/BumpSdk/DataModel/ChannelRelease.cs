// Copyright © Roby Van Damme.

using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace DotBump.Commands.BumpSdk.DataModel;

/// <summary>
/// A single release within a .NET channel, including the SDK versions it shipped per feature band.
/// </summary>
internal record ChannelRelease(
    [property: JsonPropertyName("security")] bool Security,
    [property: JsonPropertyName("sdks")] Collection<ChannelSdk>? Sdks);
