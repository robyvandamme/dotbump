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

    public ToolsManifest GetToolsManifest(string manifestPath)
    {
        logger.MethodStart(nameof(ToolFileService), nameof(GetToolsManifest));

        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);

        var fullPath = Path.GetFullPath(manifestPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Tool manifest file not found at path: {fullPath}");
        }

        var json = File.ReadAllText(fullPath);
        var manifest = JsonSerializer.Deserialize<ToolsManifest>(json, s_serializerOptions);

        if (manifest == null)
        {
            throw new DotBumpException("The tool manifest file could not be deserialized.");
        }

        logger.MethodReturn(nameof(ToolFileService), nameof(GetToolsManifest), manifest);

        return manifest;
    }

    /// <summary>
    /// Saves the tool manifest.
    /// </summary>
    /// <param name="manifest">The updated tools manifest.</param>
    /// <param name="manifestPath">The path of the tools manifest to save.</param>
    /// <exception cref="DotBumpException">If the manifest can not be found.</exception>
    public void SaveToolsManifest(ToolsManifest manifest, string manifestPath)
    {
        logger.MethodStart(nameof(ToolFileService), nameof(SaveToolsManifest));

        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);

        var fullPath = Path.GetFullPath(manifestPath);
        var json = JsonSerializer.Serialize(manifest, s_serializerOptions);

        var directoryPath = Path.GetDirectoryName(fullPath);
        if (!Directory.Exists(directoryPath))
        {
            throw new DotBumpException($"Tools file directory {fullPath} not found");
        }

        File.WriteAllText(fullPath, json);

        logger.MethodReturn(nameof(ToolFileService), nameof(SaveToolsManifest));
    }
}
