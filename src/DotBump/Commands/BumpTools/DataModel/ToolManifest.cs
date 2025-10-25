// Copyright © 2025 Roby Van Damme.

using System.Text.Json.Serialization;

namespace DotBump.Commands.BumpTools.DataModel;

/// <summary>
/// Represents the structure of the dotnet-tools.json file.
/// </summary>
internal class ToolManifest
{
    public int Version { get; set; }

    public bool IsRoot { get; set; }

    public required Dictionary<string, ToolManifestEntry> Tools { get; set; } = [];
}
