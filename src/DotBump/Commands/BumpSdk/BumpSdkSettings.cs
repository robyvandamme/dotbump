// Copyright © 2025 Roby Van Damme.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace DotBump.Commands.BumpSdk;

/// <summary>
/// The settings for the Bump SDK command.
/// </summary>
internal class BumpSdkSettings : BumpSettings
{
    [Description(
        "The bump type. Defaults to `minor`. The option is ignored for now (only the minor option is implemented.")]
    [CommandOption("-t|--type")]
    internal BumpType? Type { get; init; }

    [Description("The global.json file to update. Defaults to `./global.json`.")]
    [CommandOption("-f|--file")]
    internal string? GlobalJsonPath { get; init; }

    [Description("Output file name. The name of the file to write the result to. The output format is json.")]
    [CommandOption("-o|--output")]
    internal string? Output { get; init; }
}
