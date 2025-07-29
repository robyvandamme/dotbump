// Copyright © 2025 Roby Van Damme.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace DotBump.Commands.BumpSdk;

/// <summary>
/// The settings for the Bump SDK command.
/// </summary>
internal class BumpSdkSettings : BumpSettings
{
    [Description("The bump type. Defaults to `minor`. Available options are `minor` and `patch`.")]
    [CommandOption("-t|--type")]
    public BumpType? Type { get; init; }

    [Description("The global.json file to update. Defaults to `./global.json`.")]
    [CommandOption("-f|--file")]
    public string? GlobalJsonPath { get; init; }

    [Description("Output file name. The name of the file to write the result to. The output format is json.")]
    [CommandOption("-o|--output")]
    public string? Output { get; init; }

    [Description("Only bump the version if the new release is a security release. Defaults to false.")]
    [CommandOption("-s|--security")]
    [DefaultValue(false)]
    public bool Security { get; init; }
}
