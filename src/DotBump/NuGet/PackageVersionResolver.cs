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
    IPackageSourceMappingResolver packageSourceMappingResolver,
    INuGetClientFactory nuGetClientFactory,
    INuGetReleaseFinder nuGetReleaseFinder,
    ILogger logger) : IPackageVersionResolver
{
    public async Task<PackageResolutionResult> ResolveAsync(
        IReadOnlyCollection<PackageToBump> packages,
        BumpType bumpType,
        NuGetConfig nuGetConfiguration,
        bool preRelease = false)
    {
        logger.MethodStart(nameof(PackageVersionResolver), nameof(ResolveAsync), bumpType);

        ArgumentNullException.ThrowIfNull(packages);
        ArgumentNullException.ThrowIfNull(nuGetConfiguration);

        var packageList = packages.ToList();
        var mappingResult = packageSourceMappingResolver.Resolve(
            packageList.Select(o => o.PackageId).ToList(),
            nuGetConfiguration);

        LogMappingWarnings(mappingResult.Warnings);

        var bestVersions = new Dictionary<PackageToBump, SemanticVersion>();
        if (packageList.Count == 0 || nuGetConfiguration.PackageSources.Count == 0)
        {
            return CompleteResolution(bestVersions, mappingResult.Warnings);
        }

        var usePreRelease = preRelease && bumpType == BumpType.Major;
        var packagesBySource = BuildPackagesBySource(packageList, mappingResult);

        foreach (var nugetPackageSource in nuGetConfiguration.PackageSources)
        {
            if (!packagesBySource.TryGetValue(nugetPackageSource.Key, out var packagesForSource))
            {
                logger.Debug(
                    "Skipping package source {PackageSource} because no package is mapped to it",
                    nugetPackageSource.Key);
                continue;
            }

            await ResolveFromSourceAsync(
                    nugetPackageSource,
                    packagesForSource,
                    nuGetConfiguration,
                    bestVersions,
                    bumpType,
                    usePreRelease)
                .ConfigureAwait(false);
        }

        return CompleteResolution(bestVersions, mappingResult.Warnings);
    }

    private static bool IsBetterCandidate(SemanticVersion candidate, SemanticVersion? currentBest, bool preRelease)
    {
        if (currentBest == null)
        {
            return true;
        }

        if (preRelease)
        {
            return candidate > currentBest;
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

    private static void AddPackageToSource(
        Dictionary<string, List<PackageToBump>> packagesBySource,
        string sourceKey,
        PackageToBump package)
    {
        if (!packagesBySource.TryGetValue(sourceKey, out var packagesForSource))
        {
            packagesForSource = [];
            packagesBySource[sourceKey] = packagesForSource;
        }

        packagesForSource.Add(package);
    }

    private void LogMappingWarnings(IReadOnlyList<string> warnings)
    {
        foreach (var warning in warnings)
        {
            logger.Warning("{Warning}", warning);
        }
    }

    private void LogMappingMatch(PackageToBump package, PackageSourceMappingMatch match)
    {
        if (match.MatchedPattern == null)
        {
            return;
        }

        logger.Debug(
            "Package {PackageId} is mapped to package source(s) {PackageSources} by pattern {Pattern}",
            package.PackageId,
            match.Sources.Select(o => o.Key).ToArray(),
            match.MatchedPattern);
    }

    private Dictionary<string, List<PackageToBump>> BuildPackagesBySource(
        IReadOnlyList<PackageToBump> packageList,
        PackageSourceMappingResult mappingResult)
    {
        var packagesBySource = new Dictionary<string, List<PackageToBump>>(StringComparer.OrdinalIgnoreCase);

        foreach (var package in packageList)
        {
            if (!mappingResult.Matches.TryGetValue(package.PackageId, out var match) || match.Sources.Count == 0)
            {
                continue;
            }

            LogMappingMatch(package, match);

            foreach (var matchSource in match.Sources)
            {
                AddPackageToSource(packagesBySource, matchSource.Key, package);
            }
        }

        return packagesBySource;
    }

    private async Task ResolveFromSourceAsync(
        PackageSource nugetPackageSource,
        IReadOnlyList<PackageToBump> packagesForSource,
        NuGetConfig nuGetConfiguration,
        Dictionary<PackageToBump, SemanticVersion> bestVersions,
        BumpType bumpType,
        bool usePreRelease)
    {
        var clientConfig = new NuGetClientConfig(nugetPackageSource.Key, nuGetConfiguration, logger);

        using var nuGetClient = nuGetClientFactory.CreateNuGetClient(clientConfig);
        var index = await nuGetClient.GetServiceIndexAsync(clientConfig.Url).ConfigureAwait(false);
        var baseUrl = nuGetReleaseFinder.GetRegistrationsBaseUrl(index);

        foreach (var package in packagesForSource)
        {
            await ResolvePackageFromSourceAsync(
                    nuGetClient,
                    baseUrl,
                    package,
                    bestVersions,
                    bumpType,
                    usePreRelease)
                .ConfigureAwait(false);
        }
    }

    private async Task ResolvePackageFromSourceAsync(
        INuGetClient nuGetClient,
        string baseUrl,
        PackageToBump package,
        Dictionary<PackageToBump, SemanticVersion> bestVersions,
        BumpType bumpType,
        bool usePreRelease)
    {
        bestVersions.TryGetValue(package, out var currentBest);

        var candidateVersion = await ResolvePackageVersionAsync(
                nuGetClient,
                baseUrl,
                package,
                bumpType,
                usePreRelease)
            .ConfigureAwait(false);

        if (candidateVersion != null && IsBetterCandidate(candidateVersion, currentBest, usePreRelease))
        {
            bestVersions[package] = candidateVersion;
        }
    }

    private PackageResolutionResult CompleteResolution(
        IReadOnlyDictionary<PackageToBump, SemanticVersion> versions,
        IReadOnlyList<string> warnings)
    {
        var result = new PackageResolutionResult(versions, warnings);
        logger.MethodReturn(nameof(PackageVersionResolver), nameof(ResolveAsync), result);
        return result;
    }

    /// <summary>
    /// Resolves the best newer version for a single package from a single package source.
    /// </summary>
    private async Task<SemanticVersion?> ResolvePackageVersionAsync(
        INuGetClient nuGetClient,
        string baseUrl,
        PackageToBump package,
        BumpType bumpType,
        bool preRelease)
    {
        var releaseIndex = await nuGetClient
            .GetPackageInformationAsync(baseUrl, package.PackageId)
            .ConfigureAwait(false);

        if (releaseIndex == null)
        {
            return null;
        }

        var pages = nuGetReleaseFinder.TryFindNewReleaseCatalogPages(
            releaseIndex,
            package.CurrentVersion,
            bumpType);

        if (pages.Count == 0)
        {
            logger.Debug("No new versions found in catalog for {Package}", package.PackageId);
            return null;
        }

        // Then there are 2 options at the moment:
        // either the release info is in the release index itself
        // or the release info is in a page linked from the release index.
        if (pages[0].HasPackageDetails)
        {
            return nuGetReleaseFinder.TryFindNewVersionInCatalogPages(
                pages,
                package.CurrentVersion,
                bumpType,
                preRelease || package.CurrentVersion.IsPreRelease,
                preRelease);
        }

        var detailPages = await nuGetClient
            .GetRelevantCatalogPagesAsync(pages)
            .ConfigureAwait(false);

        return nuGetReleaseFinder.TryFindNewVersionInCatalogPages(
            detailPages.ToList(),
            package.CurrentVersion,
            bumpType,
            preRelease || package.CurrentVersion.IsPreRelease,
            preRelease);
    }
}
