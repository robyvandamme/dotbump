// Copyright © Roby Van Damme.

using System.Diagnostics;
using System.Reflection;

namespace DotBump.Common;

internal record VersionInfo
{
    public VersionInfo(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var version = assembly.GetName().Version;
        if (version != null)
        {
            AssemblyVersion = version.ToString();
        }

        AssemblyFileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion;
        ProductVersion = FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion;

        Version = ParseProductVersion(ProductVersion);
    }

    internal static string? ParseProductVersion(string? productVersion)
    {
        if (string.IsNullOrEmpty(productVersion))
        {
            return null;
        }

        var plusSign = productVersion.IndexOf('+', StringComparison.OrdinalIgnoreCase);
        return plusSign >= 0 ? productVersion[..plusSign] : productVersion;
    }

    public string? AssemblyVersion { get; }

    public string? AssemblyFileVersionInfo { get; }

    public string? ProductVersion { get; }

    public string? Version { get; }
}
