// Copyright © Roby Van Damme.

using DotBump.Reports;

namespace DotBump.Commands.BumpPackages.Interfaces;

internal interface IBumpPackagesHandler
{
    Task<BumpReport> HandleAsync(BumpType bumpType, string repositoryPath, string nugetConfigPath);
}
