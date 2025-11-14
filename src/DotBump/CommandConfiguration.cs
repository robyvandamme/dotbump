// Copyright © 2025 Roby Van Damme.

using System.Diagnostics;
using DotBump.Commands.BumpSdk;
using DotBump.Commands.BumpSdk.Interfaces;
using DotBump.Commands.BumpTools;
using DotBump.Commands.BumpTools.Interfaces;
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

            config.AddCommand<BumpSdkCommand>(name: "sdk")
                .WithDescription(
                    "Bump the global.json SDK version. " +
                    "Use the 'minor' type option to bump the SDK to the latest minor or patch version for the current major version. " +
                    "Use the 'patch' type option to bump the SDK to the latest patch version for the current minor version. ")
                .WithExample("sdk")
                .WithExample("sdk", "--type", "patch")
                .WithExample("sdk", "--file", "./other/global.json", "--output", "bump-sdk-report.json")
                .WithExample("sdk", "--security-only", "true", "--debug", "true", "--logfile", "bump-sdk-log.txt");

            config.Settings.Registrar.Register<IToolFileService, ToolFileService>();
            config.Settings.Registrar.Register<INuGetReleaseFinder, NuGetReleaseFinder>();
            config.Settings.Registrar.Register<IBumpToolsHandler, BumpToolsHandler>();
            config.Settings.Registrar.Register<INuGetClientFactory, NuGetClientFactory>();
            config.Settings.Registrar.Register<INuGetConfigValidator, NuGetConfigValidator>();

            config.AddCommand<BumpToolsCommand>(name: "tools")
                .WithDescription(
                    "Bump the local .NET tools versions. " +
                    "Use the 'minor' type option to bump the tools to the latest minor or patch versions for the current major version. " +
                    "Use the 'patch' type option to bump the tools to the latest patch version for the current minor version. ")
                .WithExample("tools")
                .WithExample("tools", "--type", "patch")
                .WithExample("tools", "--config", "./custom-nuget.config", "--output", "bump-tools-report.json")
                .WithExample("tools", "--debug", "true", "--logfile", "bump-tools-log.txt");
        });
    }
}
