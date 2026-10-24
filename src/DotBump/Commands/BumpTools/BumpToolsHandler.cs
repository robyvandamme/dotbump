// Copyright © Roby Van Damme.

using DotBump.Commands.BumpTools.Interfaces;
using DotBump.Common;
using DotBump.NuGet.DataModel.PackageResolution;
using DotBump.NuGet.Interfaces;
using DotBump.Reports;
using Serilog;

namespace DotBump.Commands.BumpTools;

internal class BumpToolsHandler(
    IToolFileService toolFileService,
    INuGetConfigFileService nugetConfigFileService,
    IPackageVersionResolver packageVersionResolver,
    INuGetConfigValidator nugetConfigValidator,
    ILogger logger) : IBumpToolsHandler
{
    public async Task<BumpReport> HandleAsync(
        BumpType bumpType,
        string nugetConfigPath,
        string toolManifestPath,
        bool preRelease = false,
        IReadOnlyCollection<string>? skipTools = null)
    {
        logger.MethodStart(nameof(BumpToolsHandler), nameof(HandleAsync), bumpType);

        var manifest = toolFileService.GetToolsManifest(toolManifestPath);
        var bumpReport = new BumpReport(manifest, bumpType);

        var nuGetConfiguration = nugetConfigFileService.GetNuGetConfiguration(nugetConfigPath);
        var validationErrors = nugetConfigValidator.Validate(nuGetConfiguration);
        if (validationErrors.Any())
        {
            bumpReport.ReportErrors(validationErrors);
            logger.MethodReturn(nameof(BumpToolsHandler), nameof(HandleAsync), bumpReport);
            return bumpReport;
        }

        var skipMatcher = NamePatternMatcher.Create(skipTools);
        var skipWarnings = new List<string>();
        var packagesToBump = new List<PackageToBump>();

        foreach (var tool in manifest.Tools)
        {
            if (skipMatcher.TryMatch(tool.Key, out var matchedPattern))
            {
                var skipWarning =
                    $"Skipping tool '{tool.Key}' because it matches --skip-tool pattern '{matchedPattern}'.";
                logger.Warning("{Warning}", skipWarning);
                skipWarnings.Add(skipWarning);
                continue;
            }

            packagesToBump.Add(new PackageToBump(tool.Key, tool.Value.SemanticVersion));
        }

        var bestVersions = await packageVersionResolver
            .ResolveAsync(packagesToBump, bumpType, nuGetConfiguration, preRelease)
            .ConfigureAwait(false);

        foreach (var (toolKey, newVersion) in bestVersions)
        {
            manifest.Tools[toolKey].Version = newVersion.ToString();
        }

        bumpReport.ReportChanges(manifest);
        bumpReport.ReportWarnings(skipWarnings);

        if (bumpReport.HasChanges)
        {
            toolFileService.SaveToolsManifest(manifest, toolManifestPath);
        }

        logger.MethodReturn(nameof(BumpToolsHandler), nameof(HandleAsync), bumpReport);

        return bumpReport;
    }
}
