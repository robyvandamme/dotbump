// Copyright © 2025 Roby Van Damme.

namespace DotBump.Commands.BumpTools.DataModel.NuGetConfiguration;

// TODO: remove pragma and fix
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
internal record PackageSource
{
    public string Key { get; set; }

    public string Value { get; set; }

    public string ProtocolVersion { get; set; }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
