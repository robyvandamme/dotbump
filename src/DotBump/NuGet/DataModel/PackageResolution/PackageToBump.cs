// Copyright © Roby Van Damme.

using DotBump.Common;

namespace DotBump.NuGet.DataModel.PackageResolution;

/// <summary>
/// Represents a package and its current version for which a newer version should be resolved.
/// </summary>
internal record PackageToBump(string PackageId, SemanticVersion CurrentVersion);
