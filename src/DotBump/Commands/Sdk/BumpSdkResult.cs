// Copyright © 2025 Roby Van Damme.

namespace DotBump.Commands.Sdk;

public record BumpSdkResult(bool Updated, string OldVersion, string NewVersion);
