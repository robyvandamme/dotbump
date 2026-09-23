// Copyright © Roby Van Damme.

namespace DotBump.NuGet.DataModel.NuGetConfiguration;

internal record PackageSource
{
    public required string Key { get; set; }

    public required string Value { get; set; }

    public required string ProtocolVersion { get; set; }
}
