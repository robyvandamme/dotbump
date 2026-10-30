// Copyright © Roby Van Damme.

using DotBump.Common;

namespace DotBump.NuGet.DataModel.PackageResolution;

/// <summary>
/// Represents a package and its current version for which a newer version should be resolved.
/// Two instances are equal when their package ids match case-insensitively and their current
/// versions are semantically equal.
/// </summary>
internal record PackageToBump(string PackageId, SemanticVersion CurrentVersion)
{
    /// <summary>
    /// Determines whether this instance is equal to another package input. Package ids follow
    /// NuGet's case-insensitive semantics; versions use <see cref="SemanticVersion"/> equality.
    /// </summary>
    public virtual bool Equals(PackageToBump? other)
    {
        return other is not null
               && string.Equals(PackageId, other.PackageId, StringComparison.OrdinalIgnoreCase)
               && CurrentVersion.Equals(other.CurrentVersion);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(
            StringComparer.OrdinalIgnoreCase.GetHashCode(PackageId),
            CurrentVersion.GetHashCode());
    }
}
