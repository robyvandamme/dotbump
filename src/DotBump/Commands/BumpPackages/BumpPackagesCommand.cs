// Copyright © Roby Van Damme.

using DotBump.Common;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotBump.Commands.BumpPackages;

internal class BumpPackagesCommand(IAnsiConsole console, ILogger logger)
    : AsyncCommand<BumpPackagesSettings>
{
    private readonly string _defaultNugetConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "nuget.config");

    internal Task<int> ExecuteForTestAsync(
        CommandContext context,
        BumpPackagesSettings settings,
        CancellationToken cancellationToken)
        => ExecuteAsync(context, settings, cancellationToken);

    protected override Task<int> ExecuteAsync(
        CommandContext context,
        BumpPackagesSettings settings,
        CancellationToken cancellationToken)
    {
        logger.MethodStart(nameof(BumpPackagesCommand), nameof(ExecuteAsync));

        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);

        var bumpType = settings.BumpType ?? BumpType.Minor;
        var outputFile = settings.Output;
        var nugetConfigPath = !string.IsNullOrWhiteSpace(settings.NuGetConfigPath)
            ? Path.GetFullPath(settings.NuGetConfigPath)
            : _defaultNugetConfigPath;

        logger.Debug("Bump type: {Type}", bumpType);
        logger.Debug("Output file : {OutputFile}", outputFile);
        logger.Debug("NuGet config : {NuGetConfig}", nugetConfigPath);

        console.MarkupLine("Bumping packages is not implemented yet.");

        logger.MethodReturn(nameof(BumpPackagesCommand), nameof(ExecuteAsync));
        return Task.FromResult(0);
    }
}
