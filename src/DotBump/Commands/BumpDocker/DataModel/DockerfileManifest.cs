// Copyright © Roby Van Damme.

namespace DotBump.Commands.BumpDocker.DataModel;

/// <summary>
/// Represents all supported .NET base image tags discovered in a repository.
/// </summary>
internal sealed class DockerfileManifest
{
    private readonly List<DockerImageEntry> _entries = [];
    private readonly List<string> _warnings = [];
    private readonly Dictionary<string, string> _fileTexts = new(StringComparer.Ordinal);
    private readonly HashSet<string> _changedFiles = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets all supported image occurrences.
    /// </summary>
    public IReadOnlyList<DockerImageEntry> Entries => _entries;

    /// <summary>
    /// Gets the warnings raised while reading the repository.
    /// </summary>
    public IReadOnlyList<string> Warnings => _warnings;

    /// <summary>
    /// Gets a value indicating whether any image tag has been changed.
    /// </summary>
    public bool HasChanges => _entries.Any(entry =>
        !string.Equals(entry.Tag, entry.OriginalTag, StringComparison.Ordinal));

    internal IReadOnlyCollection<string> ChangedFiles => _changedFiles;

    /// <summary>
    /// Sets the image tag for the specified entry and records its file as changed.
    /// </summary>
    /// <param name="entry">The image entry to update.</param>
    /// <param name="newTag">The new image tag.</param>
    public void SetTag(DockerImageEntry entry, string newTag)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentException.ThrowIfNullOrWhiteSpace(newTag);

        entry.Tag = newTag;

        if (!string.Equals(entry.Tag, entry.OriginalTag, StringComparison.Ordinal))
        {
            _changedFiles.Add(entry.FilePath);
        }
    }

    internal void Add(DockerImageEntry entry)
    {
        _entries.Add(entry);
    }

    internal void AddWarning(string warning)
    {
        _warnings.Add(warning);
    }

    internal void RegisterFileText(string filePath, string text)
    {
        _fileTexts[filePath] = text;
    }

    internal string? GetFileText(string filePath)
    {
        return _fileTexts.GetValueOrDefault(filePath);
    }
}
