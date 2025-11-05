// Copyright © 2025 Roby Van Damme.

using System.Text.Json.Serialization;
using DotBump.Common;

namespace DotBump.Commands.BumpTools.DataModel.Registrations;

internal record PackageDetails
{
    [JsonPropertyName("@id")]
    public string? Id { get; init; }

    [JsonPropertyName("@type")]
    public string? Type { get; init; }

    [JsonPropertyName("listed")]
    public bool? Listed { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonIgnore]
    public SemanticVersion SemanticVersion
    {
        get { return new SemanticVersion(Version); }
    }
}
