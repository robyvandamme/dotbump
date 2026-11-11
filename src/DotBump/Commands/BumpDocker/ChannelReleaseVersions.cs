// Copyright © Roby Van Damme.

using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Common;

namespace DotBump.Commands.BumpDocker;

/// <summary>
/// Resolves the release component version that a supported .NET image is tagged with.
/// </summary>
internal static class ChannelReleaseVersions
{
    /// <summary>
    /// Gets the version of the specified component for a channel release, or <see langword="null"/>
    /// when the release does not publish a valid version for it.
    /// </summary>
    /// <param name="release">The channel release.</param>
    /// <param name="versionSource">The component to read.</param>
    /// <returns>The parsed component version, or <see langword="null"/>.</returns>
    internal static SemanticVersion? GetVersion(
        ChannelRelease release,
        DotnetImageVersionSource versionSource)
    {
        ArgumentNullException.ThrowIfNull(release);

        return versionSource switch
        {
            DotnetImageVersionSource.Sdk => GetSdkVersion(release),
            DotnetImageVersionSource.Runtime => GetValidVersion(release.Runtime?.SemanticVersion)
                ?? GetValidVersion(Parse(release.ReleaseVersion)),
            DotnetImageVersionSource.AspNetCore => GetValidVersion(release.AspNetCoreRuntime?.SemanticVersion),
            _ => throw new ArgumentOutOfRangeException(nameof(versionSource), versionSource, null),
        };
    }

    private static SemanticVersion? GetSdkVersion(ChannelRelease release)
    {
        var sdkVersion = GetValidVersion(release.Sdk?.SemanticVersion);
        if (sdkVersion != null)
        {
            return sdkVersion;
        }

        return release.Sdks?
            .Select(sdk => GetValidVersion(sdk.SemanticVersion))
            .OfType<SemanticVersion>()
            .OrderByDescending(version => version)
            .FirstOrDefault();
    }

    private static SemanticVersion? Parse(string? version)
    {
        return string.IsNullOrWhiteSpace(version) ? null : new SemanticVersion(version);
    }

    private static SemanticVersion? GetValidVersion(SemanticVersion? version)
    {
        return version is { IsValid: true } ? version : null;
    }
}
