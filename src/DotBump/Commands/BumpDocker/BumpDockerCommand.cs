// Copyright © Roby Van Damme.

using DotBump.Commands.BumpDocker.Interfaces;
using DotBump.Common;
using DotBump.Reports;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotBump.Commands.BumpDocker;

internal class BumpDockerCommand(
    IAnsiConsole console,
    ILogger logger,
    IBumpDockerHandler bumpDockerHandler)
    : AsyncCommand<BumpDockerSettings>
{
    private const string NoChangesMessage = "No Docker image tags were bumped.";
    private const string ChangesMessage = "Docker image tags bumped:";
    private const string ErrorMessage = "An error occurred bumping Docker image tags.";

    private readonly string _defaultRepositoryPath = Directory.GetCurrentDirectory();

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        BumpDockerSettings settings,
        CancellationToken cancellationToken)
    {
        logger.MethodStart(nameof(BumpDockerCommand), nameof(ExecuteAsync));

        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);

        if (context.Name != BumpCommandNames.Docker)
        {
            throw new DotBumpException($"Unsupported command name {context.Name}");
        }

        try
        {
            var bumpType = settings.BumpType ?? BumpType.Minor;
            var outputFile = settings.Output;
            var repositoryPath = !string.IsNullOrWhiteSpace(settings.RepositoryPath)
                ? Path.GetFullPath(settings.RepositoryPath)
                : _defaultRepositoryPath;
            var excludedPaths = settings.Exclude?
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(Path.GetFullPath)
                .ToArray();
            var preRelease = settings.PreRelease;

            logger.Debug("Bump type: {Type}", bumpType);
            logger.Debug("Repository path : {RepositoryPath}", repositoryPath);
            logger.Debug("Output file : {OutputFile}", outputFile);
            logger.Debug("Excluded paths : {ExcludedPaths}", excludedPaths ?? []);
            logger.Debug("Include pre-release versions: {PreRelease}", preRelease);

            console.MarkupLine(
                $"Bumping Docker image tags with settings: type={bumpType}, path={Markup.Escape(repositoryPath)}, output: {Markup.Escape(outputFile ?? "none")}, preRelease: {preRelease}");

            var bumpReport = await bumpDockerHandler
                .HandleAsync(bumpType, repositoryPath, excludedPaths, preRelease)
                .ConfigureAwait(false);

            WriteReportToConsole(bumpReport);

            await bumpReport.WriteToFileAsync(outputFile).ConfigureAwait(false);

            if (bumpReport.Errors.Any())
            {
                logger.MethodReturn(nameof(BumpDockerCommand), nameof(ExecuteAsync));
                return 1;
            }
        }
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            logger.Error(e, "An error occurred while trying to bump the Docker image tags");
            console.WriteException(e, ExceptionFormats.ShortenEverything);
            logger.MethodReturn(nameof(BumpDockerCommand), nameof(ExecuteAsync));
            return 1;
        }

        logger.MethodReturn(nameof(BumpDockerCommand), nameof(ExecuteAsync));
        return 0;
    }

    private void WriteReportToConsole(BumpReport bumpReport)
    {
        if (bumpReport.Errors.Any())
        {
            console.MarkupLine(ErrorMessage);
            foreach (var bumpReportError in bumpReport.Errors)
            {
                console.MarkupLine(Markup.Escape(bumpReportError));
            }

            return;
        }

        if (!bumpReport.HasChanges)
        {
            console.MarkupLine(NoChangesMessage);
            return;
        }

        console.MarkupLine(ChangesMessage);
        foreach (var bumpResult in bumpReport.Results)
        {
            if (bumpResult.WasBumped)
            {
                console.MarkupLine(Markup.Escape(bumpResult.ToString()));
            }
        }
    }
}
