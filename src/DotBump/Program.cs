// Copyright © Roby Van Damme.

using System.Diagnostics;
using System.Reflection;
using DotBump;
using DotBump.Common;
using Serilog;
using Serilog.Debugging;
using Spectre.Console;
using Spectre.Console.Cli;

#if DEBUG
SelfLog.Enable(msg => Debug.WriteLine(msg));
#endif

try
{
    var versionInfo = new VersionInfo(Assembly.GetExecutingAssembly());
    var commandApp = new CommandApp();

    // Logging is configured by LoggingInterceptor once Spectre has parsed the command line, so the registered logger
    // forwards to the active Log.Logger at call time. See LoggingInterceptor for the lifecycle.
    commandApp.Configure(new DelegatingLogger(), versionInfo);

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
