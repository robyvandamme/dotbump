// Copyright © Roby Van Damme.

using DotBump.Commands.BumpFrameworks.Interfaces;
using DotBump.Reports;
using Serilog;
using Spectre.Console;

namespace DotBump.Commands.BumpFrameworks;

internal sealed class BumpFrameworksCommand(
    IAnsiConsole console,
    ILogger logger,
    IBumpFrameworksHandler bumpFrameworksHandler)
    : SdkAlignmentCommand<BumpFrameworksSettings>(
        console,
        logger,
        BumpCommandNames.Frameworks,
        "target frameworks")
{
    protected override Task<BumpReport> HandleAsync(
        string repositoryPath,
        string globalJsonPath,
        string[]? excludedPaths)
    {
        return bumpFrameworksHandler.HandleAsync(repositoryPath, globalJsonPath, excludedPaths);
    }
}
