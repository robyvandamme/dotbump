// Copyright © Roby Van Damme.

namespace DotBump.Commands.BumpPackages.DataModel;

/// <summary>
/// Represents all occurrences of a package id that share the same effective MSBuild condition.
/// Occurrences with different conditions are bumped independently.
/// </summary>
internal sealed class PackageVariant(
    string packageId,
    string? condition,
    IReadOnlyList<PackageVersionEntry> occurrences)
{
    /// <summary>
    /// Gets the package identifier.
    /// </summary>
    public string PackageId { get; } = packageId;

    /// <summary>
    /// Gets the effective MSBuild condition shared by all occurrences, or <see langword="null"/> when
    /// the variant is unconditional.
    /// </summary>
    public string? Condition { get; } = condition;

    /// <summary>
    /// Gets the occurrences that make up this variant.
    /// </summary>
    public IReadOnlyList<PackageVersionEntry> Occurrences { get; } = occurrences;
}
