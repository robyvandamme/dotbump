// Copyright © Roby Van Damme.

namespace DotBump.Commands.BumpDocker;

/// <summary>
/// Identifies which release component a supported .NET image is tagged with.
/// </summary>
internal enum DotnetImageVersionSource
{
    Sdk,
    Runtime,
    AspNetCore,
}
