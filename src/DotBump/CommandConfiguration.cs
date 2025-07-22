// Copyright © 2025 Roby Van Damme.

using System.Diagnostics;
using DotBump.Commands.BumpSdk;
using DotBump.Commands.BumpSdk.Interfaces;
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
            config.Settings.Registrar.RegisterInstance(logger);

            config.Settings.Registrar.Register<ISdkFileService, SdkFileService>();
            config.Settings.Registrar.Register<IReleaseService, ReleaseWebService>();
            config.Settings.Registrar.Register<IReleaseFinder, ReleaseFinder>();
            config.Settings.Registrar.Register<IBumpSdkHandler, BumpSdkHandler>();

            config.AddCommand<BumpSdkCommand>(name: "sdk");
        });
    }
}
