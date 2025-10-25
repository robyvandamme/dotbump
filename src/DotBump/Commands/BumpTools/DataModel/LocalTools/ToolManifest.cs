// Copyright © 2025 Roby Van Damme.

namespace DotBump.Commands.BumpTools.DataModel.LocalTools;

/// <summary>
/// Represents the structure of the dotnet-tools.json file.
/// </summary>
internal class ToolManifest
{
    public int Version { get; set; }

    public bool IsRoot { get; set; }

    public required Dictionary<string, ToolManifestEntry> Tools { get; set; } = [];
}
