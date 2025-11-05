// Copyright © 2025 Roby Van Damme.

namespace DotBump.Commands.BumpTools.DataModel.NuGetConfiguration;

internal record PackageSource
{
    public required string Key { get; set; }

    public required string Value { get; set; }

    public required string ProtocolVersion { get; set; }
}
