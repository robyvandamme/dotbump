// Copyright © Roby Van Damme.

using DotBump.Commands.BumpDocker.DataModel;

namespace DotBump.Commands.BumpDocker.Interfaces;

internal interface IDockerfileService
{
    DockerfileManifest GetDockerfileManifest(
        string repositoryPath,
        IReadOnlyCollection<string>? excludedPaths = null);

    void SaveDockerfileManifest(DockerfileManifest manifest);
}
