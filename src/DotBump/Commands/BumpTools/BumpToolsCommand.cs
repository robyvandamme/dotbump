// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpTools.Interfaces;
using DotBump.Common;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotBump.Commands.BumpTools;

internal class BumpToolsCommand(
    IAnsiConsole console,
    ILogger logger,
    IToolFileService toolFileService)
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

            var manifest = toolFileService.GetToolManifest();

            console.MarkupLine(">> Local Tools:");
            for (var i = 0; i < manifest.Tools.Count; i++)
            {
                var tool = manifest.Tools.ElementAt(i);
                console.MarkupLine(tool.Key);
            }

            var nugetPackageSources = toolFileService.GetNuGetPackageSources();

            console.MarkupLine(">> NuGet Package Sources:");
            foreach (var packageSource in nugetPackageSources)
            {
                console.MarkupLine(packageSource);
            }

            var bumpToolResults = new List<BumpToolResult>();

            if (bumpToolResults.Count == 0)
            {
                logger.Debug("No tool versions were bumped");
                console.MarkupLine("No tool versions were bumped.");
            }

            foreach (var update in bumpToolResults)
            {
                console.MarkupLine(update.ToString());
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
