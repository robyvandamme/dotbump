// Copyright © 2025 Roby Van Damme.

namespace DotBump.Commands.BumpTools.DataModel.NuGetConfiguration;

internal class SourceCredential
{
    public string SourceName { get; set; }

    public List<Credential> Credentials { get; set; } = new List<Credential>();
}
