// Copyright © Roby Van Damme.

namespace DotBump.Common;

/// <summary>
/// Helpers for validating user supplied paths without throwing.
/// </summary>
internal static class PathValidation
{
    /// <summary>
    /// Tries to normalize the supplied path using <see cref="Path.GetFullPath(string)"/>, returning
    /// <see langword="false"/> instead of throwing when the path is not valid.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <param name="normalizedPath">The normalized path when the input is valid.</param>
    /// <returns><see langword="true"/> when the path could be normalized; otherwise <see langword="false"/>.</returns>
    public static bool TryGetFullPath(string path, out string normalizedPath)
    {
        try
        {
            normalizedPath = Path.GetFullPath(path);
            return true;
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException)
        {
            normalizedPath = string.Empty;
            return false;
        }
    }
}
