// Copyright © 2025 Roby Van Damme.

using System.Diagnostics;
using DotBump.Commands.Sdk;
using Serilog;
using Spectre.Console.Cli;

namespace DotBump;

internal static class CommandConfiguration
{
    internal static void Configure(this CommandApp commandApp, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(nameof(commandApp));
        ArgumentNullException.ThrowIfNull(nameof(logger));

        Debug.Assert(commandApp != null, nameof(commandApp) + " != null");
        commandApp.Configure(config =>
        {
#if DEBUG
            config.PropagateExceptions();
            config.ValidateExamples();
#endif
            config.SetApplicationName("dotbump");
            config.Settings.Registrar.RegisterInstance(Log.Logger);

            config.AddCommand<BumpSdkSettings>(name: "sdk");
        });
    }
}
