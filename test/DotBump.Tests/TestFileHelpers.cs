// Copyright © 2025 Roby Van Damme.

using DotBump.Tests.Common;

namespace DotBump.Tests;

public static class TestFileHelpers
{
    public static void EnsureDirectoryCreated(this LocalDirectory localDirectory)
    {
        ArgumentNullException.ThrowIfNull(localDirectory);
        var info = new DirectoryInfo(localDirectory.AbsolutePath);
        info.Create();
    }

    public static void EnsureDirectoryDeleted(this LocalDirectory localDirectory)
    {
        ArgumentNullException.ThrowIfNull(localDirectory);
        var info = new DirectoryInfo(localDirectory.AbsolutePath);
        if (info.Exists)
        {
            info.Delete(true);
        }
    }

    public static void EnsureFileCreated(this LocalDirectory localDirectory, string fileName)
    {
        ArgumentNullException.ThrowIfNull(localDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        var info = new FileInfo(Path.Combine(localDirectory.AbsolutePath, fileName));
        info.Create();
    }

    public static void EnsureFileCreated(this LocalDirectory localDirectory, string fileName, string content)
    {
        ArgumentNullException.ThrowIfNull(localDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        var directoryInfo = new DirectoryInfo(localDirectory.AbsolutePath);
        directoryInfo.Create();
        File.WriteAllText(Path.Combine(localDirectory.AbsolutePath, fileName), content);
    }

    public static void EnsureFileDeleted(this LocalDirectory localDirectory, string fileName)
    {
        ArgumentNullException.ThrowIfNull(localDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        var info = new FileInfo(Path.Combine(localDirectory.AbsolutePath, fileName));
        if (info.Exists)
        {
            info.Delete();
        }
    }
}
