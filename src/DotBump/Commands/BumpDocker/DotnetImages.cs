// Copyright © Roby Van Damme.

namespace DotBump.Commands.BumpDocker;

/// <summary>
/// The .NET base images this command supports and the release component each one is tagged with.
/// </summary>
internal static class DotnetImages
{
    internal const string Sdk = "mcr.microsoft.com/dotnet/sdk";
    internal const string AspNet = "mcr.microsoft.com/dotnet/aspnet";
    internal const string Runtime = "mcr.microsoft.com/dotnet/runtime";
    internal const string RuntimeDeps = "mcr.microsoft.com/dotnet/runtime-deps";

    /// <summary>
    /// Tries to resolve the version source for a supported image.
    /// </summary>
    /// <param name="image">The image name, without a tag or digest.</param>
    /// <param name="versionSource">The release component the image is tagged with.</param>
    /// <returns><see langword="true"/> when the image is supported.</returns>
    internal static bool TryGetVersionSource(string image, out DotnetImageVersionSource versionSource)
    {
        if (string.IsNullOrWhiteSpace(image))
        {
            versionSource = default;
            return false;
        }

        if (string.Equals(image, Sdk, StringComparison.OrdinalIgnoreCase))
        {
            versionSource = DotnetImageVersionSource.Sdk;
            return true;
        }

        if (string.Equals(image, AspNet, StringComparison.OrdinalIgnoreCase))
        {
            versionSource = DotnetImageVersionSource.AspNetCore;
            return true;
        }

        if (string.Equals(image, Runtime, StringComparison.OrdinalIgnoreCase)
            || string.Equals(image, RuntimeDeps, StringComparison.OrdinalIgnoreCase))
        {
            versionSource = DotnetImageVersionSource.Runtime;
            return true;
        }

        versionSource = default;
        return false;
    }
}
