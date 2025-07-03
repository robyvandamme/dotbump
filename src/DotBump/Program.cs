// Copyright © 2025 Roby Van Damme.

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using DotBump;
using DotBump.Common;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Spectre.Console;
using Spectre.Console.Cli;

#if DEBUG
Serilog.Debugging.SelfLog.Enable(msg => Debug.WriteLine(msg));
#endif

var defaultLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Error);
if (ArgumentHandler.IsDebugMode(args))
{
    defaultLevelSwitch.MinimumLevel = LogEventLevel.Debug;
}

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.ControlledBy(defaultLevelSwitch)
#if DEBUG
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .WriteTo.File(
        "dotbumplog.txt", // TODO: what is a good log location and when do we want to log?
        rollingInterval: RollingInterval.Day,
        formatProvider: CultureInfo.InvariantCulture)
#endif
    .CreateLogger();

var versionInfo = new VersionInfo(Assembly.GetExecutingAssembly());
Log.Debug("DotBump version {@Version}", versionInfo);
Log.Debug("Configuring app");

var commandApp = new CommandApp();

AnsiConsole.WriteLine($"Initializing DotBump version {versionInfo.ProductVersion}");

commandApp.Configure(Log.Logger);

try
{
    Log.Debug("Starting app");
    return commandApp.Run(args);
}
#pragma warning disable CA1031
catch (Exception ex)
#pragma warning restore CA1031
{
    Log.Error(ex, "An error occurred");
    AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
