// Copyright © 2025 Roby Van Damme.

namespace DotBump.Commands.BumpTools.DataModel.NuGetConfiguration;

internal record NuGetConfig
{
    public List<PackageSource> PackageSources { get; set; } = new();

    public Dictionary<string, SourceCredential> Credentials { get; set; } = new();
}
