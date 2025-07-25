// Copyright © 2025 Roby Van Damme.

using System.Globalization;
using System.Text.RegularExpressions;

namespace DotBump.Common;

/// <summary>
/// Represents a semantic version according to Semantic Versioning 2.0.0 (https://semver.org/).
/// Supports pre-release versions like alpha, beta, rc, preview, etc.
/// </summary>
internal record SemanticVersion : IComparable<SemanticVersion>
{
    private static readonly Regex s_versionPattern = new Regex(
        @"^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:-(?<prerelease>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$",
        RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticVersion"/> class.
    /// Creates a new semantic version from a version string.
    /// </summary>
    /// <param name="version">
    /// Version string in format x.y.z or x.y.z-prerelease
    /// where prerelease can be any combination of alphanumerics and hyphens separated by dots
    /// (e.g., "1.0.0-alpha", "1.0.0-beta.2", "1.0.0-rc.1", "1.0.0-preview.1.25080.5", etc.)
    /// </param>
    public SemanticVersion(string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        var match = s_versionPattern.Match(version);
        if (!match.Success)
        {
            throw new ArgumentException(
                $"The version '{version}' does not have the expected format x.y.z[-prerelease]",
                nameof(version));
        }

        Major = int.Parse(match.Groups["major"].Value, CultureInfo.InvariantCulture);
        Minor = int.Parse(match.Groups["minor"].Value, CultureInfo.InvariantCulture);
        Patch = int.Parse(match.Groups["patch"].Value, CultureInfo.InvariantCulture);

        if (match.Groups["prerelease"].Success)
        {
            PreRelease = match.Groups["prerelease"].Value;
            IsPreRelease = true;
        }
    }

    /// <summary>
    /// Gets the major version number.
    /// </summary>
    public int Major { get; }

    /// <summary>
    /// Gets the minor version number.
    /// </summary>
    public int Minor { get; }

    /// <summary>
    /// Gets the patch version number.
    /// </summary>
    public int Patch { get; }

    /// <summary>
    /// Gets the pre-release version string, e.g., "alpha.1", "beta.23", "preview.1.25080.5".
    /// Null if this is not a pre-release version.
    /// </summary>
    public string? PreRelease { get; }

    /// <summary>
    /// Gets a value indicating whether this is a pre-release version.
    /// </summary>
    public bool IsPreRelease { get; }

    /// <summary>
    /// Returns the string representation of this semantic version.
    /// </summary>
    public override string ToString()
    {
        return IsPreRelease
            ? $"{Major}.{Minor}.{Patch}-{PreRelease}"
            : $"{Major}.{Minor}.{Patch}";
    }

    /// <summary>
    /// Compares this instance with another <see cref="SemanticVersion"/> and returns an integer
    /// that indicates whether this instance precedes, follows, or occurs in the same
    /// position in the sort order as the other instance.
    /// </summary>
    /// <param name="other">The version to compare with this instance.</param>
    /// <returns>
    /// A value less than zero if this instance precedes <paramref name="other"/>,
    /// zero if this instance is equal to <paramref name="other"/>,
    /// or a value greater than zero if this instance follows <paramref name="other"/>.
    /// </returns>
    public int CompareTo(SemanticVersion? other)
    {
        if (other == null)
        {
            return 1;
        }

        // Compare major version
        var result = Major.CompareTo(other.Major);
        if (result != 0)
        {
            return result;
        }

        // Compare minor version
        result = Minor.CompareTo(other.Minor);
        if (result != 0)
        {
            return result;
        }

        // Compare patch version
        result = Patch.CompareTo(other.Patch);
        if (result != 0)
        {
            return result;
        }

        // A non-prerelease version is always greater than a prerelease version of the same numeric parts
        if (!IsPreRelease && other.IsPreRelease)
        {
            return 1;
        }

        if (IsPreRelease && !other.IsPreRelease)
        {
            return -1;
        }

        if (!IsPreRelease && !other.IsPreRelease)
        {
            return 0;
        }

        // Both are pre-releases, compare the pre-release identifiers
        return ComparePreReleaseVersions(PreRelease!, other.PreRelease!);
    }

    /// <summary>
    /// Compares two pre-release version strings according to SemVer 2.0.0 rules.
    /// </summary>
    private static int ComparePreReleaseVersions(string preRelease1, string preRelease2)
    {
        var parts1 = preRelease1.Split('.');
        var parts2 = preRelease2.Split('.');

        var minLength = Math.Min(parts1.Length, parts2.Length);

        // Compare each identifier
        for (var i = 0; i < minLength; i++)
        {
            var result = ComparePreReleaseIdentifiers(parts1[i], parts2[i]);
            if (result != 0)
            {
                return result;
            }
        }

        // If all identifiers up to the minimum length are equal,
        // the version with more identifiers is greater
        // e.g., "1.0.0-alpha" < "1.0.0-alpha.1"
        return parts1.Length.CompareTo(parts2.Length);
    }

    /// <summary>
    /// Compares two pre-release identifiers according to SemVer 2.0.0 rules.
    /// Numeric identifiers are compared numerically.
    /// Alphabetic or alphanumeric identifiers are compared lexically in ASCII sort order.
    /// </summary>
    private static int ComparePreReleaseIdentifiers(string id1, string id2)
    {
        // Check if both identifiers are numeric
        var isNum1 = int.TryParse(id1, out var num1);
        var isNum2 = int.TryParse(id2, out var num2);

        // If both are numeric, compare numerically
        if (isNum1 && isNum2)
        {
            return num1.CompareTo(num2);
        }

        // If only one is numeric, numeric is less
        // e.g., "1.0.0-1" < "1.0.0-alpha"
        if (isNum1)
        {
            return -1;
        }

        if (isNum2)
        {
            return 1;
        }

        // Otherwise, compare lexically
        // e.g., "1.0.0-alpha" < "1.0.0-beta"
        return string.Compare(id1, id2, StringComparison.Ordinal);
    }

    // Operator overloads for convenience
    public static bool operator <(SemanticVersion left, SemanticVersion right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(SemanticVersion left, SemanticVersion right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >(SemanticVersion left, SemanticVersion right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.CompareTo(right) > 0;
    }

    public static bool operator >=(SemanticVersion left, SemanticVersion right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.CompareTo(right) >= 0;
    }

    /// <summary>
    /// Determines which of two semantic versions is newer.
    /// </summary>
    /// <param name="version1">The first version to compare.</param>
    /// <param name="version2">The second version to compare.</param>
    /// <returns>The newer of the two versions.</returns>
    public static SemanticVersion GetNewerVersion(SemanticVersion version1, SemanticVersion version2)
    {
        ArgumentNullException.ThrowIfNull(version1);
        return version1.CompareTo(version2) >= 0 ? version1 : version2;
    }
}
