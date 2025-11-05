// Copyright © 2025 Roby Van Damme.

using System.Text.Json.Serialization;

namespace DotBump.Commands.BumpTools.DataModel.Registrations;

internal record Package
{
    [JsonPropertyName("@id")]
    public required string Id { get; init; }

    [JsonPropertyName("@type")]
    public string? Type { get; init; }

    [JsonPropertyName("catalogEntry")]
    public required PackageDetails CatalogEntry { get; init; }
}
