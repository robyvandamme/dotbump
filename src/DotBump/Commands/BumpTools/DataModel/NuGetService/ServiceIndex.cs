// Copyright © 2025 Roby Van Damme.

using System.Text.Json.Serialization;

namespace DotBump.Commands.BumpTools.DataModel.NuGetService;

public record ServiceIndex
{
    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;

    [JsonPropertyName("resources")]
    public List<Resource> Resources { get; init; } = new();

    [JsonPropertyName("@context")]
    public Context Context { get; init; } = new();
}
