// Copyright © 2025 Roby Van Damme.

using System.Text.Json.Serialization;
using DotBump.Common;

namespace DotBump.Commands.BumpSdk.DataModel;

public record Sdk
{
    public Sdk(string version, string rollForward)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        Version = version;
        RollForward = rollForward;
        SemanticVersion = new(version);
    }

    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("rollForward")]
    public string RollForward { get; set; }

    // public SemanticVersion? SemanticVersion
    // {
    //     get
    //     {
    //         if (string.IsNullOrWhiteSpace(this.Version))
    //         {
    //             return null;
    //         }
    //
    //         if (this._semanticVersion != null)
    //         {
    //             return this._semanticVersion;
    //         }
    //
    //         this._semanticVersion = new SemanticVersion(this.Version);
    //         return this._semanticVersion;
    //     }
    // }
    [JsonIgnore]
    public SemanticVersion SemanticVersion { get; set; }
}
