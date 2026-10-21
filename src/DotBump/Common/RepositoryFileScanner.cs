// Copyright © Roby Van Damme.

using Serilog;

namespace DotBump.Common;

/// <summary>
/// Enumerates candidate files in a repository while applying the shared directory exclusion rules.
/// </summary>
internal sealed class RepositoryFileScanner(ILogger logger)
{
    private static readonly string[] s_excludedDirectories = ["bin", "obj", ".git", ".vs", "node_modules"];

    public IEnumerable<string> EnumerateCandidateFiles(
        string rootPath,
        IReadOnlyCollection<string>? excludedPaths,
        Func<string, bool> isCandidateFile)
    {
        var normalizedExcludedPaths = NormalizeExcludedPaths(excludedPaths);

        if (IsExcludedDirectory(rootPath, normalizedExcludedPaths))
        {
            yield break;
        }

        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(rootPath);

        while (pendingDirectories.Count > 0)
        {
            var directory = pendingDirectories.Pop();

            foreach (var subDirectory in Directory.EnumerateDirectories(directory))
            {
                if (s_excludedDirectories.Contains(Path.GetFileName(subDirectory), StringComparer.OrdinalIgnoreCase)
                    || IsExcludedDirectory(subDirectory, normalizedExcludedPaths)
                    || IsReparsePoint(subDirectory))
                {
                    continue;
                }

                pendingDirectories.Push(subDirectory);
            }

            foreach (var filePath in Directory.EnumerateFiles(directory)
                         .Where(isCandidateFile)
                         .Where(filePath => !IsReparsePoint(filePath)))
            {
                yield return filePath;
            }
        }
    }

    internal static bool IsExcludedDirectory(string directory, HashSet<string> excludedPaths)
    {
        var normalizedDirectory = Path.TrimEndingDirectorySeparator(directory);

        return excludedPaths.Any(excludedPath =>
            string.Equals(normalizedDirectory, excludedPath, StringComparison.Ordinal)
            || normalizedDirectory.StartsWith(
                Path.EndsInDirectorySeparator(excludedPath)
                    ? excludedPath
                    : excludedPath + Path.DirectorySeparatorChar,
                StringComparison.Ordinal));
    }

    private static HashSet<string> NormalizeExcludedPaths(IReadOnlyCollection<string>? excludedPaths)
    {
        if (excludedPaths == null)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        return excludedPaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path)))
            .ToHashSet(StringComparer.Ordinal);
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
}
