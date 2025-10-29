// Copyright © 2025 Roby Van Damme.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace DotBump.Commands.BumpTools;

/// <summary>
/// The settings for the Bump Tools command.
/// </summary>
internal class BumpToolsSettings : BumpSettings
{
    [Description("The bump type. Defaults to `minor`. Available options are `minor` and `patch`.")]
    [CommandOption("-t|--type")]
    public BumpType? Type { get; init; }

    [Description("Output file name. The name of the file to write the result to. The output format is json.")]
    [CommandOption("-o|--output")]
    public string? Output { get; init; }
}
