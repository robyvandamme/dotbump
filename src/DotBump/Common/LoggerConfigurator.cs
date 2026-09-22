// Copyright © Roby Van Damme.

using System.Globalization;
using DotBump;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace DotBump.Common;

internal static class LoggerConfigurator
{
    internal static void Configure(string[] args)
    {
        var defaultLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Error);

        if (ArgumentHandler.IsDebugMode(args))
        {
            defaultLevelSwitch.MinimumLevel = LogEventLevel.Debug;
        }

        var logFile = ArgumentHandler.LogFile(args);

        if (!string.IsNullOrWhiteSpace(logFile))
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(defaultLevelSwitch)
#if DEBUG
                .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
#endif
                .WriteTo.File(
                    logFile,
                    rollingInterval: RollingInterval.Day,
                    formatProvider: CultureInfo.InvariantCulture)
                .CreateLogger();
        }
        else
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(defaultLevelSwitch)
#if DEBUG
                .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
#endif
                .CreateLogger();
        }
    }
}
