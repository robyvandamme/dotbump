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

    private static readonly string[] s_packageElementNames =
        ["PackageReference", "PackageVersion", "GlobalPackageReference"];

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
            var text = manifest.GetFileText(filePath);
            if (text == null)
            {
                continue;
            }

            var updatedText = ApplyChanges(filePath, text, manifest.Packages);
            WriteText(filePath, updatedText);
            logger.Debug("Updated package versions in {File}", filePath);
        }

        logger.MethodReturn(nameof(PackageFileService), nameof(SavePackageManifest));
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
            return (attribute.Value, attribute);
        }

        var childElement = element.Elements()
            .FirstOrDefault(child => child.Name.LocalName.Equals("Version", StringComparison.Ordinal));

        return childElement != null
            ? (childElement.Value, childElement)
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

    private static string ReadText(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        var encoding = DetectEncoding(bytes);
        var preambleLength = encoding.GetPreamble().Length;

        return encoding.GetString(bytes, preambleLength, bytes.Length - preambleLength);
    }

    private static void WriteText(string filePath, string text)
    {
        // The encoding is detected from the original bytes and carries the BOM, so the file is
        // written back with the same encoding and byte order mark it had before.
        var encoding = DetectEncoding(File.ReadAllBytes(filePath));
        File.WriteAllText(filePath, text, encoding);
    }

    private static Encoding DetectEncoding(byte[] bytes)
    {
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

    private static int[] GetLineStartOffsets(string text)
    {
        var offsets = new List<int> { 0 };

        for (var i = 0; i < text.Length; i++)
        {
            var character = text[i];
            if (character == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                offsets.Add(i + 1);
            }
            else if (character == '\n')
            {
                offsets.Add(i + 1);
            }
        }

        return offsets.ToArray();
    }

    private static int GetOffset(int[] lineStartOffsets, int lineNumber, int linePosition)
    {
        var lineIndex = lineNumber - 1;
        if (lineIndex < 0 || lineIndex >= lineStartOffsets.Length)
        {
            return 0;
        }

        return lineStartOffsets[lineIndex] + (linePosition - 1);
    }

    private IEnumerable<string> EnumerateCandidateFiles(string rootPath)
    {
        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(rootPath);

        while (pendingDirectories.Count > 0)
        {
            var directory = pendingDirectories.Pop();

            foreach (var subDirectory in Directory.EnumerateDirectories(directory))
            {
                if (s_excludedDirectories.Contains(Path.GetFileName(subDirectory), StringComparer.OrdinalIgnoreCase)
                    || IsReparsePoint(subDirectory))
                {
                    continue;
                }

                pendingDirectories.Push(subDirectory);
            }

            foreach (var filePath in Directory.EnumerateFiles(directory)
                         .Where(IsCandidateFile)
                         .Where(filePath => !IsReparsePoint(filePath)))
            {
                yield return filePath;
            }
        }
    }

    private bool IsReparsePoint(string path)
    {
        try
        {
            return File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // If the entry cannot be inspected (vanished or inaccessible), skip it rather than abort the scan.
            logger.Debug(e, "Skipping {Path} because its attributes could not be read", path);
            return true;
        }
    }

    private void ReadPackageVersions(string filePath, PackageManifest manifest)
    {
        var text = ReadText(filePath);

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
        var lineStartOffsets = GetLineStartOffsets(text);
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

            if (!IsSupportedVersion(version))
            {
                var warning =
                    $"Skipping {elementName} '{packageId}' in '{filePath}' because version '{version}' is not supported.";
                logger.Debug("{Warning}", warning);
                manifest.AddWarning(warning);
                continue;
            }

            var lineInfo = (IXmlLineInfo)versionSource;
            var versionAnchor = lineInfo.HasLineInfo()
                ? GetOffset(lineStartOffsets, lineInfo.LineNumber, lineInfo.LinePosition)
                : 0;

            manifest.Add(
                new PackageVersionEntry
                {
                    PackageId = packageId,
                    OriginalVersion = version,
                    Version = version,
                    FilePath = filePath,
                    SourceKind = sourceKind,
                    ElementName = elementName,
                    VersionAnchor = versionAnchor,
                });

            packageFound = true;
        }

        if (packageFound)
        {
            manifest.RegisterFileText(filePath, text);
        }
    }

    private string ApplyChanges(string filePath, string text, IReadOnlyList<PackageVersionEntry> packages)
    {
        var edits = new List<(int Index, string OldVersion, string NewVersion)>();

        foreach (var package in packages)
        {
            if (!string.Equals(package.FilePath, filePath, StringComparison.Ordinal)
                || string.Equals(package.Version, package.OriginalVersion, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var index = text.IndexOf(package.OriginalVersion, package.VersionAnchor, StringComparison.Ordinal);
            if (index < 0)
            {
                logger.Warning(
                    "Could not locate version '{Version}' for {PackageId} in {File}; skipping.",
                    package.OriginalVersion,
                    package.PackageId,
                    filePath);
                continue;
            }

            edits.Add((index, package.OriginalVersion, package.Version));
        }

        // Apply from the end of the file backwards so earlier offsets remain valid.
        foreach (var edit in edits.OrderByDescending(edit => edit.Index))
        {
            text = text.Remove(edit.Index, edit.OldVersion.Length).Insert(edit.Index, edit.NewVersion);
        }

        return text;
    }
}
