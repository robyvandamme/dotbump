// Copyright © 2025 Roby Van Damme.

namespace DotBump.Commands.BumpSdk;

public interface IBumpSdkHandler
{
    Task<BumpSdkResult> HandleAsync(BumpType bumpType, string filePath);
}
