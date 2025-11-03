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
                    "Use the 'patch' type option to bump the SDK to the latest patch version for the current major version. ")
                .WithExample("sdk", "-o", "bump-sdk-result.json", "--security-only", "true")
                .WithExample("sdk", "-t", "patch", "-o", "bump-sdk-result.json", "-s", "true")
                .WithExample("sdk", "--type", "patch", "-f", "./other/global.json")
                .WithExample("sdk", "--debug", "true", "--logfile", "log.txt");

            config.Settings.Registrar.Register<IToolFileService, ToolFileService>();
            config.Settings.Registrar.Register<INuGetReleaseFinder, NuGetReleaseFinder>();
            config.Settings.Registrar.Register<IBumpToolsHandler, BumpToolsHandler>();
            config.Settings.Registrar.Register<INuGetClientFactory, NuGetClientFactory>();

            config.AddCommand<BumpToolsCommand>(name: "tools")
                .WithDescription(
                    "Bump the .NET tools versions. " +
                    "Use the 'minor' type option to bump the tools to the latest minor or patch versions for the current major version. " +
                    "Use the 'patch' type option to bump the tools to the latest patch version for the current major version. ")
                .WithExample("tools", "-o", "bump-tools-result.json")
                .WithExample("tools", "-t", "patch", "-o", "bump-tools-result.json")
                .WithExample("tools", "--debug", "true", "--logfile", "log.txt");
        });
    }
}
