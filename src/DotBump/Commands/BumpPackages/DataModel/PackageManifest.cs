// Copyright © Roby Van Damme.

using System.Xml.Linq;

namespace DotBump.Commands.BumpPackages.DataModel;

/// <summary>
/// Represents all package versions discovered in a repository.
/// </summary>
internal sealed class PackageManifest
{
    private readonly List<PackageVersionEntry> _packages = [];
    private readonly List<string> _warnings = [];
    private readonly Dictionary<string, XDocument> _documents = new(StringComparer.Ordinal);
    private readonly HashSet<string> _changedFiles = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets all package version occurrences.
    /// </summary>
    public IReadOnlyList<PackageVersionEntry> Packages => _packages;

    /// <summary>
    /// Gets the warnings raised while reading the repository.
    /// </summary>
    public IReadOnlyList<string> Warnings => _warnings;

    /// <summary>
    /// Gets a value indicating whether any version has been changed.
    /// </summary>
    public bool HasChanges => _packages.Any(package =>
        !string.Equals(package.Version, package.OriginalVersion, StringComparison.OrdinalIgnoreCase));

    internal IReadOnlyCollection<string> ChangedFiles => _changedFiles;

    /// <summary>
    /// Gets the distinct package identifiers.
    /// </summary>
    /// <returns>The distinct package identifiers in discovery order.</returns>
    public IReadOnlyList<string> GetPackageIds()
    {
        return _packages
            .Select(package => package.PackageId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Sets the version of every occurrence of the specified package.
    /// </summary>
    /// <param name="packageId">The package identifier.</param>
    /// <param name="newVersion">The new version.</param>
    public void SetVersion(string packageId, string newVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(newVersion);

        foreach (var package in _packages.Where(package =>
            string.Equals(package.PackageId, packageId, StringComparison.OrdinalIgnoreCase)))
        {
            package.Version = newVersion;

            if (package.VersionAttribute != null)
            {
                package.VersionAttribute.Value = newVersion;
            }
            else if (package.VersionElement != null)
            {
                package.VersionElement.Value = newVersion;
            }

            _changedFiles.Add(package.FilePath);
        }
    }

    internal void Add(PackageVersionEntry package)
    {
        _packages.Add(package);
    }

    internal void AddWarning(string warning)
    {
        _warnings.Add(warning);
    }

    internal void RegisterDocument(string filePath, XDocument document)
    {
        _documents[filePath] = document;
    }

    internal XDocument? GetDocument(string filePath)
    {
        return _documents.GetValueOrDefault(filePath);
    }
}
