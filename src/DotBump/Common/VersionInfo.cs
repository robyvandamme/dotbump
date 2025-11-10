// Copyright © 2025 Roby Van Damme.

using System.Diagnostics;
using System.Reflection;

namespace DotBump.Common;

internal record VersionInfo
{
    public VersionInfo(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        InitializeVersionInfo(assembly);
    }

    public string? AssemblyVersion { get; private set; }

    public string? AssemblyFileVersionInfo { get; private set; }

    public string? ProductVersion { get; private set; }

    public string? Version { get; private set; }

    private void InitializeVersionInfo(Assembly assembly)
    {
        var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

        // Set assembly version
        var assemblyVersion = assembly.GetName().Version;
        AssemblyVersion = assemblyVersion?.ToString();

        // Set file version and product version
        AssemblyFileVersionInfo = fileVersionInfo.FileVersion;
        ProductVersion = fileVersionInfo.ProductVersion;

        // Extract version from product version
        if (ProductVersion != null)
        {
            Version = ExtractVersionFromProductVersion(ProductVersion);
        }
    }

    private static string ExtractVersionFromProductVersion(string productVersion)
    {
        if (string.IsNullOrEmpty(productVersion))
        {
            return string.Empty;
        }

        var plusSignIndex = productVersion.IndexOf('+', StringComparison.OrdinalIgnoreCase);
        return plusSignIndex > 0 ? productVersion.Remove(plusSignIndex) : productVersion;
    }
}
