// Copyright © 2025 Roby Van Damme.

using System.Text.Json.Serialization;

namespace DotBump.Commands.BumpTools.DataModel.Registrations;

internal record RegistrationIndex
{
    [JsonPropertyName("@id")]
    public string? Id { get; init; }

    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("items")]
    public List<CatalogPage>? CatalogPages { get; init; }
}
