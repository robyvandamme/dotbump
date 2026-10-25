// Copyright © Roby Van Damme.

namespace DotBump.Commands.BumpLanguage.DataModel;

/// <summary>
/// Represents all language version values discovered in a repository.
/// </summary>
internal sealed class LanguageManifest
{
    private readonly List<LanguageEntry> _entries = [];
    private readonly List<string> _warnings = [];
    private readonly Dictionary<string, string> _fileTexts = new(StringComparer.Ordinal);
    private readonly HashSet<string> _changedFiles = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets all language version occurrences.
    /// </summary>
    public IReadOnlyList<LanguageEntry> Entries => _entries;

    /// <summary>
    /// Gets the warnings raised while reading the repository.
    /// </summary>
    public IReadOnlyList<string> Warnings => _warnings;

    /// <summary>
    /// Gets a value indicating whether any language version has been changed.
    /// </summary>
    public bool HasChanges => _entries.Any(entry =>
        !string.Equals(entry.Version, entry.OriginalVersion, StringComparison.Ordinal));

    internal IReadOnlyCollection<string> ChangedFiles => _changedFiles;

    /// <summary>
    /// Sets the language version for the specified entry and records its file as changed.
    /// </summary>
    /// <param name="entry">The language version entry to update.</param>
    /// <param name="newVersion">The new language version.</param>
    public void SetVersion(LanguageEntry entry, string newVersion)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentException.ThrowIfNullOrWhiteSpace(newVersion);

        entry.Version = newVersion;

        if (!string.Equals(entry.Version, entry.OriginalVersion, StringComparison.Ordinal))
        {
            _changedFiles.Add(entry.FilePath);
        }
    }

    internal void Add(LanguageEntry entry)
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
