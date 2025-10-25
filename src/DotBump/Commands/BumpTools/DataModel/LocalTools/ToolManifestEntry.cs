// Copyright © 2025 Roby Van Damme.

using System.Text.Json.Serialization;
using DotBump.Common;

namespace DotBump.Commands.BumpTools.DataModel.LocalTools;

internal class ToolManifestEntry
{
    public required string Version { get; set; }

    public required List<string> Commands { get; set; }

    public bool RollForward { get; set; }

    [JsonIgnore]
    public SemanticVersion SemanticVersion => new(Version);
}
