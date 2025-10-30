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
    IClientFactory clientFactory,
    INuGetReleaseService nuGetReleaseService,
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

            if (!clientConfig.Url.Equals("https://api.nuget.org/v3/index.json", StringComparison.OrdinalIgnoreCase))
            {
                logger.Warning(
                    "Only the package source https://api.nuget.org/v3/index.json is supported at the moment");
                break;
            }

            using var nuGetClient = clientFactory.CreateNuGetClient(clientConfig);
            var index = await nuGetClient.GetServiceIndexAsync(clientConfig.Url).ConfigureAwait(false);
            var baseUrl = nuGetReleaseService.GetRegistrationsUrl(index);

            for (var i = 0; i < manifest.Tools.Count; i++)
            {
                var tool = manifest.Tools.ElementAt(i);
                var releaseIndex =
                    await nuGetClient.GetPackageInformationAsync(baseUrl, tool.Key).ConfigureAwait(false);

                if (releaseIndex != null)
                {
                    var pages = nuGetReleaseService.TryFindNewReleaseCatalogPages(
                        releaseIndex,
                        tool.Value.SemanticVersion);

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
                            nuGetReleaseService.TryGetNewMinorOrPatchVersionFromCatalogPages(
                                pages,
                                tool.Value.SemanticVersion);
                        if (newVersion != null)
                        {
                            tool.Value.Version = newVersion.ToString();
                        }
                    }
                    else
                    {
                        var detailPages = await nuGetClient.GetRelevantDetailCatalogPagesAsync(pages);
                        var newVersion =
                            nuGetReleaseService.TryGetNewMinorOrPatchVersionFromDetailCatalogPages(
                                detailPages.ToList(),
                                tool.Value.SemanticVersion);
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
