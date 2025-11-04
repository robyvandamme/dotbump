// Copyright © 2025 Roby Van Damme.

namespace DotBump.Commands.BumpTools.DataModel.NuGetConfiguration;

internal record SourceCredential
{
    public required string SourceName { get; set; }

    public List<Credential> Credentials { get; } = new();
}
