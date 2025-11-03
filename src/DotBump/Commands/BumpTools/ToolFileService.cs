// Copyright © 2025 Roby Van Damme.

using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using DotBump.Commands.BumpTools.DataModel.LocalTools;
using DotBump.Commands.BumpTools.DataModel.NuGetConfiguration;
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

    public ToolsManifest GetToolManifest()
    {
        logger.MethodStart(nameof(ToolFileService), nameof(GetToolManifest));

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

        logger.MethodReturn(nameof(ToolFileService), nameof(GetToolManifest), manifest);

        return manifest;
    }

    /// <summary>
    /// Tries to read a NuGet configuration named "nuget.config" in the current directory.
    /// If not found a default NuGet configuration is returned with the default https://api.nuget.org/v3/index.json package source.
    /// </summary>
    /// <returns>A NuGet configuration.</returns>
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

    /// <summary>
    /// Saves the default tool manifest.
    /// </summary>
    /// <param name="manifest">The updated tools manifest.</param>
    /// <exception cref="DotBumpException">If the manifest can not be found.</exception>
    public void SaveToolManifest(ToolsManifest manifest)
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

    internal NuGetConfig ReadFromConfigFile(string filePath)
    {
        var config = new NuGetConfig();
        XDocument doc;
        try
        {
            doc = XDocument.Load(filePath);
        }
        catch (XmlException exception)
        {
            logger.Error(exception, "An error occured trying to load the NuGet config file {FilePath}", filePath);
            throw;
        }

        if (doc.Root == null)
        {
            logger.Error("Unable to read the nuget config file at {FilePath} with {Content}", filePath, doc);
            throw new DotBumpException($"Unable to read the nuget config file at {filePath}.");
        }

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

        if (config.PackageSources.Count == 0)
        {
            // No package sources found...
            logger.Error(
                "No package sources were found in the NuGet config {FilePath} with content {Content}",
                filePath,
                doc);
            throw new DotBumpException($"No package sources were found in the NuGet config  {filePath}");
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
