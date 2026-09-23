// Copyright © Roby Van Damme.

using System.Text.Json;
using DotBump.Commands.BumpTools.DataModel.LocalTools;
using DotBump.Commands.BumpTools.Interfaces;
using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpTools;

internal class ToolFileService(ILogger logger) : IToolFileService
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

    public ToolsManifest GetToolsManifest()
    {
        logger.MethodStart(nameof(ToolFileService), nameof(GetToolsManifest));

        if (!File.Exists(_defaultToolManifestPath))
        {
            throw new FileNotFoundException($"Tool manifest file not found at path: {_defaultToolManifestPath}");
        }

        var json = File.ReadAllText(_defaultToolManifestPath);
        var manifest = JsonSerializer.Deserialize<ToolsManifest>(json, s_serializerOptions);

        if (manifest == null)
        {
            throw new DotBumpException("The tool manifest file could not be deserialized.");
        }

        logger.MethodReturn(nameof(ToolFileService), nameof(GetToolsManifest), manifest);

        return manifest;
    }

    /// <summary>
    /// Saves the default tool manifest.
    /// </summary>
    /// <param name="manifest">The updated tools manifest.</param>
    /// <exception cref="DotBumpException">If the manifest can not be found.</exception>
    public void SaveToolsManifest(ToolsManifest manifest)
    {
        logger.MethodStart(nameof(ToolFileService), nameof(SaveToolsManifest));

        ArgumentNullException.ThrowIfNull(manifest);

        var json = JsonSerializer.Serialize(manifest, s_serializerOptions);

        var directoryPath = Path.GetDirectoryName(_defaultToolManifestPath);
        if (!Directory.Exists(directoryPath))
        {
            throw new DotBumpException($"Tools file directory {_defaultToolManifestPath} not found");
        }

        File.WriteAllText(_defaultToolManifestPath, json);

        logger.MethodReturn(nameof(ToolFileService), nameof(SaveToolsManifest));
    }
}
