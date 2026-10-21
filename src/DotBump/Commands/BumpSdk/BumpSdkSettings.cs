// Copyright © Roby Van Damme.

using System.ComponentModel;
using Spectre.Console;
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

    [Description(OutputOptionDescription)]
    [CommandOption("-o|--output")]
    public string? Output { get; init; }

    [Description("Only bump the version if the new release is a security release. Defaults to false.")]
    [CommandOption("-s|--security-only")]
    [DefaultValue(false)]
    public bool SecurityOnly { get; init; }

    public override ValidationResult Validate()
    {
        if (Type == BumpType.Major)
        {
            return ValidationResult.Error("The 'major' bump type is not supported for the sdk command.");
        }

        return ValidationResult.Success();
    }
}
