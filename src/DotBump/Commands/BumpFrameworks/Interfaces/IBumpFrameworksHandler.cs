// Copyright © Roby Van Damme.

using DotBump.Reports;

namespace DotBump.Commands.BumpFrameworks.Interfaces;

internal interface IBumpFrameworksHandler
{
    Task<BumpReport> HandleAsync(
        string repositoryPath,
        string globalJsonPath,
        IReadOnlyCollection<string>? excludedPaths = null);
}
