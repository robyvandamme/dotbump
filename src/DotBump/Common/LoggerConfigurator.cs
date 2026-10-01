// Copyright © Roby Van Damme.

using System.Globalization;
using Destructurama;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace DotBump.Common;

internal static class LoggerConfigurator
{
    /// <summary>
    /// Creates the base logger configuration shared by <see cref="Configure"/> and the tests, so the
    /// attribute-based redaction (<c>[LogMasked]</c>, <c>[NotLogged]</c>) is wired in exactly one place
    /// and a test failure exposes an accidental removal of it.
    /// </summary>
    internal static LoggerConfiguration CreateConfiguration(LoggingLevelSwitch levelSwitch)
    {
        return new LoggerConfiguration()
            .MinimumLevel.ControlledBy(levelSwitch)
            .Destructure.UsingAttributes();
    }

    internal static void Configure(string[] args)
    {
        var defaultLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Error);

        if (ArgumentHandler.IsDebugMode(args))
        {
            defaultLevelSwitch.MinimumLevel = LogEventLevel.Debug;
        }

        var logFile = ArgumentHandler.LogFile(args);

        var configuration = CreateConfiguration(defaultLevelSwitch);
#if DEBUG
        configuration = configuration.WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);
#endif
        if (!string.IsNullOrWhiteSpace(logFile))
        {
            configuration = configuration.WriteTo.File(
                logFile,
                rollingInterval: RollingInterval.Day,
                formatProvider: CultureInfo.InvariantCulture);
        }

        Log.Logger = configuration.CreateLogger();
    }
}
