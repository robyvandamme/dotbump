// Copyright © Roby Van Damme.

using DotBump.Commands.BumpPackages.Interfaces;
using DotBump.Common;
using DotBump.Reports;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotBump.Commands.BumpPackages;

internal class BumpPackagesCommand(
    IAnsiConsole console,
    ILogger logger,
    IBumpPackagesHandler bumpPackagesHandler)
    : AsyncCommand<BumpPackagesSettings>
{
    private readonly string _defaultNugetConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "nuget.config");
    private readonly string _defaultRepositoryPath = Directory.GetCurrentDirectory();

    internal Task<int> ExecuteForTestAsync(
        CommandContext context,
        BumpPackagesSettings settings,
        CancellationToken cancellationToken)
        => ExecuteAsync(context, settings, cancellationToken);

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        BumpPackagesSettings settings,
        CancellationToken cancellationToken)
    {
        logger.MethodStart(nameof(BumpPackagesCommand), nameof(ExecuteAsync));

        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);

        if (context.Name != "packages")
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
            var nugetConfigPath = !string.IsNullOrWhiteSpace(settings.NuGetConfigPath)
                ? Path.GetFullPath(settings.NuGetConfigPath)
                : _defaultNugetConfigPath;

            logger.Debug("Bump type: {Type}", bumpType);
            logger.Debug("Repository path : {RepositoryPath}", repositoryPath);
            logger.Debug("Output file : {OutputFile}", outputFile);
            logger.Debug("NuGet config : {NuGetConfig}", nugetConfigPath);

            console.MarkupLine(
                $"Bumping Packages with settings: type={bumpType}, path={repositoryPath}, output: {outputFile ?? "none"}, config: {nugetConfigPath}");

            var bumpReport = await bumpPackagesHandler.HandleAsync(bumpType, repositoryPath, nugetConfigPath);

            WriteReportToConsole(bumpReport);

            await bumpReport.WriteToFileAsync(outputFile);

            if (bumpReport.Errors.Any())
            {
                logger.MethodReturn(nameof(BumpPackagesCommand), nameof(ExecuteAsync));
                return 1;
            }
        }
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            logger.Error(e, "An error occured while trying to bump the packages");
            console.WriteException(e, ExceptionFormats.ShortenEverything);
            logger.MethodReturn(nameof(BumpPackagesCommand), nameof(ExecuteAsync));
            return 1;
        }

        logger.MethodReturn(nameof(BumpPackagesCommand), nameof(ExecuteAsync));
        return 0;
    }

    private void WriteReportToConsole(BumpReport bumpReport)
    {
        if (bumpReport.Errors.Any())
        {
            console.MarkupLine("An error occured bumping package versions.");
            foreach (var bumpReportError in bumpReport.Errors)
            {
                console.MarkupLine(bumpReportError);
            }
        }
        else
        {
            if (!bumpReport.HasChanges)
            {
                console.MarkupLine("No package versions were bumped.");
            }
            else
            {
                console.MarkupLine("Package versions bumped:");
                foreach (var bumpResult in bumpReport.Results)
                {
                    if (bumpResult.WasBumped)
                    {
                        console.MarkupLine(bumpResult.ToString());
                    }
                }
            }
        }
    }
}
