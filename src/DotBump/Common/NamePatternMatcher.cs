// Copyright © Roby Van Damme.

using System.Text.RegularExpressions;

namespace DotBump.Common;

/// <summary>
/// Matches identifiers such as package and tool ids against exact values or wildcard patterns.
/// Patterns support <c>*</c> (zero or more characters) and <c>?</c> (exactly one character), and
/// matching is case-insensitive. Multiple patterns can be supplied as separate values or as a
/// comma-separated list.
/// </summary>
internal sealed class NamePatternMatcher
{
    private readonly IReadOnlyList<(string Value, Regex Regex)> _patterns;

    private NamePatternMatcher(IReadOnlyList<(string Value, Regex Regex)> patterns)
    {
        _patterns = patterns;
    }

    /// <summary>
    /// Creates a matcher from the supplied patterns. Empty and whitespace-only patterns are ignored.
    /// </summary>
    /// <param name="patterns">The exact values or wildcard patterns.</param>
    /// <returns>The matcher.</returns>
    public static NamePatternMatcher Create(IEnumerable<string>? patterns)
    {
        var compiledPatterns = Normalize(patterns)
            .Select(pattern => (Value: pattern, Regex: CreateRegex(pattern)))
            .ToList();

        return new NamePatternMatcher(compiledPatterns);
    }

    /// <summary>
    /// Returns the supplied patterns as a normalized list: comma-separated values are split, each value is
    /// trimmed, empty values are removed, and duplicates are removed case-insensitively.
    /// </summary>
    /// <param name="patterns">The raw pattern values.</param>
    /// <returns>The normalized patterns.</returns>
    public static IReadOnlyList<string> Normalize(IEnumerable<string>? patterns)
    {
        return (patterns ?? [])
            .SelectMany(pattern => (pattern ?? string.Empty).Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(pattern => pattern.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Determines whether the supplied name matches one of the patterns.
    /// </summary>
    /// <param name="name">The identifier to match.</param>
    /// <param name="matchedPattern">The first pattern that matched, or <see langword="null"/> when none did.</param>
    /// <returns><see langword="true"/> when a pattern matched.</returns>
    public bool TryMatch(string name, out string? matchedPattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        foreach (var (value, regex) in _patterns)
        {
            if (regex.IsMatch(name))
            {
                matchedPattern = value;
                return true;
            }
        }

        matchedPattern = null;
        return false;
    }

    private static Regex CreateRegex(string pattern)
    {
        var expression = "^"
                         + Regex.Escape(pattern)
                             .Replace("\\*", ".*", StringComparison.Ordinal)
                             .Replace("\\?", ".", StringComparison.Ordinal)
                         + "$";

        return new Regex(
            expression,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
    }
}
