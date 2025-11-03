// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpSdk;
using DotBump.Commands.BumpTools.DataModel.NuGetClientConfiguration;
using DotBump.Commands.BumpTools.DataModel.Report;
using DotBump.Commands.BumpTools.Interfaces;
using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpTools;

internal class BumpToolsHandler(
    IToolFileService toolFileService,
    INuGetClientFactory nuGetClientFactory,
    INuGetReleaseFinder nuGetReleaseFinder,
    ILogger logger) : IBumpToolsHandler
{
    public async Task<BumpReport> HandleAsync(BumpType bumpType)
    {
        logger.MethodStart(nameof(BumpSdkHandler), nameof(HandleAsync), bumpType);

        var manifest = toolFileService.GetToolManifest();
        var bumpReport = new BumpReport(manifest);

        var nuGetConfiguration = toolFileService.GetNuGetConfiguration();

        foreach (var nugetPackageSource in nuGetConfiguration.PackageSources)
        {
            var clientConfig = new NuGetClientConfig(nugetPackageSource.Key, nuGetConfiguration, logger);

            using var nuGetClient = nuGetClientFactory.CreateNuGetClient(clientConfig);
            var index = await nuGetClient.GetServiceIndexAsync(clientConfig.Url).ConfigureAwait(false);
            var baseUrl = nuGetReleaseFinder.GetRegistrationsBaseUrl(index);

            for (var i = 0; i < manifest.Tools.Count; i++)
            {
                var tool = manifest.Tools.ElementAt(i);
                var releaseIndex =
                    await nuGetClient.GetPackageInformationAsync(baseUrl, tool.Key).ConfigureAwait(false);

                if (releaseIndex != null)
                {
                    var pages = nuGetReleaseFinder.TryFindNewReleaseCatalogPages(
                        releaseIndex,
                        tool.Value.SemanticVersion,
                        bumpType);

                    if (pages.Count == 0)
                    {
                        logger.Debug("No new versions found in catalog for {Tool}", tool);
                    }

                    // then there are 2 options at the moment:
                    // either the release info is in the release index itself
                    // or the release info is in a page linked from the release index
                    else if (pages.First().HasPackageDetails)
                    {
                        // we can extract the version from the pages
                        var newVersion =
                            nuGetReleaseFinder.TryFindVersionInCatalogPages(
                                pages,
                                tool.Value.SemanticVersion,
                                bumpType);
                        if (newVersion != null)
                        {
                            tool.Value.Version = newVersion.ToString();
                        }
                    }
                    else
                    {
                        var detailPages = await nuGetClient.GetRelevantCatalogPagesAsync(pages);
                        var newVersion =
                            nuGetReleaseFinder.TryFindVersionInCatalogPages(
                                detailPages.ToList(),
                                tool.Value.SemanticVersion,
                                bumpType);
                        if (newVersion != null)
                        {
                            tool.Value.Version = newVersion.ToString();
                        }
                    }
                }
            }
        }

        bumpReport.ReportChanges(manifest);

        if (bumpReport.HasChanges)
        {
            toolFileService.SaveToolManifest(manifest);
        }

        logger.MethodReturn(nameof(BumpSdkHandler), nameof(HandleAsync), bumpReport);

        return bumpReport;
    }
}
