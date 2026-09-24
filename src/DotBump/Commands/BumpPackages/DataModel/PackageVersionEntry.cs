// Copyright © Roby Van Damme.

using System.Xml.Linq;
using DotBump.Common;

namespace DotBump.Commands.BumpPackages.DataModel;

/// <summary>
/// Represents a single package version occurrence found in a file.
/// </summary>
internal sealed class PackageVersionEntry
{
    /// <summary>
    /// Gets the package identifier.
    /// </summary>
    public required string PackageId { get; init; }

    /// <summary>
    /// Gets the version as it was found in the file.
    /// </summary>
    public required string OriginalVersion { get; init; }

    /// <summary>
    /// Gets or sets the current version, which is updated when a bump is applied.
    /// </summary>
    public required string Version { get; internal set; }

    /// <summary>
    /// Gets the path of the file this entry was found in.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets the kind of the file this entry was found in.
    /// </summary>
    public required PackageSourceKind SourceKind { get; init; }

    /// <summary>
    /// Gets the name of the element the version was found on.
    /// </summary>
    public required string ElementName { get; init; }

    /// <summary>
    /// Gets the package version as a semantic version.
    /// </summary>
    public SemanticVersion SemanticVersion => new(Version);

    internal required XElement Element { get; init; }

    internal XAttribute? VersionAttribute { get; init; }

    internal XElement? VersionElement { get; init; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{PackageId} {OriginalVersion} > {Version} ({FilePath})";
    }
}
