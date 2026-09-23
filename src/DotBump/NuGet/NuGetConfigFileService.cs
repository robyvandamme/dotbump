// Copyright © Roby Van Damme.

using System.Xml;
using System.Xml.Linq;
using DotBump.Common;
using DotBump.NuGet.DataModel.NuGetConfiguration;
using DotBump.NuGet.Interfaces;
using Serilog;

namespace DotBump.NuGet;

internal class NuGetConfigFileService(ILogger logger) : INuGetConfigFileService
{
    /// <summary>
    /// Tries to read the NuGet configuration from the nugetConfigPath.
    /// If not found a default NuGet configuration is returned with the default https://api.nuget.org/v3/index.json package source.
    /// </summary>
    /// <param name="nugetConfigPath">The NuGet config file path.</param>
    /// <returns>A NuGet configuration.</returns>
    public NuGetConfig GetNuGetConfiguration(string nugetConfigPath)
    {
        logger.MethodStart(nameof(NuGetConfigFileService), nameof(GetNuGetConfiguration), nugetConfigPath);

        logger.Debug("Looking for the nuget config file {ConfigFile}", nugetConfigPath);

        if (!File.Exists(nugetConfigPath))
        {
            logger.Debug(
                "Nuget config file {ConfigFile} not found, using default source https://api.nuget.org/v3/index.json",
                nugetConfigPath);
            var defaultConfig = new NuGetConfig();
            defaultConfig.PackageSources.Add(
                new PackageSource
                {
                    Key = "nuget.org",
                    ProtocolVersion = "3",
                    Value = "https://api.nuget.org/v3/index.json",
                });
            logger.MethodReturn(nameof(NuGetConfigFileService), nameof(GetNuGetConfiguration), defaultConfig);
            return defaultConfig;
        }

        var config = ReadFromConfigFile(nugetConfigPath);

        logger.MethodReturn(nameof(NuGetConfigFileService), nameof(GetNuGetConfiguration), config);
        return config;
    }

    private NuGetConfig ReadFromConfigFile(string filePath)
    {
        var config = new NuGetConfig();
        XDocument doc;
        try
        {
            doc = XDocument.Load(filePath);
        }
        catch (XmlException exception)
        {
            logger.Error(exception, "An error occurred trying to load the NuGet config file {FilePath}", filePath);
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
