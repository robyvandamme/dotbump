// Copyright © Roby Van Damme.

using System.ComponentModel;
using DotBump.Common;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotBump.Commands;

/// <summary>
/// Base class for DotBump command settings.
/// </summary>
internal abstract class BumpSettings : CommandSettings
{
    /// <summary>
    /// The shared description for the <c>--output</c> option, used by all bump commands.
    /// </summary>
    internal const string OutputOptionDescription =
        "Output file name. The name of the file to write the result to. " +
        "The output format is inferred from the file extension: `.json` or `.md`.";

    /// <summary>
    /// Gets or sets a value indicating whether debug logging is enabled.
    /// Note that this particular Spectre command setting is required primarily for documentation purposes and example
    /// validation. The argument is handled by the <see cref="ArgumentHandler"/> when the application is starting up .
    /// The reason this is handled outside of Spectre is to be able to hande the `--debug` argument anywhere in the
    /// argument list which seemed to not be possible with the default Spectre approach and to pick it up as early as
    /// possible to configure logging.
    /// </summary>
    [Description("Enable debug logging for troubleshooting. Includes response data.")]
    [CommandOption("--debug")]
    [DefaultValue(false)]
    public bool Debug { get; set; }

    /// <summary>
    /// Gets or sets the file to send the log output to.
    /// Note that this particular Spectre command setting is required primarily for documentation purposes and example
    /// validation. The argument is handled by the <see cref="ArgumentHandler"/> when the application is starting up .
    /// The reason this is handled outside of Spectre is to be able to hande the `--logfile` argument anywhere in the
    /// argument list which seemed to not be possible with the default Spectre approach and to pick it up as early as
    /// possible to configure logging.
    /// </summary>
    [Description("The file to send the log output to.")]
    [CommandOption("--logfile")]
    public string? LogFile { get; set; }

    /// <summary>
    /// Validates that the supplied file path, when provided, can be resolved and exists.
    /// </summary>
    /// <param name="path">The file path to validate.</param>
    /// <returns>A <see cref="ValidationResult"/> describing the failure, or <see langword="null"/> when the path is valid.</returns>
    protected static ValidationResult? ValidateFileExists(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (!PathValidation.TryGetFullPath(path, out var normalizedPath))
        {
            return ValidationResult.Error($"The file {path} is not a valid path.");
        }

        if (!File.Exists(normalizedPath))
        {
            return ValidationResult.Error($"The file {path} does not exist.");
        }

        return null;
    }

    /// <summary>
    /// Validates that the supplied directory path, when provided, can be resolved and exists.
    /// </summary>
    /// <param name="path">The directory path to validate.</param>
    /// <returns>A <see cref="ValidationResult"/> describing the failure, or <see langword="null"/> when the path is valid.</returns>
    protected static ValidationResult? ValidateDirectoryExists(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (!PathValidation.TryGetFullPath(path, out var normalizedPath))
        {
            return ValidationResult.Error($"The directory {path} is not a valid path.");
        }

        if (!Directory.Exists(normalizedPath))
        {
            return ValidationResult.Error($"The directory {path} does not exist.");
        }

        return null;
    }
}
