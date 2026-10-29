// Copyright © Roby Van Damme.

using System.Net;
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

    private static readonly string[] s_packageElementNames =
        ["PackageReference", "PackageVersion", "GlobalPackageReference"];

    public PackageManifest GetPackageManifest(
        string repositoryPath,
        IReadOnlyCollection<string>? excludedPaths = null)
    {
        logger.MethodStart(nameof(PackageFileService), nameof(GetPackageManifest), repositoryPath);

        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryPath);

        var rootPath = Path.GetFullPath(repositoryPath);
        if (!Directory.Exists(rootPath))
        {
            throw new DotBumpException($"Repository directory '{rootPath}' does not exist.");
        }

        var manifest = new PackageManifest();
        var scanner = new RepositoryFileScanner(logger);

        foreach (var filePath in scanner.EnumerateCandidateFiles(rootPath, excludedPaths, IsCandidateFile))
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
            var text = manifest.GetFileText(filePath);
            if (text == null)
            {
                continue;
            }

            var updatedText = ApplyChanges(manifest, filePath, text);
            XmlFileText.WriteText(filePath, updatedText);
            logger.Debug("Updated package versions in {File}", filePath);
        }

        logger.MethodReturn(nameof(PackageFileService), nameof(SavePackageManifest));
    }

    internal static int GetOffset(int[] lineStartOffsets, int lineNumber, int linePosition)
    {
        return XmlFileText.GetOffset(lineStartOffsets, lineNumber, linePosition);
    }

    internal static (int Start, int Length) GetVersionSpan(string text, int[] lineStartOffsets, XObject source)
    {
        return source switch
        {
            XAttribute attribute => XmlFileText.GetAttributeValueSpan(text, lineStartOffsets, attribute),
            XElement element => XmlFileText.GetElementTextSpan(text, lineStartOffsets, element),
            _ => (-1, 0),
        };
    }

    internal static bool IsExcludedDirectory(string directory, HashSet<string> excludedPaths)
    {
        return RepositoryFileScanner.IsExcludedDirectory(directory, excludedPaths);
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

    // The returned version is the XML-decoded value (XAttribute.Value / XElement.Value). The matching
    // span captured by GetVersionSpan addresses the raw text instead, so the two can legitimately
    // differ for entity-encoded values (e.g. "13&#46;0&#46;1" decodes to "13.0.1"). This is why a
    // decoded-vs-raw equality guard was deliberately removed (commit eb01a576) and must not be
    // reintroduced.
    private static (string? Version, XObject? Source) ReadVersion(XElement element)
    {
        var attribute = element.Attributes()
                            .FirstOrDefault(attribute =>
                                attribute.Name.LocalName.Equals("Version", StringComparison.Ordinal))
                        ?? element.Attributes()
                            .FirstOrDefault(attribute => attribute.Name.LocalName.Equals(
                                "VersionOverride",
                                StringComparison.Ordinal));

        if (attribute != null)
        {
            return (attribute.Value.Trim(), attribute);
        }

        var childElement = element.Elements()
            .FirstOrDefault(child => child.Name.LocalName.Equals("Version", StringComparison.Ordinal));

        return childElement != null
            ? (childElement.Value.Trim(), childElement)
            : (null, null);
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

    /// <summary>
    /// Splits a raw Sdk attribute value on semicolons and returns each versioned SDK reference with
    /// the span of its version, so only the version part is rewritten.
    /// </summary>
    private static IEnumerable<(string PackageId, string Version, int VersionStart, int VersionLength)>
        ParseSdkAttributeReferences(string text, int valueStart, int valueLength)
    {
        var valueEnd = valueStart + valueLength;
        var referenceStart = valueStart;

        while (referenceStart < valueEnd)
        {
            var separatorIndex = text.IndexOf(';', referenceStart, valueEnd - referenceStart);
            var referenceEnd = separatorIndex < 0 ? valueEnd : separatorIndex;

            var reference = TryParseSdkReference(text, referenceStart, referenceEnd);
            if (reference != null)
            {
                yield return reference.Value;
            }

            if (separatorIndex < 0)
            {
                yield break;
            }

            referenceStart = separatorIndex + 1;
        }
    }

    private static (string PackageId, string Version, int VersionStart, int VersionLength)?
        TryParseSdkReference(string text, int referenceStart, int referenceEnd)
    {
        // MSBuild SDK references are written as <id>/<version>. The id itself never contains a slash.
        var slashIndex = referenceEnd > referenceStart
            ? text.LastIndexOf('/', referenceEnd - 1, referenceEnd - referenceStart)
            : -1;

        if (slashIndex < referenceStart)
        {
            return null;
        }

        var packageId = WebUtility.HtmlDecode(text[referenceStart..slashIndex].Trim()).Trim();
        if (packageId.Length == 0)
        {
            return null;
        }

        var (versionStart, versionLength) = XmlFileText.TrimSpan(text, slashIndex + 1, referenceEnd);
        if (versionLength <= 0)
        {
            return null;
        }

        return (
            packageId,
            WebUtility.HtmlDecode(text.Substring(versionStart, versionLength)),
            versionStart,
            versionLength);
    }

    private void ReadPackageVersions(string filePath, PackageManifest manifest)
    {
        var text = XmlFileText.ReadText(filePath);

        XDocument document;
        try
        {
            document = XDocument.Load(
                new StringReader(text),
                LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
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
        var lineStartOffsets = XmlFileText.GetLineStartOffsets(text);
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

            var (version, versionSource) = ReadVersion(element);

            if (version == null || versionSource == null)
            {
                logger.Debug(
                    "Skipping {Element} {PackageId} in {File} because it has no version",
                    elementName,
                    packageId,
                    filePath);
                continue;
            }

            if (TryAddVersion(
                    manifest,
                    filePath,
                    sourceKind,
                    elementName,
                    packageId,
                    version,
                    () => GetVersionSpan(text, lineStartOffsets, versionSource)))
            {
                packageFound = true;
            }
        }

        if (sourceKind == PackageSourceKind.Project
            && ReadSdkReferences(root, text, lineStartOffsets, manifest, filePath))
        {
            packageFound = true;
        }

        if (packageFound)
        {
            manifest.RegisterFileText(filePath, text);
        }
    }

    /// <summary>
    /// Reads versioned MSBuild project SDK references from the root Sdk attribute and Sdk elements,
    /// for example <c>Sdk="Aspire.AppHost.Sdk/13.0.1"</c>.
    /// </summary>
    private bool ReadSdkReferences(
        XElement root,
        string text,
        int[] lineStartOffsets,
        PackageManifest manifest,
        string filePath)
    {
        var attributeReferenceFound =
            ReadSdkAttributeReferences(root, text, lineStartOffsets, manifest, filePath);
        var elementReferenceFound =
            ReadSdkElementReferences(root, text, lineStartOffsets, manifest, filePath);

        return attributeReferenceFound || elementReferenceFound;
    }

    private bool ReadSdkAttributeReferences(
        XElement root,
        string text,
        int[] lineStartOffsets,
        PackageManifest manifest,
        string filePath)
    {
        var sdkAttribute = root.Attributes()
            .FirstOrDefault(attribute => attribute.Name.LocalName.Equals("Sdk", StringComparison.Ordinal));
        if (sdkAttribute == null)
        {
            return false;
        }

        var (valueStart, valueLength) = XmlFileText.GetAttributeValueSpan(text, lineStartOffsets, sdkAttribute);
        if (valueStart < 0)
        {
            var warning =
                $"Skipping SDK references in '{filePath}' because the Sdk attribute value could not be located.";
            logger.Warning("{Warning}", warning);
            manifest.AddWarning(warning);
            return false;
        }

        var sdkReferenceFound = false;
        foreach (var reference in ParseSdkAttributeReferences(text, valueStart, valueLength))
        {
            if (TryAddVersion(
                    manifest,
                    filePath,
                    PackageSourceKind.Project,
                    "Sdk",
                    reference.PackageId,
                    reference.Version,
                    () => (reference.VersionStart, reference.VersionLength)))
            {
                sdkReferenceFound = true;
            }
        }

        return sdkReferenceFound;
    }

    private bool ReadSdkElementReferences(
        XElement root,
        string text,
        int[] lineStartOffsets,
        PackageManifest manifest,
        string filePath)
    {
        var sdkReferenceFound = false;

        foreach (var element in root.Descendants().Where(element =>
                     element.Name.LocalName.Equals("Sdk", StringComparison.Ordinal)))
        {
            var packageId = ReadAttribute(element, "Name");
            if (string.IsNullOrWhiteSpace(packageId))
            {
                continue;
            }

            var (version, versionSource) = ReadVersion(element);
            if (version == null || versionSource == null)
            {
                logger.Debug(
                    "Skipping Sdk {PackageId} in {File} because it has no version",
                    packageId,
                    filePath);
                continue;
            }

            if (TryAddVersion(
                    manifest,
                    filePath,
                    PackageSourceKind.Project,
                    "Sdk",
                    packageId,
                    version,
                    () => GetVersionSpan(text, lineStartOffsets, versionSource)))
            {
                sdkReferenceFound = true;
            }
        }

        return sdkReferenceFound;
    }

    private bool TryAddVersion(
        PackageManifest manifest,
        string filePath,
        PackageSourceKind sourceKind,
        string elementName,
        string packageId,
        string version,
        Func<(int Start, int Length)> getVersionSpan)
    {
        if (!IsSupportedVersion(version))
        {
            var warning =
                $"Skipping {elementName} '{packageId}' in '{filePath}' because version '{version}' is not supported.";
            logger.Warning("{Warning}", warning);
            manifest.AddWarning(warning);
            return false;
        }

        var (versionStart, versionLength) = getVersionSpan();
        if (versionStart < 0)
        {
            var warning =
                $"Skipping {elementName} '{packageId}' in '{filePath}' because its version value could not be located.";
            logger.Warning("{Warning}", warning);
            manifest.AddWarning(warning);
            return false;
        }

        manifest.Add(
            new PackageVersionEntry
            {
                PackageId = packageId,
                OriginalVersion = version,
                Version = version,
                FilePath = filePath,
                SourceKind = sourceKind,
                ElementName = elementName,
                VersionStart = versionStart,
                VersionLength = versionLength,
            });

        return true;
    }

    private string ApplyChanges(PackageManifest manifest, string filePath, string text)
    {
        var edits = new List<(int Start, int Length, string NewVersion)>();

        foreach (var package in manifest.Packages)
        {
            if (!string.Equals(package.FilePath, filePath, StringComparison.Ordinal)
                || string.Equals(package.Version, package.OriginalVersion, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Bounds check only: the span content is intentionally not compared to OriginalVersion,
            // because the span covers the raw (possibly entity-encoded) text while OriginalVersion
            // holds the decoded value (see ReadVersion). Content drift is impossible here anyway —
            // `text` is the same captured string the span was computed from (GetFileText), so a wrong
            // span can only come from capture, which now fails fast at read time (review H2).
            if (package.VersionStart < 0
                || package.VersionLength <= 0
                || package.VersionStart + package.VersionLength > text.Length)
            {
                var warning =
                    $"Skipping version '{package.OriginalVersion}' for '{package.PackageId}' in '{filePath}' because its recorded span is invalid.";
                logger.Warning("{Warning}", warning);
                manifest.AddWarning(warning);
                continue;
            }

            edits.Add((package.VersionStart, package.VersionLength, package.Version));
        }

        // Apply from the end of the file backwards so earlier offsets remain valid.
        foreach (var edit in edits.OrderByDescending(edit => edit.Start))
        {
            text = text.Remove(edit.Start, edit.Length).Insert(edit.Start, edit.NewVersion);
        }

        return text;
    }
}
