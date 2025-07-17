// Copyright © 2025 Roby Van Damme.

namespace DotBump.Commands.BumpSdk;

public enum BumpType
{
    /// <summary>
    /// Bumps the current SDK version to the latest minor or patch version.
    /// </summary>
    Minor,

    /// <summary>
    /// Bumps the current SDK version to the latest patch version.
    /// </summary>
    Patch,

    /// <summary>
    /// Bumps the SDK version to the latest LTS version.
    /// </summary>
    Lts,

    /// <summary>
    /// Bumps the SDK version to the latest Stable version.
    /// </summary>
    Stable,

    /// <summary>
    /// Bumps the SDK version to the latest Preview version.
    /// </summary>
    Preview,
}
