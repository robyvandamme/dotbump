// Copyright © Roby Van Damme.

namespace DotBump.Commands.BumpSdk;

internal record BumpSdkResult(bool Updated, string OldVersion, string NewVersion);
