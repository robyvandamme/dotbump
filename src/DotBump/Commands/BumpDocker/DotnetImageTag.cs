// Copyright © Roby Van Damme.

using System.Text.RegularExpressions;
using DotBump.Common;

namespace DotBump.Commands.BumpDocker;

/// <summary>
/// Parses and maps .NET Docker image tags such as <c>11.0.100-rc.3</c> and <c>10.0.15</c>.
/// The .NET release index reports full runtime versions (for example
/// <c>11.0.0-preview.1.25120.3</c>), while the corresponding image tag omits the build metadata.
/// </summary>
internal static class DotnetImageTag
{
    private const string PreReleaseLabels = "alpha|beta|rc|preview";

    private static readonly Regex s_imageTagPattern = new(
        $@"^(?<version>\d+\.\d+\.\d+)(?:-(?<label>{PreReleaseLabels})(?:\.(?<iteration>\d+))?)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex s_releaseVersionPattern = new(
        $@"^(?<version>\d+\.\d+\.\d+)(?:-(?<label>{PreReleaseLabels})(?:\.(?<iteration>\d+))?(?:\.[0-9A-Za-z.-]+)?)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Tries to parse a Docker image tag as a fully versioned .NET image tag. Floating tags such as
    /// <c>11.0</c> and variant tags such as <c>11.0.100-rc.3-noble</c> are not supported.
    /// </summary>
    /// <param name="tag">The image tag.</param>
    /// <param name="version">The parsed semantic version, or an invalid version when parsing fails.</param>
    /// <returns><see langword="true"/> when the tag is a supported .NET image tag.</returns>
    internal static bool TryParse(string tag, out SemanticVersion version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        version = new SemanticVersion(tag);

        // The image tag pattern is stricter than SemanticVersion (it rejects build metadata and
        // four-part versions), so it is the only gate required here.
        return s_imageTagPattern.IsMatch(tag);
    }

    /// <summary>
    /// Maps a full version from the .NET release history to the matching Docker image tag by
    /// dropping the build metadata from pre-release versions. For example
    /// <c>11.0.100-rc.3.26522.148</c> maps to <c>11.0.100-rc.3</c> and <c>10.0.403</c> stays
    /// <c>10.0.403</c>.
    /// </summary>
    /// <param name="releaseVersion">The full version from the release history.</param>
    /// <returns>The image tag, or <see langword="null"/> when no supported image tag can be derived.</returns>
    internal static string? TryMapFromVersion(string releaseVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(releaseVersion);

        var match = s_releaseVersionPattern.Match(releaseVersion);
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
