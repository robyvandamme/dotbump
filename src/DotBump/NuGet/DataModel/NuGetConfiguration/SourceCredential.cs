// Copyright © Roby Van Damme.

namespace DotBump.NuGet.DataModel.NuGetConfiguration;

internal record SourceCredential
{
    public required string SourceName { get; set; }

    public List<Credential> Credentials { get; } = new();
}
