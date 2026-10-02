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

    [Description("Output file name. The name of the file to write the result to. The output format is json.")]
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
            var normalizedPath = Path.GetFullPath(NuGetConfigPath);
            if (!File.Exists(normalizedPath))
            {
                return ValidationResult.Error($"The file {NuGetConfigPath} does not exist.");
            }
        }

        // If a root path is passed, verify the directory exists before passing it on.
        if (!string.IsNullOrWhiteSpace(RepositoryPath))
        {
            var normalizedPath = Path.GetFullPath(RepositoryPath);
            if (!Directory.Exists(normalizedPath))
            {
                return ValidationResult.Error($"The directory {RepositoryPath} does not exist.");
            }
        }

        return ValidationResult.Success();
    }
}
