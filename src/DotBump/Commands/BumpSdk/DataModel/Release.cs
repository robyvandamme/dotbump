// Copyright © 2025 Roby Van Damme.

using System.Text.Json.Serialization;
using DotBump.Common;

namespace DotBump.Commands.BumpSdk.DataModel;

internal record Release(string ChannelVersion, string LatestSdk, string SupportPhase, bool Security)
{
    [JsonPropertyName("channel-version")]
    public string ChannelVersion { get; } = ChannelVersion;

    [JsonPropertyName("latest-sdk")]
    public string LatestSdk { get; } = LatestSdk;

    public SemanticVersion LatestSdkVersion { get; } = new(LatestSdk);

    [JsonPropertyName("support-phase")]
    public string SupportPhase { get; } = SupportPhase;

    [JsonPropertyName("security")]
    public bool Security { get; set; } = Security;
}
