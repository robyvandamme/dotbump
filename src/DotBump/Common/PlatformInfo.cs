// Copyright © Roby Van Damme.

namespace DotBump.Common;

internal static class PlatformInfo
{
    internal static string GetPlatform()
    {
        if (OperatingSystem.IsWindows())
        {
            return "Windows";
        }

        if (OperatingSystem.IsMacOS())
        {
            return "macOS";
        }

        if (OperatingSystem.IsLinux())
        {
            return "Linux";
        }

        if (OperatingSystem.IsFreeBSD())
        {
            return "FreeBSD";
        }

        return "Unknown OS";
    }
}
