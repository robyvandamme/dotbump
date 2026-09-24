// Copyright © Roby Van Damme.

using System.Text;
using System.Xml;
using System.Xml.Linq;
using DotBump.Commands.BumpPackages.DataModel;
using DotBump.Commands.BumpPackages.Interfaces;
using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpPackages;

internal sealed class PackageFileService(ILogger logger) : IPackageFileService
{
    private const string DirectoryPackagesPropsFileName = "Directory.Packages.props";
    private const string DirectoryBuildPropsFileName = "Directory.Build.props";
    private const string DirectoryBuildTargetsFileName = "Directory.Build.targets";

    private static readonly string[] s_projectExtensions = [".csproj", ".fsproj", ".vbproj"];
    private static readonly string[] s_excludedDirectories = ["bin", "obj", ".git", ".vs", "node_modules"];
    private static readonly string[] s_packageElementNames = ["PackageReference", "PackageVersion", "GlobalPackageReference"];

    public PackageManifest GetPackageManifest(string repositoryPath)
    {
        logger.MethodStart(nameof(PackageFileService), nameof(GetPackageManifest), repositoryPath);

        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryPath);

        var rootPath = Path.GetFullPath(repositoryPath);
        if (!Directory.Exists(rootPath))
        {
            throw new DotBumpException($"Repository directory '{rootPath}' does not exist.");
        }

        var manifest = new PackageManifest();

        foreach (var filePath in EnumerateCandidateFiles(rootPath))
        {
            ReadPackageVersions(filePath, manifest);
        }

        logger.MethodReturn(nameof(PackageFileService), nameof(GetPackageManifest), manifest);

        return manifest;
    }

    public void SavePackageManifest(PackageManifest manifest)
    {
        logger.MethodStart(nameof(PackageFileService), nameof(SavePackageManifest));

        ArgumentNullException.ThrowIfNull(manifest);

        foreach (var filePath in manifest.ChangedFiles)
        {
            var document = manifest.GetDocument(filePath);
            if (document == null)
            {
                continue;
            }

            WriteDocument(filePath, document);
            logger.Debug("Updated package versions in {File}", filePath);
        }

        logger.MethodReturn(nameof(PackageFileService), nameof(SavePackageManifest));
    }

    private static IEnumerable<string> EnumerateCandidateFiles(string rootPath)
    {
        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(rootPath);

        while (pendingDirectories.Count > 0)
        {
            var directory = pendingDirectories.Pop();

            foreach (var subDirectory in Directory.EnumerateDirectories(directory))
            {
                if (s_excludedDirectories.Contains(Path.GetFileName(subDirectory), StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                pendingDirectories.Push(subDirectory);
            }

            foreach (var filePath in Directory.EnumerateFiles(directory).Where(IsCandidateFile))
            {
                yield return filePath;
            }
        }
    }

    private static bool IsCandidateFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);

        if (fileName.Equals(DirectoryPackagesPropsFileName, StringComparison.OrdinalIgnoreCase)
            || fileName.Equals(DirectoryBuildPropsFileName, StringComparison.OrdinalIgnoreCase)
            || fileName.Equals(DirectoryBuildTargetsFileName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return s_projectExtensions.Contains(Path.GetExtension(filePath), StringComparer.OrdinalIgnoreCase);
    }

    private static PackageSourceKind GetSourceKind(string filePath)
    {
        var fileName = Path.GetFileName(filePath);

        if (fileName.Equals(DirectoryPackagesPropsFileName, StringComparison.OrdinalIgnoreCase))
        {
            return PackageSourceKind.CentralPackageManagement;
        }

        if (fileName.Equals(DirectoryBuildPropsFileName, StringComparison.OrdinalIgnoreCase)
            || fileName.Equals(DirectoryBuildTargetsFileName, StringComparison.OrdinalIgnoreCase))
        {
            return PackageSourceKind.SharedMsBuild;
        }

        return PackageSourceKind.Project;
    }

    private static string? ReadAttribute(XElement element, string attributeName)
    {
        return element.Attributes()
            .FirstOrDefault(attribute => attribute.Name.LocalName.Equals(attributeName, StringComparison.Ordinal))
            ?.Value;
    }

    private static (string? Version, XAttribute? Attribute, XElement? Element) ReadVersion(XElement element)
    {
        var attribute = element.Attributes()
            .FirstOrDefault(attribute => attribute.Name.LocalName.Equals("Version", StringComparison.Ordinal))
            ?? element.Attributes()
                .FirstOrDefault(attribute => attribute.Name.LocalName.Equals("VersionOverride", StringComparison.Ordinal));

        if (attribute != null)
        {
            return (attribute.Value, attribute, null);
        }

        var childElement = element.Elements()
            .FirstOrDefault(child => child.Name.LocalName.Equals("Version", StringComparison.Ordinal));

        return childElement != null
            ? (childElement.Value, null, childElement)
            : (null, null, null);
    }

    private static bool IsSupportedVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        if (version.Contains("$(", StringComparison.Ordinal) || version.Contains('*'))
        {
            return false;
        }

        return version.IndexOfAny(['[', ']', '(', ')', ',']) < 0;
    }

    private static void WriteDocument(string filePath, XDocument document)
    {
        var settings = new XmlWriterSettings
        {
            Encoding = DetectEncoding(filePath),
            Indent = false,
            NewLineHandling = NewLineHandling.None,
            OmitXmlDeclaration = document.Declaration == null,
        };

        using var writer = XmlWriter.Create(filePath, settings);
        document.Save(writer);
    }

    private static Encoding DetectEncoding(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return new UTF8Encoding(true);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return new UnicodeEncoding(false, true);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return new UnicodeEncoding(true, true);
        }

        return new UTF8Encoding(false);
    }

    private void ReadPackageVersions(string filePath, PackageManifest manifest)
    {
        XDocument document;
        try
        {
            document = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);
        }
        catch (XmlException e)
        {
            throw new DotBumpException($"Failed to read '{filePath}'.", e);
        }

        var root = document.Root;
        if (root == null)
        {
            return;
        }

        var sourceKind = GetSourceKind(filePath);
        var packageFound = false;

        foreach (var element in root.Descendants())
        {
            var elementName = element.Name.LocalName;
            if (!s_packageElementNames.Contains(elementName))
            {
                continue;
            }

            var packageId = ReadAttribute(element, "Include") ?? ReadAttribute(element, "Update");
            if (string.IsNullOrWhiteSpace(packageId))
            {
                continue;
            }

            var (version, versionAttribute, versionElement) = ReadVersion(element);

            if (version == null)
            {
                logger.Debug(
                    "Skipping {Element} {PackageId} in {File} because it has no version",
                    elementName,
                    packageId,
                    filePath);
                continue;
            }

            if (!IsSupportedVersion(version))
            {
                var warning =
                    $"Skipping {elementName} '{packageId}' in '{filePath}' because version '{version}' is not supported.";
                logger.Debug("{Warning}", warning);
                manifest.AddWarning(warning);
                continue;
            }

            manifest.Add(new PackageVersionEntry
            {
                PackageId = packageId,
                OriginalVersion = version,
                Version = version,
                FilePath = filePath,
                SourceKind = sourceKind,
                ElementName = elementName,
                Element = element,
                VersionAttribute = versionAttribute,
                VersionElement = versionElement,
            });

            packageFound = true;
        }

        if (packageFound)
        {
            manifest.RegisterDocument(filePath, document);
        }
    }
}
