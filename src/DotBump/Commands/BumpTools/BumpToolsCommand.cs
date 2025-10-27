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
    IToolFileService toolFileService,
    INuGetServiceClient nuGetServiceClient,
    INuGetReleaseService nuGetReleaseService)
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
            var nugetPackageSources = toolFileService.GetNuGetPackageSources().ToList();

            console.MarkupLine(">> NuGet Package Sources:");
            foreach (var packageSource in nugetPackageSources)
            {
                console.MarkupLine(packageSource);
            }

            var indexes = await nuGetServiceClient.GetServiceIndexesAsync(nugetPackageSources.ToList())
                .ConfigureAwait(false);

            // console.MarkupLine(">> Services indexes:");

            // foreach (var serviceIndex in indexes)
            // {
            //     var registrationResource = serviceIndex.Resources.FirstOrDefault(o => o.Type.StartsWith(
            //         "Catalog",
            //         StringComparison.OrdinalIgnoreCase));
            //
            //     console.MarkupLine(registrationResource?.Id ?? "RegistrationsBaseUrl empty for service index");
            // }
            console.MarkupLine(">> Base URLs:");

            var baseUrls = nuGetReleaseService.GetRegistrationsUrls(indexes);

            foreach (var baseUrl in baseUrls)
            {
                console.MarkupLine(baseUrl);
            }

            var bumpToolResults = new List<BumpToolResult>();

            console.MarkupLine(">> Local Tools:");
            for (var i = 0; i < manifest.Tools.Count; i++)
            {
                var tool = manifest.Tools.ElementAt(i);
                console.MarkupLine(tool.Key);

                var releaseIndex =
                    await nuGetServiceClient.GetPackageInformationAsync(baseUrls, tool.Key).ConfigureAwait(false);

                console.MarkupLine(
                    releaseIndex != null ? releaseIndex.Id : $"No Release index found for package ID {tool.Key}");

                if (releaseIndex != null)
                {
                    var pages = nuGetReleaseService.TryFindNewReleaseCatalogPages(
                        releaseIndex,
                        tool.Value.SemanticVersion);

                    if (pages.Count == 0)
                    {
                        console.MarkupLine("No relevant releases found.");

                        // no new releases
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
                        var detailPages = await nuGetServiceClient.GetRelevantDetailCatalogPagesAsync(pages);
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

                    console.MarkupLine("Relevant releases found.");
                }
            }

            toolFileService.SaveToolManifest(manifest);

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
