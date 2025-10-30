// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpSdk;
using DotBump.Commands.BumpTools.DataModel.NuGetClientConfiguration;
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
    public async Task<IReadOnlyCollection<BumpToolResult>> HandleAsync(BumpType bumpType)
    {
        logger.MethodStart(nameof(BumpSdkHandler), nameof(HandleAsync), bumpType);

        var bumpToolResults = new List<BumpToolResult>();

        var manifest = toolFileService.GetToolManifest();
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
                        break;
                    }

                    // then there are 2 options?
                    // either the release info is in the release index itself
                    // or the release info is in a page linked from the release index
                    if (pages.First().HasPackageDetails)
                    {
                        // we can extract the version from the pages
                        var newVersion =
                            nuGetReleaseService.TryGetNewMinorOrPatchVersionFromCatalogPages(
                                pages,
                                tool.Value.SemanticVersion);
                        if (newVersion != null)
                        {
                            bumpToolResults.Add(
                                new BumpToolResult(tool.Key, tool.Value.Version, newVersion.ToString()));
                            tool.Value.Version = newVersion.ToString();
                        }
                    }
                    else
                    {
                        // TODO: get the relevant detail pages
                        //  However... it might be possible to pick up the version from the index?
                        //  in case the upper one is one that we can use? If the upper one is one we can use we pick that one
                        //  otherwise we need to dig deeper.
                        //  So: first check if we have a usable version available here: that would be the last available version only?
                        //  if not fetch the relevant pages.
                        //  For now: fetch the relevant pages. Add the optimization to the backlog.
                        //  That would be Pages.Last.HasMatchingVersion or something along those lines
                        var detailPages = await nuGetClient.GetRelevantDetailCatalogPagesAsync(pages);
                        var newVersion =
                            nuGetReleaseService.TryGetNewMinorOrPatchVersionFromDetailCatalogPages(
                                detailPages.ToList(),
                                tool.Value.SemanticVersion);
                        if (newVersion != null)
                        {
                            bumpToolResults.Add(
                                new BumpToolResult(tool.Key, tool.Value.Version, newVersion.ToString()));
                            tool.Value.Version = newVersion.ToString();
                        }
                    }
                }
            }
        }

        // TODO: only save when there are changes.
        // do we always have results? depends on the strategy.
        // If we look at it as a report, we include all tools and the new and the old version? Even when there are no updates? Yes....
        // So the question is: always report everything or only report changes?
        if (bumpToolResults.Any(o => o.NewVersion != o.OldVersion))
        {
            toolFileService.SaveToolManifest(manifest);
        }

        logger.MethodReturn(nameof(BumpSdkHandler), nameof(HandleAsync), bumpToolResults);

        return bumpToolResults;
    }
}
