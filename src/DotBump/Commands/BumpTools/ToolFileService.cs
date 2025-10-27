// Copyright © 2025 Roby Van Damme.

using System.Text.Json;
using System.Xml.Linq;
using DotBump.Commands.BumpTools.DataModel.LocalTools;
using DotBump.Commands.BumpTools.Interfaces;
using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpTools;

internal class ToolFileService(ILogger logger) : IToolFileService
{
    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _defaultToolManifestPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        ".config",
        "dotnet-tools.json");

    private readonly string _defaultNugetConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "nuget.config");

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

    /// <summary>
    /// Gets the package sources from the default config. If there is no default config the default nuget source is used.
    /// </summary>
    /// <returns>A list of package source URL strings.</returns>
    public IEnumerable<string> GetNuGetPackageSources()
    {
        var sources = new List<string>();

        logger.Debug("Looking for default nuget config file {ConfigFile}", _defaultNugetConfigPath);

        try
        {
            if (!File.Exists(_defaultNugetConfigPath))
            {
                logger.Debug("Default nuget config file {ConfigFile} not found", _defaultNugetConfigPath);

                sources.Add("https://api.nuget.org/v3/index.json");
                return sources;
            }

            var nugetConfig = XDocument.Load(_defaultNugetConfigPath);
            var packageSources = nugetConfig.Descendants("packageSources").Descendants("add");

            foreach (var source in packageSources)
            {
                var valueAttribute = source.Attribute("value");
                if (valueAttribute != null && !string.IsNullOrEmpty(valueAttribute.Value))
                {
                    var sourceUrl = valueAttribute.Value;
                    if (Uri.TryCreate(sourceUrl, UriKind.Absolute, out _))
                    {
                        sources.Add(sourceUrl);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error reading {NuGetConfig}", _defaultNugetConfigPath);
            throw;
        }

        return sources.Distinct().ToList();
    }

    public void SaveToolManifest(ToolManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var json = JsonSerializer.Serialize(manifest, s_serializerOptions);

        // check if the directory exists
        var directoryPath = Path.GetDirectoryName(_defaultToolManifestPath);
        if (!Directory.Exists(directoryPath))
        {
            throw new DotBumpException($"Tools file directory {_defaultToolManifestPath} not found");
        }

        File.WriteAllText(_defaultToolManifestPath, json);
    }
}
