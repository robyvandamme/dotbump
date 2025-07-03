// Copyright © 2025 Roby Van Damme.

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

        if (ProductVersion != null && ProductVersion.Length > 0)
        {
            var plusSign = ProductVersion.IndexOf('+', StringComparison.OrdinalIgnoreCase);
            Version = ProductVersion.Remove(plusSign);
        }
    }

    public string? AssemblyVersion { get; }

    public string? AssemblyFileVersionInfo { get; }

    public string? ProductVersion { get; }

    public string? Version { get; }
}
