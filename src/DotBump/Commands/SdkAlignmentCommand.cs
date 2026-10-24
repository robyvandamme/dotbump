// Copyright © Roby Van Damme.

using DotBump.Common;
using DotBump.Reports;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotBump.Commands;

/// <summary>
/// Base command for commands that align file properties to the SDK version in global.json.
/// </summary>
internal abstract class SdkAlignmentCommand<TSettings>(
    IAnsiConsole console,
    ILogger logger,
    string commandName,
    string entityName)
    : AsyncCommand<TSettings>
    where TSettings : SdkAlignmentSettings
{
    private readonly string _defaultRepositoryPath = Directory.GetCurrentDirectory();

    public sealed override async Task<int> ExecuteAsync(
        CommandContext context,
        TSettings settings,
        CancellationToken cancellationToken)
    {
        logger.MethodStart(GetType().Name, nameof(ExecuteAsync));

        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);

        if (!string.Equals(context.Name, commandName, StringComparison.Ordinal))
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
                $"Bumping {entityName} with settings: file={Markup.Escape(globalJsonPath)}, " +
                $"path={Markup.Escape(repositoryPath)}, output: {Markup.Escape(outputFile ?? "none")}");

            var bumpReport = await HandleAsync(repositoryPath, globalJsonPath, excludedPaths)
                .ConfigureAwait(false);

            WriteReportToConsole(bumpReport);

            await bumpReport.WriteToFileAsync(outputFile);

            if (bumpReport.Errors.Any())
            {
                logger.MethodReturn(GetType().Name, nameof(ExecuteAsync));
                return 1;
            }
        }
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            logger.Error(e, "An error occurred while trying to bump the {EntityName}", entityName);
            console.WriteException(e, ExceptionFormats.ShortenEverything);
            logger.MethodReturn(GetType().Name, nameof(ExecuteAsync));
            return 1;
        }

        logger.MethodReturn(GetType().Name, nameof(ExecuteAsync));
        return 0;
    }

    protected abstract Task<BumpReport> HandleAsync(
        string repositoryPath,
        string globalJsonPath,
        string[]? excludedPaths);

    private static string Capitalize(string value)
    {
        return char.ToUpperInvariant(value[0]) + value[1..];
    }

    private void WriteReportToConsole(BumpReport bumpReport)
    {
        if (bumpReport.Errors.Any())
        {
            console.MarkupLine($"An error occurred bumping {entityName}.");
            foreach (var bumpReportError in bumpReport.Errors)
            {
                console.MarkupLine(Markup.Escape(bumpReportError));
            }
        }
        else
        {
            if (!bumpReport.HasChanges)
            {
                console.MarkupLine($"No {entityName} were bumped.");
            }
            else
            {
                console.MarkupLine($"{Capitalize(entityName)} bumped:");
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
