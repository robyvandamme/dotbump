// Copyright © Roby Van Damme.

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotBump.Commands;

/// <summary>
/// Base settings for commands that scan a directory and write an optional report.
/// </summary>
internal abstract class BumpPathSettings : BumpSettings
{
    [Description("The root directory to scan. Defaults to the current directory.")]
    [CommandOption("-p|--path")]
    public string? RepositoryPath { get; init; }

    [Description("A directory to exclude from the scan. Can be specified multiple times.")]
    [CommandOption("-e|--exclude")]
    public string[]? Exclude { get; init; }

    [Description(OutputOptionDescription)]
    [CommandOption("-o|--output")]
    public string? Output { get; init; }

    /// <summary>
    /// Validates the repository path and excluded directories.
    /// </summary>
    /// <returns>A <see cref="ValidationResult"/> describing the failure, or <see langword="null"/> when valid.</returns>
    protected ValidationResult? ValidatePath()
    {
        return ValidateDirectoryExists(RepositoryPath)
            ?? ValidateExcludedDirectories(Exclude);
    }

    private static ValidationResult? ValidateExcludedDirectories(IEnumerable<string>? excludedPaths)
    {
        return (excludedPaths ?? [])
            .Select(ValidateDirectoryExists)
            .FirstOrDefault(result => result != null);
    }
}
