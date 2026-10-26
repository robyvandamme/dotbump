// Copyright © Roby Van Damme.

namespace DotBump;

internal static class ArgumentHandler
{
    internal static bool IsDebugMode(string[] args)
    {
        // Check if args is null or empty
        if (args.Length == 0)
        {
            return false;
        }

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--debug", StringComparison.OrdinalIgnoreCase))
            {
                // The flag enables debug logging unless it is explicitly disabled with a "false" value.
                if (i < args.Length - 1 && args[i + 1].Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // "--debug" is present with no value, an unrecognized value or "true"
                return true;
            }

            // The "--debug=value" form carries its value in the same token.
            if (args[i].StartsWith("--debug=", StringComparison.OrdinalIgnoreCase))
            {
                return !args[i]["--debug=".Length..].Equals("false", StringComparison.OrdinalIgnoreCase);
            }
        }

        // "--debug" not found in arguments
        return false;
    }

    internal static string LogFile(string[] args)
    {
        if (args.Length == 0)
        {
            return string.Empty;
        }

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--logfile", StringComparison.OrdinalIgnoreCase))
            {
                // Check if this is the last argument
                if (i == args.Length - 1)
                {
                    // "--logfile" is the last argument with no value
                    return string.Empty;
                }

                return args[i + 1];
            }

            // The "--logfile=value" form carries its value in the same token.
            if (args[i].StartsWith("--logfile=", StringComparison.OrdinalIgnoreCase))
            {
                return args[i]["--logfile=".Length..];
            }
        }

        // "--logfile" not found in arguments
        return string.Empty;
    }
}
