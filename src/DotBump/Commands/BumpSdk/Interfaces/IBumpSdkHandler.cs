// Copyright © 2025 Roby Van Damme.

namespace DotBump.Commands.BumpSdk.Interfaces;

internal interface IBumpSdkHandler
{
    Task<BumpSdkResult> HandleAsync(BumpType bumpType, string filePath, bool security = false);
}
