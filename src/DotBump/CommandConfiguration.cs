// Copyright © Roby Van Damme.

using DotBump.Commands;
using DotBump.Commands.BumpFrameworks;
using DotBump.Commands.BumpFrameworks.Interfaces;
using DotBump.Commands.BumpLanguage;
using DotBump.Commands.BumpLanguage.DataModel;
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

            config.AddCommand<BumpSdkCommand>(name: BumpCommandNames.Sdk)
                .WithDescription(
                    "Bump the global.json SDK version. " +
                    "Use the 'minor' type option to bump the SDK to the latest minor or patch version for the current major version. " +
                    "Use the 'patch' type option to bump the SDK to the latest patch version for the current minor version. " +
                    "Use the 'major' type option to bump the SDK to the latest version, including versions with a higher major version. " +
                    "Use '--prerelease' with the 'major' type to include pre-release versions. ")
                .WithExample(BumpCommandNames.Sdk)
                .WithExample(BumpCommandNames.Sdk, TypeOptionName, PatchBumpType)
                .WithExample(BumpCommandNames.Sdk, TypeOptionName, MajorBumpType)
                .WithExample(BumpCommandNames.Sdk, TypeOptionName, MajorBumpType, PreReleaseOptionName)
                .WithExample(BumpCommandNames.Sdk, "--file", "./other/global.json", OutputOptionName, "bump-sdk-report.json")
                .WithExample(
                    BumpCommandNames.Sdk,
                    "--security-only",
                    DebugOptionName,
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

            config.AddCommand<BumpToolsCommand>(name: BumpCommandNames.Tools)
                .WithDescription(
                    "Bump the local .NET tools versions. " +
                    "Use the 'minor' type option to bump the tools to the latest minor or patch versions for the current major version. " +
                    "Use the 'patch' type option to bump the tools to the latest patch version for the current minor version. " +
                    "Use the 'major' type option to bump the tools to the latest version, including versions with a higher major version. " +
                    "Use '--prerelease' with the 'major' type to include pre-release versions. ")
                .WithExample(BumpCommandNames.Tools)
                .WithExample(BumpCommandNames.Tools, TypeOptionName, PatchBumpType)
                .WithExample(BumpCommandNames.Tools, TypeOptionName, MajorBumpType)
                .WithExample(BumpCommandNames.Tools, TypeOptionName, MajorBumpType, PreReleaseOptionName)
                .WithExample(BumpCommandNames.Tools, TypeOptionName, MajorBumpType, "--skip-tool", "dotnet-*")
                .WithExample(BumpCommandNames.Tools, "--manifest", "./other/dotnet-tools.json")
                .WithExample(
                    BumpCommandNames.Tools,
                    "--config",
                    "./custom-nuget.config",
                    OutputOptionName,
                    "bump-tools-report.json")
                .WithExample(
                    BumpCommandNames.Tools,
                    DebugOptionName,
                    LogFileOptionName,
                    "bump-tools-log.txt");

            config.Settings.Registrar.Register<IPackageFileService, PackageFileService>();
            config.Settings.Registrar.Register<IBumpPackagesHandler, BumpPackagesHandler>();

            config.AddCommand<BumpPackagesCommand>(name: BumpCommandNames.Packages)
                .WithDescription(
                    "Bump the NuGet package versions. " +
                    "Use the 'minor' type option to bump the packages to the latest minor or patch versions for the current major version. " +
                    "Use the 'patch' type option to bump the packages to the latest patch version for the current minor version. " +
                    "Use the 'major' type option to bump the packages to the latest version, including versions with a higher major version. " +
                    "Use '--prerelease' with the 'major' type to include pre-release versions. ")
                .WithExample(BumpCommandNames.Packages)
                .WithExample(BumpCommandNames.Packages, TypeOptionName, PatchBumpType)
                .WithExample(BumpCommandNames.Packages, TypeOptionName, MajorBumpType)
                .WithExample(BumpCommandNames.Packages, TypeOptionName, MajorBumpType, PreReleaseOptionName)
                .WithExample(
                    BumpCommandNames.Packages,
                    TypeOptionName,
                    MajorBumpType,
                    PreReleaseOptionName,
                    "--skip-package",
                    "Microsoft.*")
                .WithExample(
                    BumpCommandNames.Packages,
                    "--path",
                    "./src",
                    "--config",
                    "./custom-nuget.config",
                    OutputOptionName,
                    "bump-packages-report.json")
                .WithExample(BumpCommandNames.Packages, "--exclude", "./test/DotBump.Tests/Data")
                .WithExample(
                    BumpCommandNames.Packages,
                    DebugOptionName,
                    LogFileOptionName,
                    "bump-packages-log.txt");

            config.Settings.Registrar.Register<IFrameworkFileService, FrameworkFileService>();
            config.Settings.Registrar.Register<IBumpFrameworksHandler, BumpFrameworksHandler>();

            config.AddCommand<BumpFrameworksCommand>(name: BumpCommandNames.Frameworks)
                .WithDescription(
                    "Bump the target framework versions. " +
                    "Aligns `<TargetFramework>` values to the SDK major version in global.json.")
                .WithExample(BumpCommandNames.Frameworks)
                .WithExample(BumpCommandNames.Frameworks, "--file", "./other/global.json", "--path", "./src")
                .WithExample(
                    BumpCommandNames.Frameworks,
                    "--exclude",
                    "./test",
                    OutputOptionName,
                    "bump-frameworks-report.md")
                .WithExample(
                    BumpCommandNames.Frameworks,
                    DebugOptionName,
                    LogFileOptionName,
                    "bump-frameworks-log.txt");

            config.Settings.Registrar.Register<ILanguageFileService, LanguageFileService>();
            config.Settings.Registrar.Register<IBumpLanguageHandler, BumpLanguageHandler>();

            config.AddCommand<BumpLanguageCommand>(name: BumpCommandNames.Language)
                .WithDescription(
                    "Bump the C# language version. " +
                    $"The language version is derived from the SDK major version in global.json as SDK major + {LanguageVersion.SdkMajorOffset}. " +
                    "Only explicit numeric LangVersion values in C# projects and shared MSBuild files are bumped.")
                .WithExample(BumpCommandNames.Language)
                .WithExample(BumpCommandNames.Language, "--file", "./other/global.json", "--path", "./src")
                .WithExample(BumpCommandNames.Language, OutputOptionName, "bump-language-report.json")
                .WithExample(
                    BumpCommandNames.Language,
                    DebugOptionName,
                    LogFileOptionName,
                    "bump-language-log.txt");
        });
    }
}
