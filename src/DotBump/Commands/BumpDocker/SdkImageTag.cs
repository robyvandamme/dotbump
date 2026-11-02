// Copyright © Roby Van Damme.

using System.Text.RegularExpressions;
using DotBump.Common;

namespace DotBump.Commands.BumpDocker;

/// <summary>
/// Parses and maps .NET SDK Docker image tags such as <c>11.0.100-rc.3</c>.
/// The .NET release index reports the full SDK version (for example
/// <c>11.0.100-rc.3.26522.148</c>), while the corresponding image tag omits the build metadata.
/// </summary>
internal static class SdkImageTag
{
    private const string PreReleaseLabels = "alpha|beta|rc|preview";

    private static readonly Regex s_imageTagPattern = new(
        $@"^(?<version>\d+\.\d+\.\d+)(?:-(?<label>{PreReleaseLabels})(?:\.(?<iteration>\d+))?)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex s_sdkVersionPattern = new(
        $@"^(?<version>\d+\.\d+\.\d+)(?:-(?<label>{PreReleaseLabels})(?:\.(?<iteration>\d+))?(?:\.[0-9A-Za-z.-]+)?)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Tries to parse a Docker image tag as a fully versioned .NET SDK tag. Floating tags such as
    /// <c>11.0</c> and variant tags such as <c>11.0.100-rc.3-noble</c> are not supported.
    /// </summary>
    /// <param name="tag">The image tag.</param>
    /// <param name="version">The parsed semantic version, or an invalid version when parsing fails.</param>
    /// <returns><see langword="true"/> when the tag is a supported .NET SDK tag.</returns>
    internal static bool TryParse(string tag, out SemanticVersion version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        version = new SemanticVersion(tag);

        return version.IsValid && s_imageTagPattern.IsMatch(tag);
    }

    /// <summary>
    /// Maps a full SDK version from the .NET release index to the matching Docker image tag by
    /// dropping the build metadata from pre-release versions. For example
    /// <c>11.0.100-rc.3.26522.148</c> maps to <c>11.0.100-rc.3</c> and <c>10.0.403</c> stays
    /// <c>10.0.403</c>.
    /// </summary>
    /// <param name="sdkVersion">The full SDK version from the release index.</param>
    /// <returns>The image tag, or <see langword="null"/> when no supported image tag can be derived.</returns>
    internal static string? TryMapFromSdkVersion(string sdkVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sdkVersion);

        var match = s_sdkVersionPattern.Match(sdkVersion);
        if (!match.Success)
        {
            return null;
        }

        var imageTag = match.Groups["version"].Value;

        if (match.Groups["label"].Success)
        {
            imageTag += "-" + match.Groups["label"].Value;

            if (match.Groups["iteration"].Success)
            {
                imageTag += "." + match.Groups["iteration"].Value;
            }
        }

        return TryParse(imageTag, out _) ? imageTag : null;
    }
}
