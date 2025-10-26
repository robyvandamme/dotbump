// Copyright © 2025 Roby Van Damme.

using System.Text.Json.Serialization;

namespace DotBump.Commands.BumpTools.DataModel.Catalog;

// TODO: remove pragma and fix
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
internal record NuGetCatalogPage
{
    [JsonPropertyName("@id")]
    public string Id { get; init; }

    [JsonPropertyName("@type")]
    public string Type { get; init; }

    [JsonPropertyName("commitId")]
    public string CommitId { get; init; }

    [JsonPropertyName("commitTimeStamp")]
    public DateTimeOffset CommitTimeStamp { get; init; }

    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("items")]
    public List<PackageItem> Items { get; init; }

    [JsonPropertyName("parent")]
    public string Parent { get; init; }

    [JsonPropertyName("lower")]
    public string Lower { get; init; }

    [JsonPropertyName("upper")]
    public string Upper { get; init; }

    [JsonPropertyName("@context")]
    public Context Context { get; init; }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
