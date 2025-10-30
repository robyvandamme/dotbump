// Copyright © 2025 Roby Van Damme.

using System.Text.Json;
using System.Xml.Linq;
using DotBump.Commands.BumpTools.DataModel.LocalTools;
using DotBump.Commands.BumpTools.DataModel.NuGetConfig;
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
        logger.MethodStart(nameof(ToolFileService), nameof(GetToolManifest));

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

        logger.MethodReturn(nameof(ToolFileService), nameof(GetToolManifest), manifest);

        return manifest;
    }

    /// <summary>
    /// Gets the package sources from the default config. If there is no default config the default nuget source is used.
    /// </summary>
    /// <returns>A list of package source URL strings.</returns>
    public NuGetConfig GetNuGetConfiguration()
    {
        logger.MethodStart(nameof(ToolFileService), nameof(GetNuGetConfiguration));

        logger.Debug("Looking for default nuget config file {ConfigFile}", _defaultNugetConfigPath);

        if (!File.Exists(_defaultNugetConfigPath))
        {
            logger.Debug(
                "Default nuget config file {ConfigFile} not found, using default source https://api.nuget.org/v3/index.json",
                _defaultNugetConfigPath);
            var defaultConfig = new NuGetConfig();
            defaultConfig.PackageSources.Add(
                new PackageSource
                {
                    Key = "nuget.org", ProtocolVersion = "3", Value = "https://api.nuget.org/v3/index.json",
                });
            logger.MethodReturn(nameof(ToolFileService), nameof(GetNuGetConfiguration), defaultConfig);
            return defaultConfig;
        }

        var config = ReadFromConfigFile(_defaultNugetConfigPath);

        logger.MethodReturn(nameof(ToolFileService), nameof(GetNuGetConfiguration), config);
        return config;
    }

    public void SaveToolManifest(ToolManifest manifest)
    {
        logger.MethodStart(nameof(ToolFileService), nameof(SaveToolManifest));

        ArgumentNullException.ThrowIfNull(manifest);

        var json = JsonSerializer.Serialize(manifest, s_serializerOptions);

        var directoryPath = Path.GetDirectoryName(_defaultToolManifestPath);
        if (!Directory.Exists(directoryPath))
        {
            throw new DotBumpException($"Tools file directory {_defaultToolManifestPath} not found");
        }

        File.WriteAllText(_defaultToolManifestPath, json);

        logger.MethodReturn(nameof(ToolFileService), nameof(SaveToolManifest));
    }

    private NuGetConfig ReadFromConfigFile(string filePath)
    {
        var config = new NuGetConfig();
        var doc = XDocument.Load(filePath);

        // Parse package sources
        var sourceElements = doc.Root.Element("packageSources")?.Elements("add");
        if (sourceElements != null)
        {
            foreach (var element in sourceElements)
            {
                config.PackageSources.Add(
                    new PackageSource
                    {
                        Key = element.Attribute("key")?.Value ?? string.Empty,
                        Value = element.Attribute("value")?.Value ?? string.Empty,
                        ProtocolVersion = element.Attribute("protocolVersion")?.Value ?? string.Empty,
                    });
            }
        }

        // Parse credentials
        var credentialsElement = doc.Root.Element("packageSourceCredentials");
        if (credentialsElement != null)
        {
            foreach (var sourceCredElement in credentialsElement.Elements())
            {
                // The element name itself is the source name (like "myorg")
                var sourceName = sourceCredElement.Name.LocalName;
                var sourceCred = new SourceCredential { SourceName = sourceName };

                foreach (var addElement in sourceCredElement.Elements("add"))
                {
                    sourceCred.Credentials.Add(
                        new Credential
                        {
                            Key = addElement.Attribute("key")?.Value ?? string.Empty,
                            Value = addElement.Attribute("value")?.Value ?? string.Empty,
                        });
                }

                config.Credentials[sourceName] = sourceCred;
            }
        }

        return config;
    }
}
