// Copyright © Roby Van Damme.

namespace DotBump.Commands.BumpFrameworks.DataModel;

/// <summary>
/// Represents all target framework values discovered in a repository.
/// </summary>
internal sealed class FrameworkManifest
{
    private readonly List<FrameworkEntry> _frameworks = [];
    private readonly List<string> _warnings = [];
    private readonly Dictionary<string, string> _fileTexts = new(StringComparer.Ordinal);
    private readonly HashSet<string> _changedFiles = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets all target framework occurrences.
    /// </summary>
    public IReadOnlyList<FrameworkEntry> Frameworks => _frameworks;

    /// <summary>
    /// Gets the warnings raised while reading the repository.
    /// </summary>
    public IReadOnlyList<string> Warnings => _warnings;

    /// <summary>
    /// Gets a value indicating whether any target framework has been changed.
    /// </summary>
    public bool HasChanges => _frameworks.Any(framework =>
        !string.Equals(framework.Framework, framework.OriginalFramework, StringComparison.OrdinalIgnoreCase));

    internal IReadOnlyCollection<string> ChangedFiles => _changedFiles;

    /// <summary>
    /// Sets the target framework for the specified entry and records its file as changed.
    /// </summary>
    /// <param name="entry">The target framework entry to update.</param>
    /// <param name="newFramework">The new target framework.</param>
    public void SetFramework(FrameworkEntry entry, string newFramework)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentException.ThrowIfNullOrWhiteSpace(newFramework);

        entry.Framework = newFramework;

        if (!string.Equals(entry.Framework, entry.OriginalFramework, StringComparison.OrdinalIgnoreCase))
        {
            _changedFiles.Add(entry.FilePath);
        }
    }

    internal void Add(FrameworkEntry framework)
    {
        _frameworks.Add(framework);
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
