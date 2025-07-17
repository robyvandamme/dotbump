// Copyright © 2025 Roby Van Damme.

using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpSdk;

internal class BumpSdkHandler(
    ISdkFileService fileService,
    IReleaseService releaseService,
    IReleaseFinder releaseFinder,
    ILogger logger) : IBumpSdkHandler
{
    public async Task<BumpSdkResult> HandleAsync(BumpType bumpType, string filePath)
    {
        logger.MethodStart(nameof(BumpSdkHandler), nameof(HandleAsync));

        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var currentSdk = fileService.GetCurrentSdkVersionFromFile(filePath);
        var releases = await releaseService.GetReleasesAsync().ConfigureAwait(false);
        var newRelease = releaseFinder.TryFindNewRelease(currentSdk, releases.ToList(), bumpType);

        if (newRelease != null)
        {
            logger.Debug(
                "New release found. Updating sdk version to {Version}",
                newRelease.LatestSdkVersion.ToString());
            fileService.UpdateSdkVersion(currentSdk.Version, newRelease.LatestSdk, filePath);
            var result = new BumpSdkResult(
                true,
                currentSdk.SemanticVersion.ToString(),
                newRelease.LatestSdkVersion.ToString());
            logger.MethodReturn(nameof(BumpSdkHandler), nameof(HandleAsync), result);
            return result;
        }
        else
        {
            logger.Debug("No new release found. Current Sdk version {Version}", currentSdk.SemanticVersion.ToString());
            var result = new BumpSdkResult(
                false,
                currentSdk.SemanticVersion.ToString(),
                currentSdk.SemanticVersion.ToString());
            logger.MethodReturn(nameof(BumpSdkHandler), nameof(HandleAsync), result);
            return result;
        }
    }
}
