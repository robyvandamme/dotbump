// Copyright © 2025 Roby Van Damme.

using System.Text;
using System.Text.Json;
using DotBump.Commands.BumpTools.Interfaces;
using DotBump.Common;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotBump.Commands.BumpTools;

internal class BumpToolsCommand(
    IAnsiConsole console,
    ILogger logger,
    IBumpToolsHandler bumpToolsHandler)
    : AsyncCommand<BumpToolsSettings>
{
    private readonly string _defaultNugetConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "nuget.config");

    public override async Task<int> ExecuteAsync(CommandContext context, BumpToolsSettings settings)
    {
        logger.MethodStart(nameof(BumpToolsCommand), nameof(ExecuteAsync));

        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            if (context.Name != "tools")
            {
                throw new DotBumpException($"Unsupported command name {context.Name}");
            }

            var bumpType = settings.BumpType ?? BumpType.Minor;
            var outputFile = settings.Output;
            var nugetConfigPath = !string.IsNullOrWhiteSpace(settings.NuGetConfigPath)
                ? Path.GetFullPath(settings.NuGetConfigPath)
                : _defaultNugetConfigPath;

            logger.Debug("Output file : {OutputFile}", outputFile);

            console.MarkupLine($"Bumping Tools with settings: type={bumpType}, output: {outputFile ?? "none"}");

            var bumpReport = await bumpToolsHandler.HandleAsync(bumpType, nugetConfigPath);

            if (!bumpReport.HasChanges)
            {
                console.MarkupLine("No tool versions were bumped.");
            }
            else
            {
                console.MarkupLine("Tool versions bumped:");
                foreach (var bumpResult in bumpReport.Results)
                {
                    if (bumpResult.WasBumped)
                    {
                        console.MarkupLine(bumpResult.ToString());
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(outputFile))
            {
                logger.Debug("Writing output to file {File}", outputFile);
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                };
                File.WriteAllText(outputFile, JsonSerializer.Serialize(bumpReport, options), new UTF8Encoding());
            }
        }
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            logger.Error(e, "An error occured while trying to bump the tools");
            console.WriteException(e, ExceptionFormats.ShortenEverything);
            logger.MethodReturn(nameof(BumpToolsCommand), nameof(ExecuteAsync));
            return 1;
        }

        logger.MethodReturn(nameof(BumpToolsCommand), nameof(ExecuteAsync));
        return 0;
    }
}
