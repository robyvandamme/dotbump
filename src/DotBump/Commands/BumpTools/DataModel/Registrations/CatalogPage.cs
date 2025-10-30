// Copyright © 2025 Roby Van Damme.

using System.Text.Json.Serialization;
using DotBump.Common;

namespace DotBump.Commands.BumpTools.DataModel.Registrations;

// TODO: remove pragma and fix
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
internal record CatalogPage
{
    [JsonPropertyName("@id")]
    public string Id { get; init; }

    [JsonPropertyName("@type")]
    public string Type { get; init; }

    [JsonPropertyName("commitId")]
    public string CommitId { get; init; }

    [JsonPropertyName("commitTimeStamp")]
    public DateTime CommitTimeStamp { get; init; }

    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("items")]
    public List<Package>
        Items
    {
        get;
        init;
    } // If there are no package items we need to get the page that contains the information we are looking for

    [JsonPropertyName("parent")]
    public string Parent { get; init; }

    [JsonPropertyName("lower")]
    public string Lower { get; init; }

    [JsonPropertyName("upper")]
    public string Upper { get; init; }

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
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
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
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
