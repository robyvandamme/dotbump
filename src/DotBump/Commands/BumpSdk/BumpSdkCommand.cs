// Copyright © 2025 Roby Van Damme.

using System.Text;
using System.Text.Json;
using DotBump.Commands.BumpSdk.Interfaces;
using DotBump.Common;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotBump.Commands.BumpSdk;

internal class BumpSdkCommand(IAnsiConsole console, ILogger logger, IBumpSdkHandler bumpSdkHandler)
    : AsyncCommand<BumpSdkSettings>()
{
    public override async Task<int> ExecuteAsync(CommandContext context, BumpSdkSettings settings)
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

            logger.Debug("Bump type: {Type}", bumpType);
            logger.Debug("GlobalJson file path: {Path}", globalJsonPath);
            logger.Debug("Output file : {OutputFile}", outputFile);

            console.MarkupLine(
                $"Bumping SDK with settings: type={bumpType}, file={globalJsonPath}, output: {outputFile ?? "none"}");

            var result = await bumpSdkHandler.HandleAsync(bumpType, globalJsonPath).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(outputFile))
            {
                logger.Debug("Writing output to file {File}", outputFile);
                File.WriteAllText(outputFile, JsonSerializer.Serialize(result), new UTF8Encoding());
            }

            console.MarkupLine(result.ToString());
        }
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            logger.Error(e, "An error occured while trying to bump the sdk");
            console.WriteException(e, ExceptionFormats.ShortenEverything);
            logger.MethodReturn(nameof(BumpSdkCommand), nameof(ExecuteAsync));
            return 1;
        }

        logger.MethodReturn(nameof(BumpSdkCommand), nameof(ExecuteAsync));
        return 0;
    }
}
