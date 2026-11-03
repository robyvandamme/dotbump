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
                    Key = "nuget.org", ProtocolVersion = "3", Value = "https://api.nuget.org/v3/index.json",
                });
            logger.MethodReturn(nameof(NuGetConfigFileService), nameof(GetNuGetConfiguration), defaultConfig);
            return defaultConfig;
        }

        var config = ReadFromConfigFile(nugetConfigPath);

        logger.MethodReturn(nameof(NuGetConfigFileService), nameof(GetNuGetConfiguration), config);
        return config;
    }

    private static void ParsePackageSources(XElement root, NuGetConfig config)
    {
        var sourceElements = root.Element("packageSources")?.Elements("add");
        if (sourceElements == null)
        {
            return;
        }

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

    private static List<string> ParsePackagePatterns(XElement sourceElement)
    {
        return sourceElement
            .Elements("package")
            .Select(element => element.Attribute("pattern")?.Value?.Trim())
            .Where(pattern => !string.IsNullOrWhiteSpace(pattern))
            .Select(pattern => pattern!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddOrMergePackageSourceMapping(
        NuGetConfig config,
        string sourceKey,
        List<string> patterns)
    {
        var existingMapping = config.PackageSourceMappings.FirstOrDefault(o => o.SourceKey.Equals(
            sourceKey,
            StringComparison.OrdinalIgnoreCase));
        if (existingMapping == null)
        {
            config.PackageSourceMappings.Add(
                new PackageSourceMappingEntry
                {
                    SourceKey = sourceKey,
                    Patterns = patterns,
                });
            return;
        }

        existingMapping.Patterns.AddRange(
            patterns.Where(pattern => !existingMapping.Patterns.Contains(
                pattern,
                StringComparer.OrdinalIgnoreCase)));
    }

    private static void ParseCredentials(XElement root, NuGetConfig config)
    {
        var credentialsElement = root.Element("packageSourceCredentials");
        if (credentialsElement == null)
        {
            return;
        }

        foreach (var sourceCredElement in credentialsElement.Elements())
        {
            ParseCredentialsForSource(sourceCredElement, config);
        }
    }

    private static void ParseCredentialsForSource(XElement sourceCredElement, NuGetConfig config)
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

    private NuGetConfig ReadFromConfigFile(string filePath)
    {
        var root = LoadConfigRoot(filePath);
        var config = new NuGetConfig();

        ParsePackageSources(root, config);
        ThrowIfNoPackageSources(root, config, filePath);
        ParsePackageSourceMappings(root, config, filePath);
        ParseCredentials(root, config);

        return config;
    }

    private XElement LoadConfigRoot(string filePath)
    {
        XDocument doc;
        try
        {
            doc = XDocument.Load(filePath);
        }
        catch (XmlException exception)
        {
            throw new DotBumpException($"Failed to read the NuGet config file at '{filePath}'.", exception);
        }

        if (doc.Root == null)
        {
            logger.Error("Unable to read the nuget config file at {FilePath}", filePath);
            throw new DotBumpException($"Unable to read the nuget config file at {filePath}.");
        }

        return doc.Root;
    }

    private void ThrowIfNoPackageSources(XElement root, NuGetConfig config, string filePath)
    {
        if (config.PackageSources.Count > 0)
        {
            return;
        }

        // No package sources found...
        var credentialSectionNames = root.Element("packageSourceCredentials")?
            .Elements()
            .Select(element => element.Name.LocalName)
            .ToList() ?? [];
        var credentialSections = credentialSectionNames.Count > 0
            ? string.Join(", ", credentialSectionNames)
            : "none";
        logger.Error(
            "No package sources were found in the NuGet config {FilePath} (credential sections: {CredentialSections})",
            filePath,
            credentialSections);
        throw new DotBumpException($"No package sources were found in the NuGet config  {filePath}");
    }

    private void ParsePackageSourceMappings(XElement root, NuGetConfig config, string filePath)
    {
        var mappingElement = root.Element("packageSourceMapping");
        if (mappingElement == null)
        {
            return;
        }

        foreach (var sourceElement in mappingElement.Elements("packageSource"))
        {
            var sourceKey = sourceElement.Attribute("key")?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(sourceKey))
            {
                logger.Warning(
                    "A packageSource element in the packageSourceMapping section of {FilePath} is missing a key and will be ignored",
                    filePath);
                continue;
            }

            AddOrMergePackageSourceMapping(config, sourceKey, ParsePackagePatterns(sourceElement));
        }
    }
}
