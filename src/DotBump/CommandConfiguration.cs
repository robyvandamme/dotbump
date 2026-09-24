// Copyright © Roby Van Damme.

using DotBump.Commands.BumpPackages;
using DotBump.Commands.BumpSdk;
using DotBump.Commands.BumpSdk.Interfaces;
using DotBump.Commands.BumpTools;
using DotBump.Commands.BumpTools.Interfaces;
using DotBump.Common;
using DotBump.NuGet;
using DotBump.NuGet.Interfaces;
using Serilog;
using Spectre.Console.Cli;

namespace DotBump;

internal static class CommandConfiguration
{
    private const string SdkCommandName = "sdk";
    private const string ToolsCommandName = "tools";
    private const string PackagesCommandName = "packages";

    internal static void Configure(this CommandApp commandApp, ILogger logger, VersionInfo versionInfo)
    {
        ArgumentNullException.ThrowIfNull(commandApp);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(versionInfo);

        commandApp.Configure(config =>
        {
#if DEBUG
            config.PropagateExceptions();
            config.ValidateExamples();
#endif
            config.SetApplicationName("dotbump");
            config.SetApplicationVersion(versionInfo.Version ?? "not available");
            config.Settings.Registrar.RegisterInstance(logger);

            config.Settings.Registrar.Register<ISdkFileService, SdkFileService>();
            config.Settings.Registrar.Register<IReleaseService, ReleaseWebService>();
            config.Settings.Registrar.Register<IReleaseFinder, ReleaseFinder>();
            config.Settings.Registrar.Register<IBumpSdkHandler, BumpSdkHandler>();

            config.AddCommand<BumpSdkCommand>(name: SdkCommandName)
                .WithDescription(
                    "Bump the global.json SDK version. " +
                    "Use the 'minor' type option to bump the SDK to the latest minor or patch version for the current major version. " +
                    "Use the 'patch' type option to bump the SDK to the latest patch version for the current minor version. ")
                .WithExample(SdkCommandName)
                .WithExample(SdkCommandName, "--type", "patch")
                .WithExample(SdkCommandName, "--file", "./other/global.json", "--output", "bump-sdk-report.json")
                .WithExample(SdkCommandName, "--security-only", "true", "--debug", "true", "--logfile", "bump-sdk-log.txt");

            config.Settings.Registrar.Register<IToolFileService, ToolFileService>();
            config.Settings.Registrar.Register<INuGetConfigFileService, NuGetConfigFileService>();
            config.Settings.Registrar.Register<INuGetReleaseFinder, NuGetReleaseFinder>();
            config.Settings.Registrar.Register<IBumpToolsHandler, BumpToolsHandler>();
            config.Settings.Registrar.Register<INuGetClientFactory, NuGetClientFactory>();
            config.Settings.Registrar.Register<INuGetConfigValidator, NuGetConfigValidator>();

            config.AddCommand<BumpToolsCommand>(name: ToolsCommandName)
                .WithDescription(
                    "Bump the local .NET tools versions. " +
                    "Use the 'minor' type option to bump the tools to the latest minor or patch versions for the current major version. " +
                    "Use the 'patch' type option to bump the tools to the latest patch version for the current minor version. ")
                .WithExample(ToolsCommandName)
                .WithExample(ToolsCommandName, "--type", "patch")
                .WithExample(ToolsCommandName, "--config", "./custom-nuget.config", "--output", "bump-tools-report.json")
                .WithExample(ToolsCommandName, "--debug", "true", "--logfile", "bump-tools-log.txt");

            config.AddCommand<BumpPackagesCommand>(name: PackagesCommandName)
                .WithDescription(
                    "Bump the NuGet package versions. " +
                    "Use the 'minor' type option to bump the packages to the latest minor or patch versions for the current major version. " +
                    "Use the 'patch' type option to bump the packages to the latest patch version for the current minor version. ")
                .WithExample(PackagesCommandName)
                .WithExample(PackagesCommandName, "--type", "patch")
                .WithExample(PackagesCommandName, "--config", "./custom-nuget.config", "--output", "bump-packages-report.json")
                .WithExample(PackagesCommandName, "--debug", "true", "--logfile", "bump-packages-log.txt");
        });
    }
}
