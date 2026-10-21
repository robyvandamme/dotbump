// Copyright © Roby Van Damme.

using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using DotBump.Commands.BumpFrameworks.DataModel;
using DotBump.Commands.BumpFrameworks.Interfaces;
using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpFrameworks;

internal sealed class FrameworkFileService(ILogger logger) : IFrameworkFileService
{
    private const string DirectoryBuildPropsFileName = "Directory.Build.props";
    private const string DirectoryBuildTargetsFileName = "Directory.Build.targets";
    private const string TargetFrameworkElementName = "TargetFramework";
    private const string TargetFrameworksElementName = "TargetFrameworks";

    private static readonly string[] s_projectExtensions = [".csproj", ".fsproj", ".vbproj"];

    private static readonly Regex s_targetFrameworkPattern = new(
        @"^net(?<major>\d+)\.0(?<suffix>(?:-.*)?)$",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    public FrameworkManifest GetFrameworkManifest(
        string repositoryPath,
        IReadOnlyCollection<string>? excludedPaths = null)
    {
        logger.MethodStart(nameof(FrameworkFileService), nameof(GetFrameworkManifest), repositoryPath);

        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryPath);

        var rootPath = Path.GetFullPath(repositoryPath);
        if (!Directory.Exists(rootPath))
        {
            throw new DotBumpException($"Repository directory '{rootPath}' does not exist.");
        }

        var manifest = new FrameworkManifest();
        var scanner = new RepositoryFileScanner(logger);

        foreach (var filePath in scanner.EnumerateCandidateFiles(rootPath, excludedPaths, IsCandidateFile))
        {
            ReadTargetFrameworks(rootPath, filePath, manifest);
        }

        logger.MethodReturn(nameof(FrameworkFileService), nameof(GetFrameworkManifest), manifest);

        return manifest;
    }

    public void SaveFrameworkManifest(FrameworkManifest manifest)
    {
        logger.MethodStart(nameof(FrameworkFileService), nameof(SaveFrameworkManifest));

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
            logger.Debug("Updated target frameworks in {File}", filePath);
        }

        logger.MethodReturn(nameof(FrameworkFileService), nameof(SaveFrameworkManifest));
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

    private void ReadTargetFrameworks(string rootPath, string filePath, FrameworkManifest manifest)
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

        if (root.Descendants().Any(element => element.Name.LocalName == TargetFrameworksElementName))
        {
            var warning = $"Skipping '{relativePath}' because TargetFrameworks is not supported yet.";
            logger.Warning("{Warning}", warning);
            manifest.AddWarning(warning);
            return;
        }

        var lineStartOffsets = XmlFileText.GetLineStartOffsets(text);
        var targetFrameworkFound = false;

        foreach (var element in root.Descendants()
                     .Where(element => element.Name.LocalName == TargetFrameworkElementName))
        {
            var value = element.Value.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                logger.Debug(
                    "Skipping target framework in {File} because it has no value",
                    relativePath);
                continue;
            }

            var match = s_targetFrameworkPattern.Match(value);
            if (!match.Success)
            {
                logger.Debug(
                    "Skipping target framework '{Framework}' in {File} because it is not a supported .NET target framework",
                    value,
                    relativePath);
                continue;
            }

            var (start, length) = XmlFileText.GetElementTextSpan(text, lineStartOffsets, element);
            if (start < 0)
            {
                var warning =
                    $"Skipping target framework '{value}' in '{relativePath}' because its value could not be located.";
                logger.Warning("{Warning}", warning);
                manifest.AddWarning(warning);
                continue;
            }

            manifest.Add(
                new FrameworkEntry(
                    relativePath,
                    filePath,
                    value,
                    match.Groups["suffix"].Value,
                    start,
                    length));
            targetFrameworkFound = true;
        }

        if (targetFrameworkFound)
        {
            manifest.RegisterFileText(filePath, text);
        }
    }

    private string ApplyChanges(FrameworkManifest manifest, string filePath, string text)
    {
        var edits = new List<(int Start, int Length, string NewFramework)>();

        foreach (var framework in manifest.Frameworks)
        {
            if (!string.Equals(framework.FilePath, filePath, StringComparison.Ordinal)
                || string.Equals(framework.Framework, framework.OriginalFramework, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (framework.FrameworkStart < 0
                || framework.FrameworkLength <= 0
                || framework.FrameworkStart + framework.FrameworkLength > text.Length)
            {
                var warning =
                    $"Skipping target framework '{framework.OriginalFramework}' in '{framework.Id}' because its recorded span is invalid.";
                logger.Warning("{Warning}", warning);
                manifest.AddWarning(warning);
                continue;
            }

            edits.Add((framework.FrameworkStart, framework.FrameworkLength, framework.Framework));
        }

        // Apply from the end of the file backwards so earlier offsets remain valid.
        foreach (var edit in edits.OrderByDescending(edit => edit.Start))
        {
            text = text.Remove(edit.Start, edit.Length).Insert(edit.Start, edit.NewFramework);
        }

        return text;
    }
}
