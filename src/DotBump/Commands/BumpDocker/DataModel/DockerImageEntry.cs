// Copyright © Roby Van Damme.

using DotBump.Common;

namespace DotBump.Commands.BumpDocker.DataModel;

/// <summary>
/// Represents a supported .NET SDK base image occurrence found in a Dockerfile.
/// </summary>
internal sealed class DockerImageEntry
{
    public DockerImageEntry(
        string image,
        string relativePath,
        string filePath,
        int lineNumber,
        string originalTag,
        int tagStart,
        int tagLength)
    {
        Image = image;
        RelativePath = relativePath;
        FilePath = filePath;
        LineNumber = lineNumber;
        OriginalTag = originalTag;
        ParsedVersion = new SemanticVersion(originalTag);
        Tag = originalTag;
        TagStart = tagStart;
        TagLength = tagLength;
    }

    /// <summary>
    /// Gets the image name used in reports, for example <c>mcr.microsoft.com/dotnet/sdk</c>.
    /// </summary>
    public string Image { get; }

    /// <summary>
    /// Gets the path of the Dockerfile relative to the repository root.
    /// </summary>
    public string RelativePath { get; }

    /// <summary>
    /// Gets the absolute path of the Dockerfile.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the one-based line number of the <c>FROM</c> instruction.
    /// </summary>
    public int LineNumber { get; }

    /// <summary>
    /// Gets the image tag as it was found in the Dockerfile.
    /// </summary>
    public string OriginalTag { get; }

    /// <summary>
    /// Gets the parsed semantic version of <see cref="OriginalTag"/>.
    /// </summary>
    public SemanticVersion ParsedVersion { get; }

    /// <summary>
    /// Gets or sets the image tag after the bump.
    /// </summary>
    public string Tag { get; set; }

    /// <summary>
    /// Gets the start offset of the raw image tag in the Dockerfile.
    /// </summary>
    public int TagStart { get; }

    /// <summary>
    /// Gets the length of the raw image tag in the Dockerfile.
    /// </summary>
    public int TagLength { get; }
}
