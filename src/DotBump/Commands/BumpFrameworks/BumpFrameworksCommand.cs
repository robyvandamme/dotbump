// Copyright © Roby Van Damme.

using DotBump.Common;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotBump.Commands.BumpFrameworks;

internal class BumpFrameworksCommand(IAnsiConsole console, ILogger logger)
    : AsyncCommand<BumpFrameworksSettings>
{
    public override Task<int> ExecuteAsync(
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

        console.MarkupLine("The frameworks command is not implemented yet.");

        logger.MethodReturn(nameof(BumpFrameworksCommand), nameof(ExecuteAsync));
        return Task.FromResult(1);
    }
}
