// Copyright © Roby Van Damme.

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotBump.Commands;

/// <summary>
/// Base settings for commands that align file properties to the SDK version in global.json.
/// </summary>
internal abstract class SdkAlignmentSettings : BumpPathSettings
{
    [Description("The global.json file used to determine the target versions. Defaults to `./global.json`.")]
    [CommandOption("-f|--file")]
    public string? GlobalJsonPath { get; init; }

    public override ValidationResult Validate()
    {
        return ValidateFileExists(GlobalJsonPath)
            ?? ValidatePath()
            ?? ValidationResult.Success();
    }
}
