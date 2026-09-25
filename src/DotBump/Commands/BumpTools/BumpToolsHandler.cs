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
    public async Task<BumpReport> HandleAsync(BumpType bumpType, string nugetConfigPath)
    {
        logger.MethodStart(nameof(BumpToolsHandler), nameof(HandleAsync), bumpType);

        var manifest = toolFileService.GetToolsManifest();
        var bumpReport = new BumpReport(manifest, bumpType);

        var nuGetConfiguration = nugetConfigFileService.GetNuGetConfiguration(nugetConfigPath);
        var validationErrors = nugetConfigValidator.Validate(nuGetConfiguration);
        if (validationErrors.Any())
        {
            bumpReport.ReportErrors(validationErrors);
            logger.MethodReturn(nameof(BumpToolsHandler), nameof(HandleAsync), bumpReport);
            return bumpReport;
        }

        var packagesToBump = manifest.Tools
            .Select(tool => new PackageToBump(tool.Key, tool.Value.SemanticVersion))
            .ToList();

        var bestVersions = await packageVersionResolver
            .ResolveAsync(packagesToBump, bumpType, nuGetConfiguration)
            .ConfigureAwait(false);

        foreach (var (toolKey, newVersion) in bestVersions)
        {
            manifest.Tools[toolKey].Version = newVersion.ToString();
        }

        bumpReport.ReportChanges(manifest);

        if (bumpReport.HasChanges)
        {
            toolFileService.SaveToolsManifest(manifest);
        }

        logger.MethodReturn(nameof(BumpToolsHandler), nameof(HandleAsync), bumpReport);

        return bumpReport;
    }
}
