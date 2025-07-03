// Copyright © 2025 Roby Van Damme.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace DotBump.Commands;

/// <summary>
/// Base class for DotBump command settings.
/// </summary>
public abstract class BumpSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets the debug flag.
    /// Note that this particular Spectre command setting is not required since the argument gets picked up by the
    /// <see cref="ArgumentHandler"/> when the application is starting up.
    /// </summary>
    [Description("Enable debug logging for troubleshooting")]
    [CommandOption("--debug")]
    [DefaultValue(false)]
    public bool? Debug { get; set; }
}
