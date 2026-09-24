// Copyright © Roby Van Damme.

using DotBump.Commands;
using DotBump.Common;
using DotBump.NuGet.DataModel.NuGetClientConfiguration;
using DotBump.NuGet.DataModel.NuGetConfiguration;
using DotBump.NuGet.DataModel.PackageResolution;
using DotBump.NuGet.Interfaces;
using Serilog;

namespace DotBump.NuGet;

internal class PackageVersionResolver(
    INuGetClientFactory nuGetClientFactory,
    INuGetReleaseFinder nuGetReleaseFinder,
    ILogger logger) : IPackageVersionResolver
{
    public async Task<IReadOnlyDictionary<string, SemanticVersion>> ResolveAsync(
        IReadOnlyCollection<PackageToBump> packages,
        BumpType bumpType,
        NuGetConfig nuGetConfiguration)
    {
        logger.MethodStart(nameof(PackageVersionResolver), nameof(ResolveAsync), bumpType);

        ArgumentNullException.ThrowIfNull(packages);
        ArgumentNullException.ThrowIfNull(nuGetConfiguration);

        var bestVersions = new Dictionary<string, SemanticVersion>();

        if (packages.Count == 0 || nuGetConfiguration.PackageSources.Count == 0)
        {
            logger.MethodReturn(nameof(PackageVersionResolver), nameof(ResolveAsync), bestVersions);
            return bestVersions;
        }

        var packageList = packages.ToList();

        foreach (var nugetPackageSource in nuGetConfiguration.PackageSources)
        {
            var clientConfig = new NuGetClientConfig(nugetPackageSource.Key, nuGetConfiguration, logger);

            using var nuGetClient = nuGetClientFactory.CreateNuGetClient(clientConfig);
            var index = await nuGetClient.GetServiceIndexAsync(clientConfig.Url).ConfigureAwait(false);
            var baseUrl = nuGetReleaseFinder.GetRegistrationsBaseUrl(index);

            foreach (var package in packageList)
            {
                bestVersions.TryGetValue(package.PackageId, out var currentBest);

                var releaseIndex = await nuGetClient
                    .GetPackageInformationAsync(baseUrl, package.PackageId)
                    .ConfigureAwait(false);

                if (releaseIndex == null)
                {
                    continue;
                }

                var pages = nuGetReleaseFinder.TryFindNewReleaseCatalogPages(
                    releaseIndex,
                    package.CurrentVersion);

                if (pages.Count == 0)
                {
                    logger.Debug("No new versions found in catalog for {Package}", package.PackageId);
                    continue;
                }

                // Then there are 2 options at the moment:
                // either the release info is in the release index itself
                // or the release info is in a page linked from the release index.
                SemanticVersion? candidateVersion;
                if (pages[0].HasPackageDetails)
                {
                    candidateVersion = nuGetReleaseFinder.TryFindNewVersionInCatalogPages(
                        pages,
                        package.CurrentVersion,
                        bumpType,
                        package.CurrentVersion.IsPreRelease);
                }
                else
                {
                    var detailPages = await nuGetClient
                        .GetRelevantCatalogPagesAsync(pages)
                        .ConfigureAwait(false);
                    candidateVersion = nuGetReleaseFinder.TryFindNewVersionInCatalogPages(
                        detailPages.ToList(),
                        package.CurrentVersion,
                        bumpType,
                        package.CurrentVersion.IsPreRelease);
                }

                if (candidateVersion != null && IsBetterCandidate(candidateVersion, currentBest))
                {
                    bestVersions[package.PackageId] = candidateVersion;
                }
            }
        }

        logger.MethodReturn(nameof(PackageVersionResolver), nameof(ResolveAsync), bestVersions);

        return bestVersions;
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
