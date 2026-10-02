// Copyright © Roby Van Damme.

namespace DotBump.Commands.BumpPackages.DataModel;

/// <summary>
/// Identifies the kind of file a package version was discovered in.
/// </summary>
internal enum PackageSourceKind
{
    /// <summary>A project file (.csproj, .fsproj or .vbproj).</summary>
    Project,

    /// <summary>A central package management file (Directory.Packages.props).</summary>
    CentralPackageManagement,

    /// <summary>A shared MSBuild file (Directory.Build.props or Directory.Build.targets).</summary>
    SharedMsBuild,
}
