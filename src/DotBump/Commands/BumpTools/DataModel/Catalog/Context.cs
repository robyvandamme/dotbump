// Copyright © 2025 Roby Van Damme.

using System.Text.Json.Serialization;

namespace DotBump.Commands.BumpTools.DataModel.Catalog;

// TODO: remove pragma and fix
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
internal record Context
{
    [JsonPropertyName("@id")]
    public string Id { get; init; }

    [JsonPropertyName("@type")]
    public string Type { get; init; }

    [JsonPropertyName("@container")]
    public string Container { get; init; }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
