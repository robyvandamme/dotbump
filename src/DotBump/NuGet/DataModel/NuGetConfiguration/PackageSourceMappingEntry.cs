// Copyright © Roby Van Damme.

namespace DotBump.NuGet.DataModel.NuGetConfiguration;

/// <summary>
/// Represents a package source mapping entry from the <c>packageSourceMapping</c> section of a
/// nuget.config file: a package source key and the package patterns mapped to it.
/// </summary>
internal record PackageSourceMappingEntry
{
    public required string SourceKey { get; init; }

    public List<string> Patterns { get; init; } = new();
}
