// Copyright © Roby Van Damme.

namespace DotBump.NuGet.DataModel.NuGetConfiguration;

internal record NuGetConfig
{
    public List<PackageSource> PackageSources { get; set; } = new();

    public List<PackageSourceMappingEntry> PackageSourceMappings { get; set; } = new();

    public Dictionary<string, SourceCredential> Credentials { get; set; } = new();
}
