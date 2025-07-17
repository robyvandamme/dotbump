// Copyright © 2025 Roby Van Damme.

using System.Text.Json.Serialization;
using DotBump.Common;

namespace DotBump.Commands.BumpSdk.DataModel;

public record Release(string ChannelVersion, string LatestSdk, string SupportPhase)
{
    [JsonPropertyName("channel-version")]
    public string ChannelVersion { get; } = ChannelVersion;

    [JsonPropertyName("latest-sdk")]
    public string LatestSdk { get; } = LatestSdk;

    public SemanticVersion LatestSdkVersion { get; } = new(LatestSdk);

    [JsonPropertyName("support-phase")]
    public string SupportPhase { get; } = SupportPhase;
}
