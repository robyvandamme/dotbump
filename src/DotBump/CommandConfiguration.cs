// Copyright © Roby Van Damme.

using DotBump.Commands.BumpFrameworks;
using DotBump.Commands.BumpFrameworks.Interfaces;
using DotBump.Commands.BumpLanguage;
using DotBump.Commands.BumpLanguage.Interfaces;
using DotBump.Commands.BumpPackages;
using DotBump.Commands.BumpPackages.Interfaces;
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
    private const string FrameworksCommandName = "frameworks";
    private const string LanguageCommandName = "language";
    private const string TypeOptionName = "--type";
    private const string PreReleaseOptionName = "--prerelease";
    private const string OutputOptionName = "--output";
    private const string DebugOptionName = "--debug";
    private const string LogFileOptionName = "--logfile";
    private const string PatchBumpType = "patch";
    private const string MajorBumpType = "major";

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
                    "Use the 'patch' type option to bump the SDK to the latest patch version for the current minor version. " +
                    "Use the 'major' type option to bump the SDK to the latest version, including versions with a higher major version. " +
                    "Use '--prerelease' with the 'major' type to include pre-release versions. ")
                .WithExample(SdkCommandName)
                .WithExample(SdkCommandName, TypeOptionName, PatchBumpType)
                .WithExample(SdkCommandName, TypeOptionName, MajorBumpType)
                .WithExample(SdkCommandName, TypeOptionName, MajorBumpType, PreReleaseOptionName, "true")
                .WithExample(SdkCommandName, "--file", "./other/global.json", OutputOptionName, "bump-sdk-report.json")
                .WithExample(
                    SdkCommandName,
                    "--security-only",
                    "true",
                    DebugOptionName,
                    "true",
                    LogFileOptionName,
                    "bump-sdk-log.txt");

            config.Settings.Registrar.Register<IToolFileService, ToolFileService>();
            config.Settings.Registrar.Register<IToolManifestLocator, ToolManifestLocator>();
            config.Settings.Registrar.Register<INuGetConfigFileService, NuGetConfigFileService>();
            config.Settings.Registrar.Register<INuGetReleaseFinder, NuGetReleaseFinder>();
            config.Settings.Registrar.Register<IBumpToolsHandler, BumpToolsHandler>();
            config.Settings.Registrar.Register<INuGetClientFactory, NuGetClientFactory>();
            config.Settings.Registrar.Register<INuGetConfigValidator, NuGetConfigValidator>();
            config.Settings.Registrar.Register<IPackageVersionResolver, PackageVersionResolver>();

            config.AddCommand<BumpToolsCommand>(name: ToolsCommandName)
                .WithDescription(
                    "Bump the local .NET tools versions. " +
                    "Use the 'minor' type option to bump the tools to the latest minor or patch versions for the current major version. " +
                    "Use the 'patch' type option to bump the tools to the latest patch version for the current minor version. " +
                    "Use the 'major' type option to bump the tools to the latest version, including versions with a higher major version. " +
                    "Use '--prerelease' with the 'major' type to include pre-release versions. ")
                .WithExample(ToolsCommandName)
                .WithExample(ToolsCommandName, TypeOptionName, PatchBumpType)
                .WithExample(ToolsCommandName, TypeOptionName, MajorBumpType)
                .WithExample(ToolsCommandName, TypeOptionName, MajorBumpType, PreReleaseOptionName, "true")
                .WithExample(ToolsCommandName, "--manifest", "./other/dotnet-tools.json")
                .WithExample(
                    ToolsCommandName,
                    "--config",
                    "./custom-nuget.config",
                    OutputOptionName,
                    "bump-tools-report.json")
                .WithExample(
                    ToolsCommandName,
                    DebugOptionName,
                    "true",
                    LogFileOptionName,
                    "bump-tools-log.txt");

            config.Settings.Registrar.Register<IPackageFileService, PackageFileService>();
            config.Settings.Registrar.Register<IBumpPackagesHandler, BumpPackagesHandler>();

            config.AddCommand<BumpPackagesCommand>(name: PackagesCommandName)
                .WithDescription(
                    "Bump the NuGet package versions. " +
                    "Use the 'minor' type option to bump the packages to the latest minor or patch versions for the current major version. " +
                    "Use the 'patch' type option to bump the packages to the latest patch version for the current minor version. " +
                    "Use the 'major' type option to bump the packages to the latest version, including versions with a higher major version. " +
                    "Use '--prerelease' with the 'major' type to include pre-release versions. ")
                .WithExample(PackagesCommandName)
                .WithExample(PackagesCommandName, TypeOptionName, PatchBumpType)
                .WithExample(PackagesCommandName, TypeOptionName, MajorBumpType)
                .WithExample(PackagesCommandName, TypeOptionName, MajorBumpType, PreReleaseOptionName, "true")
                .WithExample(
                    PackagesCommandName,
                    "--config",
                    "./custom-nuget.config",
                    OutputOptionName,
                    "bump-packages-report.json")
                .WithExample(
                    PackagesCommandName,
                    DebugOptionName,
                    "true",
                    LogFileOptionName,
                    "bump-packages-log.txt");

            config.Settings.Registrar.Register<IFrameworkFileService, FrameworkFileService>();
            config.Settings.Registrar.Register<IBumpFrameworksHandler, BumpFrameworksHandler>();

            config.AddCommand<BumpFrameworksCommand>(name: FrameworksCommandName)
                .WithDescription("Bump the target framework versions.");

            config.Settings.Registrar.Register<ILanguageFileService, LanguageFileService>();
            config.Settings.Registrar.Register<IBumpLanguageHandler, BumpLanguageHandler>();

            config.AddCommand<BumpLanguageCommand>(name: LanguageCommandName)
                .WithDescription(
                    "Bump the C# language version. " +
                    "The language version is derived from the SDK major version in global.json as SDK major + 4. " +
                    "Only explicit numeric LangVersion values in C# projects and shared MSBuild files are bumped.")
                .WithExample(LanguageCommandName)
                .WithExample(LanguageCommandName, "--file", "./other/global.json", "--path", "./src")
                .WithExample(LanguageCommandName, OutputOptionName, "bump-language-report.json")
                .WithExample(
                    LanguageCommandName,
                    DebugOptionName,
                    "true",
                    LogFileOptionName,
                    "bump-language-log.txt");
        });
    }
}
