// Copyright © 2025 Roby Van Damme.

using System.Text.Json.Serialization;

namespace DotBump.Commands.BumpTools.DataModel.NuGetService;

public record Context
{
    [JsonPropertyName("@vocab")]
    public string Vocab { get; init; } = string.Empty;

    [JsonPropertyName("comment")]
    public string Comment { get; init; } = string.Empty;
}
