// Copyright © 2025 Roby Van Damme.

namespace DotBump.Commands.BumpSdk;

public record BumpSdkResult(bool Updated, string OldVersion, string NewVersion);
