// Copyright © 2025 Roby Van Damme.

using System.Text.Json.Serialization;
using DotBump.Common;

namespace DotBump.Commands.BumpTools.DataModel.Catalog;

// TODO: remove pragma and fix
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
internal record CatalogEntry
{
    [JsonPropertyName("@id")]
    public string Id { get; init; }

    [JsonPropertyName("@type")]
    public string Type { get; init; }

    [JsonPropertyName("authors")]
    public string Authors { get; init; }

    [JsonPropertyName("description")]
    public string Description { get; init; }

    [JsonPropertyName("iconUrl")]
    public string IconUrl { get; init; }

    [JsonPropertyName("id")]
    public string PackageId { get; init; }

    [JsonPropertyName("language")]
    public string Language { get; init; }

    [JsonPropertyName("licenseExpression")]
    public string LicenseExpression { get; init; }

    [JsonPropertyName("licenseUrl")]
    public string LicenseUrl { get; init; }

    [JsonPropertyName("readmeUrl")]
    public string ReadmeUrl { get; init; }

    [JsonPropertyName("listed")]
    public bool Listed { get; init; }

    [JsonPropertyName("minClientVersion")]
    public string MinClientVersion { get; init; }

    [JsonPropertyName("packageContent")]
    public string PackageContent { get; init; }

    [JsonPropertyName("projectUrl")]
    public string ProjectUrl { get; init; }

    [JsonPropertyName("published")]
    public DateTimeOffset Published { get; init; }

    [JsonPropertyName("requireLicenseAcceptance")]
    public bool RequireLicenseAcceptance { get; init; }

    [JsonPropertyName("summary")]
    public string Summary { get; init; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; }

    [JsonPropertyName("version")]
    public string Version { get; init; }

    [JsonIgnore]
    public SemanticVersion SemanticVersion
    {
        get { return new SemanticVersion(Version); }
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
