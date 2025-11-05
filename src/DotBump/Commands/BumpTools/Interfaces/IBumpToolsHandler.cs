// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpTools.DataModel.Report;

namespace DotBump.Commands.BumpTools.Interfaces;

internal interface IBumpToolsHandler
{
    Task<BumpReport> HandleAsync(BumpType bumpType, string nugetConfigPath);
}
