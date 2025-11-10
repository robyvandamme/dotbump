// Copyright © 2025 Roby Van Damme.

using System.Text.Json.Serialization;
using Destructurama.Attributed;
using DotBump.Common;

namespace DotBump.Commands.BumpTools.DataModel.Registrations;

internal record CatalogPage
{
    [JsonPropertyName("@id")]
    public required string Id { get; init; }

    [JsonPropertyName("@type")]
    public string? Type { get; init; }

    [JsonPropertyName("count")]
    public int Count { get; init; }

    [NotLogged]
    [JsonPropertyName("items")]
    public List<Package>?
        Items
    {
        get;
        init;
    }

    [JsonPropertyName("lower")]
    public required string Lower { get; init; }

    [JsonPropertyName("upper")]
    public required string Upper { get; init; }

    [JsonIgnore]
    public SemanticVersion LowerSemanticVersion
    {
        get { return new SemanticVersion(Lower); }
    }

    [JsonIgnore]
    public SemanticVersion UpperSemanticVersion
    {
        get { return new SemanticVersion(Upper); }
    }

    [JsonIgnore]
    public bool HasPackageDetails
    {
        get
        {
            if (Items == null)
            {
                return false;
            }

            if (Items.Count == 0)
            {
                return false;
            }

            return true;
        }
    }
}
