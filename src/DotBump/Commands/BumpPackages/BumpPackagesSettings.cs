// Copyright © Roby Van Damme.

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotBump.Commands.BumpPackages;

/// <summary>
/// The settings for the Bump Packages command.
/// </summary>
internal class BumpPackagesSettings : BumpPathSettings
{
    [Description("The bump type. Defaults to `minor`. Available options are `minor`, `patch` and `major`.")]
    [CommandOption("-t|--type")]
    public BumpType? BumpType { get; init; }

    [Description("The nuget config file to use. Defaults to `./nuget.config`.")]
    [CommandOption("-c|--config")]
    public string? NuGetConfigPath { get; init; }

    [Description(
        "A package id or pattern (`*` and `?` supported) to skip when bumping. Can be specified multiple times or " +
        "as a comma-separated list.")]
    [CommandOption("--skip-package")]
    public string[]? SkipPackages { get; init; }

    [Description(
        "Include pre-release versions when bumping. Only applies to `major` bumps. Defaults to false.")]
    [CommandOption("--prerelease")]
    [DefaultValue(false)]
    public bool PreRelease { get; init; }

    public override ValidationResult Validate()
    {
        return ValidateFileExists(NuGetConfigPath)
            ?? ValidatePath()
            ?? ValidateSkipPatterns(SkipPackages)
            ?? ValidationResult.Success();
    }
}
