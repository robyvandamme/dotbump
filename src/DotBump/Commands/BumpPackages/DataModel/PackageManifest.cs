// Copyright © Roby Van Damme.

namespace DotBump.Commands.BumpPackages.DataModel;

/// <summary>
/// Represents all package versions discovered in a repository.
/// </summary>
internal sealed class PackageManifest
{
    private static readonly VariantKeyComparer s_variantKeyComparer = new();

    private readonly List<PackageVersionEntry> _packages = [];
    private readonly List<string> _warnings = [];
    private readonly Dictionary<string, string> _fileTexts = new(StringComparer.Ordinal);
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
    /// Gets the package variants: occurrences of the same package id are grouped together only when
    /// they share the same effective MSBuild condition.
    /// </summary>
    /// <returns>The package variants in discovery order.</returns>
    public IReadOnlyList<PackageVariant> GetVariants()
    {
        var variants = new List<(string PackageId, string? Condition, List<PackageVersionEntry> Occurrences)>();
        var indexes = new Dictionary<(string PackageId, string? Condition), int>(s_variantKeyComparer);

        foreach (var package in _packages)
        {
            var condition = PackageVariant.NormalizeCondition(package.Condition);
            var key = (package.PackageId, condition);

            if (!indexes.TryGetValue(key, out var index))
            {
                index = variants.Count;
                variants.Add((package.PackageId, condition, []));
                indexes[key] = index;
            }

            variants[index].Occurrences.Add(package);
        }

        return variants
            .Select(variant => new PackageVariant(variant.PackageId, variant.Condition, variant.Occurrences))
            .ToList();
    }

    /// <summary>
    /// Sets the version of every occurrence in the specified package variant.
    /// </summary>
    /// <param name="variant">The package variant to update.</param>
    /// <param name="newVersion">The new version.</param>
    public void SetVersion(PackageVariant variant, string newVersion)
    {
        ArgumentNullException.ThrowIfNull(variant);
        ArgumentException.ThrowIfNullOrWhiteSpace(newVersion);

        foreach (var package in variant.Occurrences)
        {
            package.Version = newVersion;
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

    internal void RegisterFileText(string filePath, string text)
    {
        _fileTexts[filePath] = text;
    }

    internal string? GetFileText(string filePath)
    {
        return _fileTexts.GetValueOrDefault(filePath);
    }

    private sealed class VariantKeyComparer : IEqualityComparer<(string PackageId, string? Condition)>
    {
        public bool Equals(
            (string PackageId, string? Condition) x,
            (string PackageId, string? Condition) y)
        {
            return string.Equals(x.PackageId, y.PackageId, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(x.Condition, y.Condition, StringComparison.Ordinal);
        }

        public int GetHashCode((string PackageId, string? Condition) obj)
        {
            return HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.PackageId),
                obj.Condition == null ? 0 : StringComparer.Ordinal.GetHashCode(obj.Condition));
        }
    }
}
