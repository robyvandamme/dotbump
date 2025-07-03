// Copyright © 2025 Roby Van Damme.

namespace DotBump.Common;

internal static class ArgumentHandler
{
    internal static bool IsDebugMode(string[] args)
    {
        // Check if args is null or empty
        if (args.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--debug", StringComparison.OrdinalIgnoreCase))
            {
                // Check if this is the last argument or if the next argument isn't "true"
                if (i == args.Length - 1)
                {
                    // "--debug" is the last argument with no value
                    return false;
                }

                // Check if the next argument is "true"
                if (args[i + 1].Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // "--debug" is present but followed by something other than "true"
                return false;
            }
        }

        // "--debug" not found in arguments
        return false;
    }
}
