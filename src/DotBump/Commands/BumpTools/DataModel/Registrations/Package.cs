// Copyright © 2025 Roby Van Damme.

using System.Text.Json.Serialization;

namespace DotBump.Commands.BumpTools.DataModel.Registrations;

// TODO: remove pragma and fix
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
internal record Package
{
    [JsonPropertyName("@id")]
    public string Id { get; init; }

    // [JsonPropertyName("@type")]
    // public string Type { get; init; }

    // [JsonPropertyName("commitId")]
    // public string CommitId { get; init; }

    // [JsonPropertyName("commitTimeStamp")]
    // public DateTime CommitTimeStamp { get; init; }

    [JsonPropertyName("catalogEntry")]
    public PackageDetails CatalogEntry { get; init; }

    // [JsonPropertyName("packageContent")]
    // public string PackageContent { get; init; }

    // [JsonPropertyName("registration")]
    // public string Registration { get; init; }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
