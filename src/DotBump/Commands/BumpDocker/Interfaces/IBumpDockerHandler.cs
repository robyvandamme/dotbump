// Copyright © Roby Van Damme.

using DotBump.Reports;

namespace DotBump.Commands.BumpDocker.Interfaces;

internal interface IBumpDockerHandler
{
    Task<BumpReport> HandleAsync(
        BumpType bumpType,
        string repositoryPath,
        IReadOnlyCollection<string>? excludedPaths = null,
        bool preRelease = false);
}
