// Copyright © Roby Van Damme.

using DotBump.Commands.BumpTools.Interfaces;
using DotBump.Common;
using DotBump.NuGet.DataModel.NuGetClientConfiguration;
using DotBump.NuGet.Interfaces;
using DotBump.Reports;
using Serilog;

namespace DotBump.Commands.BumpTools;

internal class BumpToolsHandler(
    IToolFileService toolFileService,
    INuGetConfigFileService nugetConfigFileService,
    INuGetClientFactory nuGetClientFactory,
    INuGetReleaseFinder nuGetReleaseFinder,
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

        var initialVersions = manifest.Tools.ToDictionary(
            t => t.Key,
            t => t.Value.SemanticVersion);

        var bestVersions = new Dictionary<string, SemanticVersion>();

        foreach (var nugetPackageSource in nuGetConfiguration.PackageSources)
        {
            var clientConfig = new NuGetClientConfig(nugetPackageSource.Key, nuGetConfiguration, logger);

            using var nuGetClient = nuGetClientFactory.CreateNuGetClient(clientConfig);
            var index = await nuGetClient.GetServiceIndexAsync(clientConfig.Url).ConfigureAwait(false);
            var baseUrl = nuGetReleaseFinder.GetRegistrationsBaseUrl(index);

            for (var i = 0; i < manifest.Tools.Count; i++)
            {
                var tool = manifest.Tools.ElementAt(i);
                var initialVersion = initialVersions[tool.Key];
                bestVersions.TryGetValue(tool.Key, out var currentBest);

                var releaseIndex =
                    await nuGetClient.GetPackageInformationAsync(baseUrl, tool.Key).ConfigureAwait(false);

                if (releaseIndex != null)
                {
                    var pages = nuGetReleaseFinder.TryFindNewReleaseCatalogPages(
                        releaseIndex,
                        initialVersion);

                    if (pages.Count == 0)
                    {
                        logger.Debug("No new versions found in catalog for {Tool}", tool);
                    }
                    else
                    {
                        // then there are 2 options at the moment:
                        // either the release info is in the release index itself
                        // or the release info is in a page linked from the release index
                        SemanticVersion? candidateVersion;
                        if (pages[0].HasPackageDetails)
                        {
                            candidateVersion =
                                nuGetReleaseFinder.TryFindNewVersionInCatalogPages(
                                    pages,
                                    initialVersion,
                                    bumpType,
                                    initialVersion.IsPreRelease);
                        }
                        else
                        {
                            var detailPages = await nuGetClient.GetRelevantCatalogPagesAsync(pages);
                            candidateVersion =
                                nuGetReleaseFinder.TryFindNewVersionInCatalogPages(
                                    detailPages.ToList(),
                                    initialVersion,
                                    bumpType,
                                    initialVersion.IsPreRelease);
                        }

                        if (candidateVersion != null && IsBetterCandidate(candidateVersion, currentBest))
                        {
                            bestVersions[tool.Key] = candidateVersion;
                        }
                    }
                }
            }
        }

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

    private static bool IsBetterCandidate(SemanticVersion candidate, SemanticVersion? currentBest)
    {
        if (currentBest == null)
        {
            return true;
        }

        if (!candidate.IsPreRelease && currentBest.IsPreRelease)
        {
            return true;
        }

        if (candidate.IsPreRelease && !currentBest.IsPreRelease)
        {
            return false;
        }

        return candidate > currentBest;
    }
}
