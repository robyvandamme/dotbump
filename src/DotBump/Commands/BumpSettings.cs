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
    /// Gets or sets the debug flag.
    /// Note that this particular Spectre command setting is not required since the argument gets picked up by the
    /// <see cref="ArgumentHandler"/> when the application is starting up but is useful for documentation purposes.
    /// The reason this is handled outside of Spectre is to be able to hande the `--debug` argument anywhere in the
    /// argument list which seemed to not be possible with the default Spectre approach and to pick it up as early as
    /// possible to configure logging.
    /// </summary>
    [Description("Enable debug logging for troubleshooting")]
    [CommandOption("--debug")]
    [DefaultValue(false)]
    public bool? Debug { get; set; }
}
