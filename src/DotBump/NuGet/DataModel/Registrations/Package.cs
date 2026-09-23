// Copyright © Roby Van Damme.

using System.Text.Json.Serialization;

namespace DotBump.NuGet.DataModel.Registrations;

internal record Package
{
    [JsonPropertyName("@id")]
    public required string Id { get; init; }

    [JsonPropertyName("@type")]
    public string? Type { get; init; }

    [JsonPropertyName("catalogEntry")]
    public required PackageDetails CatalogEntry { get; init; }
}
