// Copyright © 2025 Roby Van Damme.

namespace DotBump.Commands.BumpTools.Interfaces;

internal interface IBumpToolsHandler
{
    Task<IReadOnlyCollection<BumpToolResult>> HandleAsync(BumpType bumpType);
}
