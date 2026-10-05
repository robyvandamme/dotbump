// Copyright © Roby Van Damme.

using System.ComponentModel;
using DotBump.Common;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotBump.Commands.BumpTools;

/// <summary>
/// The settings for the Bump Tools command.
/// </summary>
internal class BumpToolsSettings : BumpSettings
{
    [Description("The bump type. Defaults to `minor`. Available options are `minor` and `patch`.")]
    [CommandOption("-t|--type")]
    public BumpType? BumpType { get; init; }

    [Description("Output file name. The name of the file to write the result to. The output format is inferred from the file extension: `.json` or `.md`.")]
    [CommandOption("-o|--output")]
    public string? Output { get; init; }

    [Description("The nuget config file to use. Defaults to `./nuget.config`.")]
    [CommandOption("-c|--config")]
    public string? NuGetConfigPath { get; init; }

    [Description("The tools manifest file to update. Defaults to `./dotnet-tools.json`.")]
    [CommandOption("-m|--manifest")]
    public string? ToolManifestPath { get; init; }

    public override ValidationResult Validate()
    {
        // If a config file is passed, verify it exists before passing it on.
        if (!string.IsNullOrWhiteSpace(NuGetConfigPath))
        {
            if (!PathValidation.TryGetFullPath(NuGetConfigPath, out var normalizedConfigPath))
            {
                return ValidationResult.Error($"The file {NuGetConfigPath} is not a valid path.");
            }

            if (!File.Exists(normalizedConfigPath))
            {
                return ValidationResult.Error($"The file {NuGetConfigPath} does not exist.");
            }
        }

        // If a manifest file is passed, verify it exists before passing it on.
        if (!string.IsNullOrWhiteSpace(ToolManifestPath))
        {
            if (!PathValidation.TryGetFullPath(ToolManifestPath, out var normalizedManifestPath))
            {
                return ValidationResult.Error($"The file {ToolManifestPath} is not a valid path.");
            }

            if (!File.Exists(normalizedManifestPath))
            {
                return ValidationResult.Error($"The file {ToolManifestPath} does not exist.");
            }
        }

        return ValidationResult.Success();
    }
}
