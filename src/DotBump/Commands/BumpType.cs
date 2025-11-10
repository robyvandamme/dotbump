// Copyright © 2025 Roby Van Damme.

namespace DotBump.Commands;

internal enum BumpType
{
    /// <summary>
    /// Bumps the current version to the latest minor or patch version.
    /// </summary>
    Minor,

    /// <summary>
    /// Bumps the current version to the latest patch version.
    /// </summary>
    Patch,
}
