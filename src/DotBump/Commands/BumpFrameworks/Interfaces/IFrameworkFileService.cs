// Copyright © Roby Van Damme.

using DotBump.Commands.BumpFrameworks.DataModel;

namespace DotBump.Commands.BumpFrameworks.Interfaces;

internal interface IFrameworkFileService
{
    FrameworkManifest GetFrameworkManifest(
        string repositoryPath,
        IReadOnlyCollection<string>? excludedPaths = null);

    void SaveFrameworkManifest(FrameworkManifest manifest);
}
