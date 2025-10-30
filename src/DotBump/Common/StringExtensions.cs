// Copyright © 2025 Roby Van Damme.

using System.Text.RegularExpressions;

namespace DotBump.Common;

internal static class StringExtensions
{
    private static readonly Regex s_versionPattern = new(
        @"^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:-(?<prerelease>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$",
        RegexOptions.Compiled);

    /// <summary>
    /// Checks if the string matches the Semantic Version regex pattern.
    /// </summary>
    /// <param name="version">The version string.</param>
    /// <returns><see cref="Match"/>The match result.</returns>
    internal static Match MatchesSemanticVersionPattern(this string version)
    {
        var match = s_versionPattern.Match(version);
        return match;
    }
}
