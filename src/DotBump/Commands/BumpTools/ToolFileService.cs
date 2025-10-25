// Copyright © 2025 Roby Van Damme.

using System.Text.Json;
using System.Text.Json.Serialization;
using DotBump.Commands.BumpTools.DataModel;
using DotBump.Commands.BumpTools.Interfaces;
using DotBump.Common;

namespace DotBump.Commands.BumpTools;

internal class ToolFileService : IToolFileService
{
    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _defaultToolManifestPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        ".config",
        "dotnet-tools.json");

    public ToolManifest GetToolManifest()
    {
        if (!File.Exists(_defaultToolManifestPath))
        {
            throw new FileNotFoundException($"Tool manifest file not found at path: {_defaultToolManifestPath}");
        }

        var json = File.ReadAllText(_defaultToolManifestPath);
        var manifest = JsonSerializer.Deserialize<ToolManifest>(json, s_serializerOptions);

        if (manifest == null)
        {
            throw new DotBumpException("The tool manifest file could not be deserialized.");
        }

        return manifest;
    }
}
