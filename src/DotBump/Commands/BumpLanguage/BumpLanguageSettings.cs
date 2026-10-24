// Copyright © Roby Van Damme.

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotBump.Commands.BumpLanguage;

/// <summary>
/// The settings for the Bump Language command.
/// </summary>
internal class BumpLanguageSettings : BumpSettings
{
    [Description("The global.json file used to determine the C# language version. Defaults to `./global.json`.")]
    [CommandOption("-f|--file")]
    public string? GlobalJsonPath { get; init; }

    [Description("The root directory to scan. Defaults to the current directory.")]
    [CommandOption("-p|--path")]
    public string? RepositoryPath { get; init; }

    [Description("A directory to exclude from the scan. Can be specified multiple times.")]
    [CommandOption("-e|--exclude")]
    public string[]? Exclude { get; init; }

    [Description(OutputOptionDescription)]
    [CommandOption("-o|--output")]
    public string? Output { get; init; }

    public override ValidationResult Validate()
    {
        return ValidateFileExists(GlobalJsonPath)
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
