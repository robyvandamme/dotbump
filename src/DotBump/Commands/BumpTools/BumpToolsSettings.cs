// Copyright © Roby Van Damme.

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotBump.Commands.BumpTools;

/// <summary>
/// The settings for the Bump Tools command.
/// </summary>
internal class BumpToolsSettings : BumpSettings
{
    [Description("The bump type. Defaults to `minor`. Available options are `minor`, `patch` and `major`.")]
    [CommandOption("-t|--type")]
    public BumpType? BumpType { get; init; }

    [Description(OutputOptionDescription)]
    [CommandOption("-o|--output")]
    public string? Output { get; init; }

    [Description("The nuget config file to use. Defaults to `./nuget.config`.")]
    [CommandOption("-c|--config")]
    public string? NuGetConfigPath { get; init; }

    [Description(
        "The tools manifest file to update. Defaults to `./dotnet-tools.json`, " +
        "falling back to `./.config/dotnet-tools.json`.")]
    [CommandOption("-m|--manifest")]
    public string? ToolManifestPath { get; init; }

    [Description(
        "A tool id or pattern (`*` and `?` supported) to skip when bumping. Can be specified multiple times or " +
        "as a comma-separated list.")]
    [CommandOption("--skip-tool")]
    public string[]? SkipTools { get; init; }

    [Description(
        "Include pre-release versions when bumping. Only applies to `major` bumps. Defaults to false.")]
    [CommandOption("--prerelease")]
    [DefaultValue(false)]
    public bool PreRelease { get; init; }

    public override ValidationResult Validate()
    {
        return ValidateFileExists(NuGetConfigPath)
            ?? ValidateFileExists(ToolManifestPath)
            ?? ValidateSkipPatterns(SkipTools)
            ?? ValidationResult.Success();
    }
}
