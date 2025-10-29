// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpSdk;
using DotBump.Commands.BumpTools.Interfaces;
using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpTools;

internal class BumpToolsHandler(
    IToolFileService toolFileService,
    INuGetServiceClient nuGetServiceClient,
    INuGetReleaseService nuGetReleaseService,
    ILogger logger) : IBumpToolsHandler
{
    public async Task<IReadOnlyCollection<BumpToolResult>> HandleAsync(BumpType bumpType)
    {
        logger.MethodStart(nameof(BumpSdkHandler), nameof(HandleAsync), bumpType);

        var manifest = toolFileService.GetToolManifest();
        var nugetPackageSources = toolFileService.GetNuGetPackageSources();
        var indexes = await nuGetServiceClient.GetServiceIndexesAsync(nugetPackageSources)
            .ConfigureAwait(false);
        var baseUrls = nuGetReleaseService.GetRegistrationsUrls(indexes);

        var bumpToolResults = new List<BumpToolResult>();

        for (var i = 0; i < manifest.Tools.Count; i++)
        {
            var tool = manifest.Tools.ElementAt(i);
            var releaseIndex =
                await nuGetServiceClient.GetPackageInformationAsync(baseUrls, tool.Key).ConfigureAwait(false);

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
                        bumpToolResults.Add(new BumpToolResult(tool.Key, tool.Value.Version, newVersion.ToString()));
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
                    var detailPages = await nuGetServiceClient.GetRelevantDetailCatalogPagesAsync(pages);
                    var newVersion =
                        nuGetReleaseService.TryGetNewMinorOrPatchVersionFromDetailCatalogPages(
                            detailPages.ToList(),
                            tool.Value.SemanticVersion);
                    if (newVersion != null)
                    {
                        bumpToolResults.Add(new BumpToolResult(tool.Key, tool.Value.Version, newVersion.ToString()));
                        tool.Value.Version = newVersion.ToString();
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
