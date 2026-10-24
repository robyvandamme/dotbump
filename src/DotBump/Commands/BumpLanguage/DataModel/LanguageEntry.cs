// Copyright © Roby Van Damme.

namespace DotBump.Commands.BumpLanguage.DataModel;

/// <summary>
/// Represents a LangVersion occurrence found in a C# project or shared MSBuild file.
/// </summary>
internal sealed class LanguageEntry
{
    public LanguageEntry(
        string id,
        string filePath,
        string originalVersion,
        LanguageVersion parsedVersion,
        int versionStart,
        int versionLength)
    {
        Id = id;
        FilePath = filePath;
        OriginalVersion = originalVersion;
        ParsedVersion = parsedVersion;
        Version = originalVersion;
        VersionStart = versionStart;
        VersionLength = versionLength;
    }

    /// <summary>
    /// Gets the identifier used in reports, typically the path relative to the repository root.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the absolute path of the file containing the language version.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the language version as it was found in the file.
    /// </summary>
    public string OriginalVersion { get; }

    /// <summary>
    /// Gets the parsed numeric language version.
    /// </summary>
    public LanguageVersion ParsedVersion { get; }

    /// <summary>
    /// Gets or sets the language version after the bump.
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// Gets the start offset of the raw language version value in the file.
    /// </summary>
    public int VersionStart { get; }

    /// <summary>
    /// Gets the length of the raw language version value in the file.
    /// </summary>
    public int VersionLength { get; }
}
