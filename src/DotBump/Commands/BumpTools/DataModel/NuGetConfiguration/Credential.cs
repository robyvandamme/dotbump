// Copyright © 2025 Roby Van Damme.

namespace DotBump.Commands.BumpTools.DataModel.NuGetConfiguration;

internal record Credential
{
    public string Key { get; set; }

    public string Value { get; set; }
}
