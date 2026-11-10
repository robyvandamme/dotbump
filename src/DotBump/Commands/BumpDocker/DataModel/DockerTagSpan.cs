// Copyright © Roby Van Damme.

namespace DotBump.Commands.BumpDocker.DataModel;

/// <summary>
/// The image tag as found in a Dockerfile, together with its text span so the tag can be replaced
/// without re-parsing the file.
/// </summary>
/// <param name="OriginalTag">The tag as it appears in the file.</param>
/// <param name="Start">The zero-based offset of the tag in the file text.</param>
/// <param name="Length">The length of the tag in the file text.</param>
internal readonly record struct DockerTagSpan(string OriginalTag, int Start, int Length);
