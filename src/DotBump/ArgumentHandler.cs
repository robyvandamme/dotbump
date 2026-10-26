// Copyright © Roby Van Damme.

namespace DotBump;

internal static class ArgumentHandler
{
    private const string DebugOption = "--debug";
    private const string DebugOptionPrefix = "--debug=";
    private const string LogFileOption = "--logfile";
    private const string LogFileOptionPrefix = "--logfile=";

    /// <summary>
    /// Determines whether debug logging is enabled. Aligned with the Spectre.Console.Cli grammar so that the outcome
    /// depends only on argument forms that the application would also accept: explicit "true"/"false" values and the
    /// bare flag. Any other value is rejected by Spectre, so the helper fails closed (returns false).
    /// </summary>
    internal static bool IsDebugMode(string[] args)
    {
        // Check if args is empty
        if (args.Length == 0)
        {
            return false;
        }

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Equals(DebugOption, StringComparison.OrdinalIgnoreCase))
            {
                // An explicit "true"/"false" value token enables or disables debug logging.
                if (i < args.Length - 1 && args[i + 1].Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (i < args.Length - 1 && args[i + 1].Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // A following non-option token is a value Spectre rejects (the flag takes no separate value), so
                // fail closed. A following option or nothing means the flag was used bare.
                if (i < args.Length - 1 && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    return false;
                }

                return true;
            }

            // The "--debug=value" form carries its value in the same token. Only "true" enables; anything else is
            // either explicitly disabled or a value Spectre rejects.
            if (args[i].StartsWith(DebugOptionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return args[i][DebugOptionPrefix.Length..].Equals("true", StringComparison.OrdinalIgnoreCase);
            }
        }

        // "--debug" not found in arguments
        return false;
    }

    /// <summary>
    /// Determines the log file, if requested. Note that "--logfile=" (empty inline value) is rejected by Spectre, so
    /// it can only affect the logging configured for a run that is about to fail; returning no file name is the safe
    /// outcome.
    /// </summary>
    internal static string LogFile(string[] args)
    {
        // Check if args is empty
        if (args.Length == 0)
        {
            return string.Empty;
        }

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Equals(LogFileOption, StringComparison.OrdinalIgnoreCase))
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
            if (args[i].StartsWith(LogFileOptionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return args[i][LogFileOptionPrefix.Length..];
            }
        }

        // "--logfile" not found in arguments
        return string.Empty;
    }
}
