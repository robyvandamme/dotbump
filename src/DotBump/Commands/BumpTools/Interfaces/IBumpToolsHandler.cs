// Copyright © 2025 Roby Van Damme.

using DotBump.Reports;

namespace DotBump.Commands.BumpTools.Interfaces;

internal interface IBumpToolsHandler
{
    Task<BumpReport> HandleAsync(BumpType bumpType, string nugetConfigPath);
}
