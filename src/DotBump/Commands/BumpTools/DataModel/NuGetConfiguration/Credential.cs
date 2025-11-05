// Copyright © 2025 Roby Van Damme.

namespace DotBump.Commands.BumpTools.DataModel.NuGetConfiguration;

internal record Credential
{
    public required string Key { get; set; }

    public required string Value { get; set; }
}
