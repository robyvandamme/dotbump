// Copyright © Roby Van Damme.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using DotBump;
using DotBump.Common;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

#if DEBUG
Serilog.Debugging.SelfLog.Enable(msg => Debug.WriteLine(msg));
#endif

LoggerConfigurator.Configure(args);

var versionInfo = new VersionInfo(Assembly.GetExecutingAssembly());
var platform = PlatformInfo.GetPlatform();

Log.Debug(
    "DotBump {Version} running on {Runtime}, {Platform} {Architecture} (OS details: {OSDescription})",
    versionInfo.Version,
    RuntimeInformation.FrameworkDescription,
    platform,
    RuntimeInformation.ProcessArchitecture,
    RuntimeInformation.OSDescription);
Log.Debug("Configuring app");

var commandApp = new CommandApp();

commandApp.Configure(Log.Logger);

try
{
    Log.Debug("Starting app");
    return await commandApp.RunAsync(args);
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
    await Log.CloseAndFlushAsync();
}
