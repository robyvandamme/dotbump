// Copyright © Roby Van Damme.

using System.Globalization;

namespace DotBump.Commands.BumpLanguage.DataModel;

/// <summary>
/// Represents a numeric C# language version with an optional minor component.
/// </summary>
internal readonly record struct LanguageVersion(int Major, int Minor) : IComparable<LanguageVersion>
{
    /// <summary>
    /// The offset added to the SDK major version to determine the default C# language version.
    /// </summary>
    public const int SdkMajorOffset = 4;

    /// <summary>
    /// Creates the default C# language version for an SDK major version.
    /// </summary>
    /// <param name="sdkMajor">The SDK major version.</param>
    /// <returns>The language version matching the SDK major.</returns>
    public static LanguageVersion FromSdkMajor(int sdkMajor)
    {
        return new LanguageVersion(sdkMajor + SdkMajorOffset, 0);
    }

    /// <summary>
    /// Tries to parse a numeric language version such as <c>12</c> or <c>12.0</c>.
    /// </summary>
    /// <param name="value">The language version text.</param>
    /// <param name="version">The parsed language version when parsing succeeds.</param>
    /// <returns><see langword="true"/> when the value is a supported numeric language version.</returns>
    public static bool TryParse(string value, out LanguageVersion version)
    {
        version = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split('.', StringSplitOptions.None);
        if (parts.Length is < 1 or > 2
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var major))
        {
            return false;
        }

        var minor = 0;
        if (parts.Length == 2
            && !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out minor))
        {
            return false;
        }

        version = new LanguageVersion(major, minor);
        return true;
    }

    /// <inheritdoc />
    public int CompareTo(LanguageVersion other)
    {
        var majorComparison = Major.CompareTo(other.Major);
        return majorComparison != 0 ? majorComparison : Minor.CompareTo(other.Minor);
    }

    public static bool operator <(LanguageVersion left, LanguageVersion right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(LanguageVersion left, LanguageVersion right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >(LanguageVersion left, LanguageVersion right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator >=(LanguageVersion left, LanguageVersion right)
    {
        return left.CompareTo(right) >= 0;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return string.Create(CultureInfo.InvariantCulture, $"{Major}.{Minor}");
    }
}
