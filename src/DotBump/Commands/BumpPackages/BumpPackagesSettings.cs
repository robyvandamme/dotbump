// Copyright © Roby Van Damme.

using System.ComponentModel;
using DotBump.Common;
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

    [Description("Output file name. The name of the file to write the result to. The output format is inferred from the file extension: `.json` or `.md`.")]
    [CommandOption("-o|--output")]
    public string? Output { get; init; }

    [Description("The nuget config file to use. Defaults to `./nuget.config`.")]
    [CommandOption("-c|--config")]
    public string? NuGetConfigPath { get; init; }

    [Description("The root directory to scan. Defaults to the current directory.")]
    [CommandOption("-p|--path")]
    public string? RepositoryPath { get; init; }

    public override ValidationResult Validate()
    {
        // If a config file is passed, verify it exists before passing it on.
        if (!string.IsNullOrWhiteSpace(NuGetConfigPath))
        {
            if (!PathValidation.TryGetFullPath(NuGetConfigPath, out var normalizedConfigPath))
            {
                return ValidationResult.Error($"The file {NuGetConfigPath} is not a valid path.");
            }

            if (!File.Exists(normalizedConfigPath))
            {
                return ValidationResult.Error($"The file {NuGetConfigPath} does not exist.");
            }
        }

        // If a root path is passed, verify the directory exists before passing it on.
        if (!string.IsNullOrWhiteSpace(RepositoryPath))
        {
            if (!PathValidation.TryGetFullPath(RepositoryPath, out var normalizedRepositoryPath))
            {
                return ValidationResult.Error($"The directory {RepositoryPath} is not a valid path.");
            }

            if (!Directory.Exists(normalizedRepositoryPath))
            {
                return ValidationResult.Error($"The directory {RepositoryPath} does not exist.");
            }
        }

        return ValidationResult.Success();
    }
}
