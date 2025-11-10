// Copyright © 2025 Roby Van Damme.

using DotBump.Reports;

namespace DotBump.Commands.BumpSdk.Interfaces;

internal interface IBumpSdkHandler
{
    Task<BumpReport> HandleAsync(BumpType bumpType, string filePath, bool securityOnly);
}
