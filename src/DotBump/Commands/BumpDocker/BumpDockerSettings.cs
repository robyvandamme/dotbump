// Copyright © Roby Van Damme.

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotBump.Commands.BumpDocker;

/// <summary>
/// The settings for the Bump Docker command.
/// </summary>
internal class BumpDockerSettings : BumpPathSettings
{
    [Description("The bump type. Defaults to `minor`. Available options are `minor`, `patch` and `major`.")]
    [CommandOption("-t|--type")]
    public BumpType? BumpType { get; init; }

    [Description(
        "Include pre-release SDK versions when bumping. Only applies to `major` bumps. Defaults to false.")]
    [CommandOption("--prerelease")]
    [DefaultValue(false)]
    public bool PreRelease { get; init; }

    public override ValidationResult Validate()
    {
        return ValidatePath() ?? ValidationResult.Success();
    }
}
