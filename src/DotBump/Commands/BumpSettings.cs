// Copyright © 2025 Roby Van Damme.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace DotBump.Commands;

/// <summary>
/// Base class for DotBump command settings.
/// </summary>
internal abstract class BumpSettings : CommandSettings
{
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
}
