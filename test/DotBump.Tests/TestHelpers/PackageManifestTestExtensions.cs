// Copyright © Roby Van Damme.

using DotBump.Commands.BumpPackages.DataModel;

namespace DotBump.Tests.TestHelpers;

internal static class PackageManifestTestExtensions
{
    /// <summary>
    /// Sets the version of the only variant for the package id. Tests that cover conditional variants
    /// use <see cref="PackageManifest.SetVersion(PackageVariant, string)"/> directly.
    /// </summary>
    public static void SetVersion(this PackageManifest manifest, string packageId, string newVersion)
    {
        var variant = manifest.GetVariants().Single(candidate =>
            string.Equals(candidate.PackageId, packageId, StringComparison.OrdinalIgnoreCase));

        manifest.SetVersion(variant, newVersion);
    }
}
