// Copyright © 2025 Roby Van Damme.

namespace DotBump.Tests.Common;

public record LocalDirectory(string RelativePath)
{
    public string AbsolutePath { get; init; } = Path.GetFullPath(RelativePath);

    public string RelativePath { get; init; } = RelativePath;

    public bool Exists()
    {
        return Directory.Exists(AbsolutePath);
    }
}
