// Copyright © 2025 Roby Van Damme.

using System.Xml.Linq;

namespace DotBump.Commands.BumpTools.DataModel.NuGetConfiguration;

internal class NuGetConfiguration
{
    public List<PackageSource> PackageSources { get; set; } = new List<PackageSource>();

    public Dictionary<string, SourceCredential> Credentials { get; set; } = new Dictionary<string, SourceCredential>();

    public static NuGetConfiguration Load(string filePath)
    {
        var config = new NuGetConfiguration();
        XDocument doc = XDocument.Load(filePath);

        // Parse package sources
        var sourceElements = doc.Root.Element("packageSources")?.Elements("add");
        if (sourceElements != null)
        {
            foreach (var element in sourceElements)
            {
                config.PackageSources.Add(
                    new PackageSource
                    {
                        Key = element.Attribute("key")?.Value,
                        Value = element.Attribute("value")?.Value,
                        ProtocolVersion = element.Attribute("protocolVersion")?.Value
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
                string sourceName = sourceCredElement.Name.LocalName;
                var sourceCred = new SourceCredential { SourceName = sourceName };

                foreach (var addElement in sourceCredElement.Elements("add"))
                {
                    sourceCred.Credentials.Add(
                        new Credential
                        {
                            Key = addElement.Attribute("key")?.Value, Value = addElement.Attribute("value")?.Value
                        });
                }

                config.Credentials[sourceName] = sourceCred;
            }
        }

        return config;
    }
}
