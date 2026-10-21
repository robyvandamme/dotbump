// Copyright © Roby Van Damme.

using DotBump.Commands.BumpFrameworks.Interfaces;
using DotBump.Common;
using DotBump.Reports;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotBump.Commands.BumpFrameworks;

internal class BumpFrameworksCommand(
    IAnsiConsole console,
    ILogger logger,
    IBumpFrameworksHandler bumpFrameworksHandler)
    : AsyncCommand<BumpFrameworksSettings>
{
    private readonly string _defaultRepositoryPath = Directory.GetCurrentDirectory();

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        BumpFrameworksSettings settings,
        CancellationToken cancellationToken)
    {
        logger.MethodStart(nameof(BumpFrameworksCommand), nameof(ExecuteAsync));

        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);

        if (context.Name != "frameworks")
        {
            throw new DotBumpException($"Unsupported command name {context.Name}");
        }

        try
        {
            var globalJsonPath = settings.GlobalJsonPath ?? "./global.json";
            var outputFile = settings.Output;
            var repositoryPath = !string.IsNullOrWhiteSpace(settings.RepositoryPath)
                ? Path.GetFullPath(settings.RepositoryPath)
                : _defaultRepositoryPath;
            var excludedPaths = settings.Exclude?
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(Path.GetFullPath)
                .ToArray();

            logger.Debug("GlobalJson file path : {GlobalJson}", globalJsonPath);
            logger.Debug("Repository path : {RepositoryPath}", repositoryPath);
            logger.Debug("Output file : {OutputFile}", outputFile);
            logger.Debug("Excluded paths : {ExcludedPaths}", excludedPaths ?? []);

            console.MarkupLine(
                $"Bumping Frameworks with settings: file={Markup.Escape(globalJsonPath)}, " +
                $"path={Markup.Escape(repositoryPath)}, output: {Markup.Escape(outputFile ?? "none")}");

            var bumpReport = await bumpFrameworksHandler
                .HandleAsync(repositoryPath, globalJsonPath, excludedPaths)
                .ConfigureAwait(false);

            WriteReportToConsole(bumpReport);

            await bumpReport.WriteToFileAsync(outputFile);

            if (bumpReport.Errors.Any())
            {
                logger.MethodReturn(nameof(BumpFrameworksCommand), nameof(ExecuteAsync));
                return 1;
            }
        }
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            logger.Error(e, "An error occurred while trying to bump the frameworks");
            console.WriteException(e, ExceptionFormats.ShortenEverything);
            logger.MethodReturn(nameof(BumpFrameworksCommand), nameof(ExecuteAsync));
            return 1;
        }

        logger.MethodReturn(nameof(BumpFrameworksCommand), nameof(ExecuteAsync));
        return 0;
    }

    private void WriteReportToConsole(BumpReport bumpReport)
    {
        if (bumpReport.Errors.Any())
        {
            console.MarkupLine("An error occurred bumping target framework versions.");
            foreach (var bumpReportError in bumpReport.Errors)
            {
                console.MarkupLine(Markup.Escape(bumpReportError));
            }
        }
        else
        {
            if (!bumpReport.HasChanges)
            {
                console.MarkupLine("No target frameworks were bumped.");
            }
            else
            {
                console.MarkupLine("Target frameworks bumped:");
                foreach (var bumpResult in bumpReport.Results)
                {
                    if (bumpResult.WasBumped)
                    {
                        console.MarkupLine(Markup.Escape(bumpResult.ToString()));
                    }
                }
            }
        }
    }
}
