// Copyright © Roby Van Damme.

using System.Xml;
using System.Xml.Linq;
using DotBump.Commands.BumpLanguage.DataModel;
using DotBump.Commands.BumpLanguage.Interfaces;
using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpLanguage;

internal sealed class LanguageFileService(ILogger logger) : ILanguageFileService
{
    private const string DirectoryBuildPropsFileName = "Directory.Build.props";
    private const string DirectoryBuildTargetsFileName = "Directory.Build.targets";
    private const string LangVersionElementName = "LangVersion";

    private static readonly string[] s_projectExtensions = [".csproj"];

    public LanguageManifest GetLanguageManifest(
        string repositoryPath,
        IReadOnlyCollection<string>? excludedPaths = null)
    {
        logger.MethodStart(nameof(LanguageFileService), nameof(GetLanguageManifest), repositoryPath);

        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryPath);

        var rootPath = Path.GetFullPath(repositoryPath);
        if (!Directory.Exists(rootPath))
        {
            throw new DotBumpException($"Repository directory '{rootPath}' does not exist.");
        }

        var manifest = new LanguageManifest();
        var scanner = new RepositoryFileScanner(logger);

        foreach (var filePath in scanner.EnumerateCandidateFiles(rootPath, excludedPaths, IsCandidateFile))
        {
            ReadLanguageVersions(rootPath, filePath, manifest);
        }

        logger.MethodReturn(nameof(LanguageFileService), nameof(GetLanguageManifest), manifest);

        return manifest;
    }

    public void SaveLanguageManifest(LanguageManifest manifest)
    {
        logger.MethodStart(nameof(LanguageFileService), nameof(SaveLanguageManifest));

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
            logger.Debug("Updated language versions in {File}", filePath);
        }

        logger.MethodReturn(nameof(LanguageFileService), nameof(SaveLanguageManifest));
    }

    private static bool IsCandidateFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);

        if (fileName.Equals(DirectoryBuildPropsFileName, StringComparison.OrdinalIgnoreCase)
            || fileName.Equals(DirectoryBuildTargetsFileName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return s_projectExtensions.Contains(Path.GetExtension(filePath), StringComparer.OrdinalIgnoreCase);
    }

    private static string GetRelativePath(string rootPath, string filePath)
    {
        return Path.GetRelativePath(rootPath, filePath).Replace('\\', '/');
    }

    private void ReadLanguageVersions(string rootPath, string filePath, LanguageManifest manifest)
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

        var relativePath = GetRelativePath(rootPath, filePath);
        var lineStartOffsets = XmlFileText.GetLineStartOffsets(text);
        var languageVersionFound = false;

        foreach (var element in root.Descendants()
                     .Where(element => element.Name.LocalName == LangVersionElementName))
        {
            var value = element.Value.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                logger.Debug(
                    "Skipping language version in {File} because it has no value",
                    relativePath);
                continue;
            }

            if (!LanguageVersion.TryParse(value, out var parsedVersion))
            {
                logger.Debug(
                    "Skipping language version '{Version}' in {File} because it is not a numeric value",
                    value,
                    relativePath);
                continue;
            }

            var (start, length) = XmlFileText.GetElementTextSpan(text, lineStartOffsets, element);
            if (start < 0)
            {
                var warning =
                    $"Skipping language version '{value}' in '{relativePath}' because its value could not be located.";
                logger.Warning("{Warning}", warning);
                manifest.AddWarning(warning);
                continue;
            }

            manifest.Add(new LanguageEntry(relativePath, filePath, value, parsedVersion, start, length));
            languageVersionFound = true;
        }

        if (languageVersionFound)
        {
            manifest.RegisterFileText(filePath, text);
        }
    }

    private string ApplyChanges(LanguageManifest manifest, string filePath, string text)
    {
        var edits = new List<(int Start, int Length, string NewVersion)>();

        foreach (var entry in manifest.Entries)
        {
            if (!string.Equals(entry.FilePath, filePath, StringComparison.Ordinal)
                || string.Equals(entry.Version, entry.OriginalVersion, StringComparison.Ordinal))
            {
                continue;
            }

            if (entry.VersionStart < 0
                || entry.VersionLength <= 0
                || entry.VersionStart + entry.VersionLength > text.Length)
            {
                var warning =
                    $"Skipping language version '{entry.OriginalVersion}' in '{entry.Id}' because its recorded span is invalid.";
                logger.Warning("{Warning}", warning);
                manifest.AddWarning(warning);
                continue;
            }

            edits.Add((entry.VersionStart, entry.VersionLength, entry.Version));
        }

        // Apply from the end of the file backwards so earlier offsets remain valid.
        foreach (var edit in edits.OrderByDescending(edit => edit.Start))
        {
            text = text.Remove(edit.Start, edit.Length).Insert(edit.Start, edit.NewVersion);
        }

        return text;
    }
}
