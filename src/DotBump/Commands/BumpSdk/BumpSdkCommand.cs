// Copyright © Roby Van Damme.

using DotBump.Commands.BumpSdk.Interfaces;
using DotBump.Common;
using DotBump.Reports;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotBump.Commands.BumpSdk;

internal class BumpSdkCommand(IAnsiConsole console, ILogger logger, IBumpSdkHandler bumpSdkHandler)
    : AsyncCommand<BumpSdkSettings>()
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        BumpSdkSettings settings,
        CancellationToken cancellationToken)
    {
        logger.MethodStart(nameof(BumpSdkCommand), nameof(ExecuteAsync));

        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            if (context.Name != "sdk")
            {
                throw new DotBumpException($"Unsupported command name {context.Name}");
            }

            var bumpType = settings.Type ?? BumpType.Minor;
            var globalJsonPath = settings.GlobalJsonPath ?? "./global.json";
            var outputFile = settings.Output;
            var securityOnly = settings.SecurityOnly;
            var preRelease = settings.PreRelease;

            logger.Debug("Bump type: {Type}", bumpType);
            logger.Debug("GlobalJson file path: {Path}", globalJsonPath);
            logger.Debug("Output file : {OutputFile}", outputFile);
            logger.Debug("Security updates only: {Security}", securityOnly);
            logger.Debug("Include pre-release versions: {PreRelease}", preRelease);

            console.MarkupLine(
                $"Bumping SDK with settings: type={bumpType}, file={Markup.Escape(globalJsonPath)}, output: {Markup.Escape(outputFile ?? "none")}, securityOnly: {securityOnly}, preRelease: {preRelease}");

            var bumpReport = await bumpSdkHandler.HandleAsync(bumpType, globalJsonPath, securityOnly, preRelease)
                .ConfigureAwait(false);

            WriteReportToConsole(bumpReport);

            await bumpReport.WriteToFileAsync(outputFile);

            if (bumpReport.Errors.Any())
            {
                logger.MethodReturn(nameof(BumpSdkCommand), nameof(ExecuteAsync));
                return 1;
            }
        }
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            logger.Error(e, "An error occurred while trying to bump the sdk");
            console.WriteException(e, ExceptionFormats.ShortenEverything);
            logger.MethodReturn(nameof(BumpSdkCommand), nameof(ExecuteAsync));
            return 1;
        }

        logger.MethodReturn(nameof(BumpSdkCommand), nameof(ExecuteAsync));
        return 0;
    }

    private void WriteReportToConsole(BumpReport bumpReport)
    {
        if (bumpReport.Errors.Any())
        {
            console.MarkupLine($"An error occurred bumping the SDK version.");
            foreach (var bumpReportError in bumpReport.Errors)
            {
                console.MarkupLine(Markup.Escape(bumpReportError));
            }
        }
        else
        {
            if (!bumpReport.HasChanges)
            {
                console.MarkupLine("SDK version not bumped.");
            }
            else
            {
                console.MarkupLine("SDK version bumped:");
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
