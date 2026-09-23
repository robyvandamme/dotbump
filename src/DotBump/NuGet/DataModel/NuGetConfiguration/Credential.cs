// Copyright © Roby Van Damme.

using Destructurama.Attributed;

namespace DotBump.NuGet.DataModel.NuGetConfiguration;

internal record Credential
{
    public required string Key { get; set; }

    [LogMasked]
    public required string Value { get; set; }
}
