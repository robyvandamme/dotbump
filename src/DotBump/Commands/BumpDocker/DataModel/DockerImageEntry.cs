// Copyright © Roby Van Damme.

using DotBump.Common;

namespace DotBump.Commands.BumpDocker.DataModel;

/// <summary>
/// Represents a supported .NET base image occurrence found in a Dockerfile.
/// </summary>
internal sealed class DockerImageEntry
{
    public DockerImageEntry(
        string image,
        DotnetImageVersionSource versionSource,
        string relativePath,
        string filePath,
        int lineNumber,
        DockerTagSpan tag)
    {
        Image = image;
        VersionSource = versionSource;
        RelativePath = relativePath;
        FilePath = filePath;
        LineNumber = lineNumber;
        TagSpan = tag;
        ParsedVersion = new SemanticVersion(tag.OriginalTag);
        Tag = tag.OriginalTag;
    }

    /// <summary>
    /// Gets the image name used in reports, for example <c>mcr.microsoft.com/dotnet/sdk</c>.
    /// </summary>
    public string Image { get; }

    /// <summary>
    /// Gets the release component the image is tagged with.
    /// </summary>
    public DotnetImageVersionSource VersionSource { get; }

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
    /// Gets the image tag as it was found in the Dockerfile, together with its text span.
    /// </summary>
    public DockerTagSpan TagSpan { get; }

    /// <summary>
    /// Gets the image tag as it was found in the Dockerfile.
    /// </summary>
    public string OriginalTag => TagSpan.OriginalTag;

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
    public int TagStart => TagSpan.Start;

    /// <summary>
    /// Gets the length of the raw image tag in the Dockerfile.
    /// </summary>
    public int TagLength => TagSpan.Length;
}
