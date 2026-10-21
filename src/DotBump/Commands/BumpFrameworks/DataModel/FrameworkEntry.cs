// Copyright © Roby Van Damme.

namespace DotBump.Commands.BumpFrameworks.DataModel;

/// <summary>
/// Represents a target framework occurrence found in a project or props file.
/// </summary>
internal sealed class FrameworkEntry
{
    public FrameworkEntry(
        string id,
        string filePath,
        string originalFramework,
        string suffix,
        int frameworkStart,
        int frameworkLength)
    {
        Id = id;
        FilePath = filePath;
        OriginalFramework = originalFramework;
        Suffix = suffix;
        Framework = originalFramework;
        FrameworkStart = frameworkStart;
        FrameworkLength = frameworkLength;
    }

    /// <summary>
    /// Gets the identifier used in reports, typically the path relative to the repository root.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the absolute path of the file containing the target framework.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the target framework as it was found in the file.
    /// </summary>
    public string OriginalFramework { get; }

    /// <summary>
    /// Gets the platform suffix of the target framework, for example <c>-windows</c>.
    /// </summary>
    public string Suffix { get; }

    /// <summary>
    /// Gets or sets the target framework after the bump.
    /// </summary>
    public string Framework { get; set; }

    /// <summary>
    /// Gets the start offset of the raw target framework value in the file.
    /// </summary>
    public int FrameworkStart { get; }

    /// <summary>
    /// Gets the length of the raw target framework value in the file.
    /// </summary>
    public int FrameworkLength { get; }
}
