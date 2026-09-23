// Copyright © Roby Van Damme.

using System.Text.Json.Serialization;

namespace DotBump.NuGet.DataModel.NuGetService;

public record Resource
{
    [JsonPropertyName("@id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("@type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("comment")]
    public string? Comment { get; init; }

    [JsonPropertyName("clientVersion")]
    public string? ClientVersion { get; init; }
}
