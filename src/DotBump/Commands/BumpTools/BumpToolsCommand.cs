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

            var bumpType = settings.Type ?? BumpType.Minor;
            var outputFile = settings.Output;

            logger.Debug("Bump type: {Type}", bumpType);
            logger.Debug("Output file : {OutputFile}", outputFile);

            console.MarkupLine($"Bumping Tools with settings: type={bumpType}, output: {outputFile ?? "none"}");

            var result = await bumpToolsHandler.HandleAsync(bumpType);

            if (result.Count == 0)
            {
                console.MarkupLine("No tool versions were bumped.");
            }

            foreach (var update in result)
            {
                console.MarkupLine(update.ToString());
            }

            if (!string.IsNullOrWhiteSpace(outputFile))
            {
                logger.Debug("Writing output to file {File}", outputFile);
                File.WriteAllText(outputFile, JsonSerializer.Serialize(result), new UTF8Encoding());
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
