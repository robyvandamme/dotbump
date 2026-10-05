// Copyright © Roby Van Damme.

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotBump.Commands.BumpPackages;

/// <summary>
/// The settings for the Bump Packages command.
/// </summary>
internal class BumpPackagesSettings : BumpSettings
{
    [Description("The bump type. Defaults to `minor`. Available options are `minor` and `patch`.")]
    [CommandOption("-t|--type")]
    public BumpType? BumpType { get; init; }

    [Description(OutputOptionDescription)]
    [CommandOption("-o|--output")]
    public string? Output { get; init; }

    [Description("The nuget config file to use. Defaults to `./nuget.config`.")]
    [CommandOption("-c|--config")]
    public string? NuGetConfigPath { get; init; }

    [Description("The root directory to scan. Defaults to the current directory.")]
    [CommandOption("-p|--path")]
    public string? RepositoryPath { get; init; }

    [Description("A directory to exclude from the scan. Can be specified multiple times.")]
    [CommandOption("-e|--exclude")]
    public string[]? Exclude { get; init; }

    public override ValidationResult Validate()
    {
        return ValidateFileExists(NuGetConfigPath)
            ?? ValidateDirectoryExists(RepositoryPath)
            ?? ValidateExcludedDirectories(Exclude)
            ?? ValidationResult.Success();
    }

    private static ValidationResult? ValidateExcludedDirectories(IEnumerable<string>? excludedPaths)
    {
        return (excludedPaths ?? [])
            .Select(ValidateDirectoryExists)
            .FirstOrDefault(result => result != null);
    }
}
