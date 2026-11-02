// Copyright © Roby Van Damme.

using System.Text.RegularExpressions;
using DotBump.Commands.BumpDocker.DataModel;
using DotBump.Commands.BumpDocker.Interfaces;
using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpDocker;

internal sealed class DockerfileService(ILogger logger) : IDockerfileService
{
    internal const string SupportedImage = "mcr.microsoft.com/dotnet/sdk";

    private static readonly Regex s_fromPattern = new(
        @"^\s*FROM\s+(?<args>.*?)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    public DockerfileManifest GetDockerfileManifest(
        string repositoryPath,
        IReadOnlyCollection<string>? excludedPaths = null)
    {
        logger.MethodStart(nameof(DockerfileService), nameof(GetDockerfileManifest), repositoryPath);

        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryPath);

        var rootPath = Path.GetFullPath(repositoryPath);
        if (!Directory.Exists(rootPath))
        {
            throw new DotBumpException($"Repository directory '{rootPath}' does not exist.");
        }

        var manifest = new DockerfileManifest();
        var scanner = new RepositoryFileScanner(logger);

        foreach (var filePath in scanner.EnumerateCandidateFiles(rootPath, excludedPaths, IsCandidateFile))
        {
            ReadFromInstructions(rootPath, filePath, manifest);
        }

        logger.MethodReturn(nameof(DockerfileService), nameof(GetDockerfileManifest), manifest);

        return manifest;
    }

    public void SaveDockerfileManifest(DockerfileManifest manifest)
    {
        logger.MethodStart(nameof(DockerfileService), nameof(SaveDockerfileManifest));

        ArgumentNullException.ThrowIfNull(manifest);

        foreach (var filePath in manifest.ChangedFiles)
        {
            var text = manifest.GetFileText(filePath);
            if (text == null)
            {
                var relativePath = manifest.Entries
                    .FirstOrDefault(entry => string.Equals(entry.FilePath, filePath, StringComparison.Ordinal))
                    ?.RelativePath ?? filePath;

                AddWarning(
                    manifest,
                    $"Could not update SDK image tags in '{relativePath}' because the file content was not available.");
                continue;
            }

            var updatedText = ApplyChanges(manifest, filePath, text);
            FileText.WriteText(filePath, updatedText);
            logger.Debug("Updated SDK image tags in {File}", filePath);
        }

        logger.MethodReturn(nameof(DockerfileService), nameof(SaveDockerfileManifest));
    }

    private static bool IsCandidateFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);

        if (fileName.Equals("Dockerfile", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("Containerfile", StringComparison.OrdinalIgnoreCase)
            || fileName.StartsWith("Dockerfile.", StringComparison.OrdinalIgnoreCase)
            || fileName.StartsWith("Containerfile.", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // OrdinalIgnoreCase also covers the .Dockerfile extension.
        return Path.GetExtension(filePath).Equals(".dockerfile", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetRelativePath(string rootPath, string filePath)
    {
        return Path.GetRelativePath(rootPath, filePath).Replace('\\', '/');
    }

    private static List<(string Value, int Start)> Tokenize(string value, int start)
    {
        var tokens = new List<(string Value, int Start)>();
        var index = 0;

        while (index < value.Length)
        {
            while (index < value.Length && char.IsWhiteSpace(value[index]))
            {
                index++;
            }

            if (index >= value.Length)
            {
                break;
            }

            var tokenStart = index;

            while (index < value.Length && !char.IsWhiteSpace(value[index]))
            {
                index++;
            }

            tokens.Add((value[tokenStart..index], start + tokenStart));
        }

        return tokens;
    }

    private static (string Line, int Start) GetLine(string text, int[] lineStartOffsets, int lineIndex)
    {
        var start = lineStartOffsets[lineIndex];
        var end = lineIndex + 1 < lineStartOffsets.Length ? lineStartOffsets[lineIndex + 1] : text.Length;

        return (text[start..end].TrimEnd('\r', '\n'), start);
    }

    private static bool IsSkippable(string line)
    {
        var trimmedStart = line.TrimStart();

        return trimmedStart.Length == 0 || trimmedStart[0] == '#';
    }

    private static bool EndsWithContinuation(string line)
    {
        return line.TrimEnd().EndsWith('\\');
    }

    private static bool StartsContinuation(string line)
    {
        return !IsSkippable(line) && EndsWithContinuation(line);
    }

    /// <summary>
    /// Updates the continuation state for the current line and returns whether the line belongs to a
    /// continued instruction and must be skipped.
    /// </summary>
    private static bool IsInsideContinuation(string line, ref bool isContinuation)
    {
        if (!isContinuation)
        {
            return false;
        }

        // Docker removes comment and empty lines inside a continued instruction before processing
        // the continuation, so they must not change the continuation state here.
        if (!IsSkippable(line))
        {
            isContinuation = EndsWithContinuation(line);
        }

        return true;
    }

    private void ReadFromInstructions(string rootPath, string filePath, DockerfileManifest manifest)
    {
        var text = FileText.ReadText(filePath);
        var relativePath = GetRelativePath(rootPath, filePath);
        var lineStartOffsets = FileText.GetLineStartOffsets(text);
        var entriesBefore = manifest.Entries.Count;
        var isContinuation = false;

        for (var lineIndex = 0; lineIndex < lineStartOffsets.Length; lineIndex++)
        {
            var (line, lineStart) = GetLine(text, lineStartOffsets, lineIndex);

            if (IsInsideContinuation(line, ref isContinuation))
            {
                continue;
            }

            var match = s_fromPattern.Match(line);

            if (StartsContinuation(line))
            {
                WarnIfMultiLineFrom(match, manifest, relativePath, lineIndex + 1);
                isContinuation = true;
                continue;
            }

            if (!match.Success)
            {
                continue;
            }

            var args = match.Groups["args"];
            ReadFromInstruction(
                args.Value,
                lineStart + args.Index,
                lineIndex + 1,
                relativePath,
                filePath,
                manifest);
        }

        if (manifest.Entries.Count > entriesBefore)
        {
            manifest.RegisterFileText(filePath, text);
        }
    }

    private void WarnIfMultiLineFrom(Match match, DockerfileManifest manifest, string relativePath, int lineNumber)
    {
        if (!match.Success)
        {
            return;
        }

        // The instruction spans multiple physical lines; joining it correctly would require tracking
        // logical line spans, so it is skipped rather than risking a partial rewrite.
        AddWarning(
            manifest,
            $"Skipping multi-line FROM instruction in '{relativePath}' (line {lineNumber}) because multi-line FROM instructions are not supported yet.");
    }

    private void ReadFromInstruction(
        string args,
        int argsStart,
        int lineNumber,
        string relativePath,
        string filePath,
        DockerfileManifest manifest)
    {
        var tokens = Tokenize(args, argsStart);

        var tokenIndex = 0;
        while (tokenIndex < tokens.Count
               && tokens[tokenIndex].Value.StartsWith("--", StringComparison.Ordinal))
        {
            tokenIndex++;
        }

        if (tokenIndex >= tokens.Count)
        {
            return;
        }

        var imageToken = tokens[tokenIndex];
        var digestIndex = imageToken.Value.IndexOf('@', StringComparison.Ordinal);
        var nameAndTag = digestIndex >= 0 ? imageToken.Value[..digestIndex] : imageToken.Value;

        // Only the last colon that follows the last slash separates the image name from the tag,
        // so registry ports such as localhost:5000 are not mistaken for a tag.
        var lastSlash = nameAndTag.LastIndexOf('/');
        var tagSeparator = nameAndTag.LastIndexOf(':');
        var hasTag = tagSeparator > lastSlash;
        var image = hasTag ? nameAndTag[..tagSeparator] : nameAndTag;
        var tag = hasTag ? nameAndTag[(tagSeparator + 1)..] : null;

        if (!string.Equals(image, SupportedImage, StringComparison.OrdinalIgnoreCase))
        {
            logger.Debug("Skipping unsupported image {Image} in {File}", image, relativePath);
            return;
        }

        if (digestIndex >= 0)
        {
            AddWarning(
                manifest,
                $"Skipping image '{image}' in '{relativePath}' (line {lineNumber}) because image digests are not supported yet.");
            return;
        }

        if (!hasTag || string.IsNullOrWhiteSpace(tag))
        {
            AddWarning(
                manifest,
                $"Skipping image '{image}' in '{relativePath}' (line {lineNumber}) because it has no tag.");
            return;
        }

        if (tag.Contains('$', StringComparison.Ordinal) || tag.Contains('{', StringComparison.Ordinal))
        {
            AddWarning(
                manifest,
                $"Skipping image '{image}:{tag}' in '{relativePath}' (line {lineNumber}) because build arguments are not supported yet.");
            return;
        }

        if (!SdkImageTag.TryParse(tag, out _))
        {
            AddWarning(
                manifest,
                $"Skipping image '{image}:{tag}' in '{relativePath}' (line {lineNumber}) because only fully versioned .NET SDK tags are supported.");
            return;
        }

        manifest.Add(
            new DockerImageEntry(
                image,
                relativePath,
                filePath,
                lineNumber,
                tag,
                imageToken.Start + tagSeparator + 1,
                tag.Length));
    }

    private void AddWarning(DockerfileManifest manifest, string warning)
    {
        logger.Warning("{Warning}", warning);
        manifest.AddWarning(warning);
    }

    private string ApplyChanges(DockerfileManifest manifest, string filePath, string text)
    {
        var edits = new List<(int Start, int Length, string NewTag)>();

        foreach (var entry in manifest.Entries)
        {
            if (!string.Equals(entry.FilePath, filePath, StringComparison.Ordinal)
                || string.Equals(entry.Tag, entry.OriginalTag, StringComparison.Ordinal))
            {
                continue;
            }

            if (entry.TagStart < 0
                || entry.TagLength <= 0
                || entry.TagStart + entry.TagLength > text.Length)
            {
                AddWarning(
                    manifest,
                    $"Skipping image tag '{entry.OriginalTag}' in '{entry.RelativePath}' (line {entry.LineNumber}) because its recorded span is invalid.");
                continue;
            }

            edits.Add((entry.TagStart, entry.TagLength, entry.Tag));
        }

        // Apply from the end of the file backwards so earlier offsets remain valid.
        foreach (var edit in edits.OrderByDescending(edit => edit.Start))
        {
            text = text.Remove(edit.Start, edit.Length).Insert(edit.Start, edit.NewTag);
        }

        return text;
    }
}
