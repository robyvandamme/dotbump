// Copyright © Roby Van Damme.

using DotBump.Commands.BumpLanguage.Interfaces;
using DotBump.Reports;
using Serilog;
using Spectre.Console;

namespace DotBump.Commands.BumpLanguage;

internal sealed class BumpLanguageCommand(
    IAnsiConsole console,
    ILogger logger,
    IBumpLanguageHandler bumpLanguageHandler)
    : SdkAlignmentCommand<BumpLanguageSettings>(
        console,
        logger,
        "language",
        "language versions")
{
    protected override Task<BumpReport> HandleAsync(
        string repositoryPath,
        string globalJsonPath,
        string[]? excludedPaths)
    {
        return bumpLanguageHandler.HandleAsync(repositoryPath, globalJsonPath, excludedPaths);
    }
}
