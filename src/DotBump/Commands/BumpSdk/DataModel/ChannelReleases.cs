// Copyright © Roby Van Damme.

using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace DotBump.Commands.BumpSdk.DataModel;

/// <summary>
/// The release history of a single .NET channel, as published in the channel's <c>releases.json</c>.
/// </summary>
internal record ChannelReleases(
    [property: JsonPropertyName("releases")] Collection<ChannelRelease> Releases);
