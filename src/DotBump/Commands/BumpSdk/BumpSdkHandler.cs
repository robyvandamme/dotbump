// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpSdk.Interfaces;
using DotBump.Common;
using DotBump.Reports;
using Serilog;

namespace DotBump.Commands.BumpSdk;

internal class BumpSdkHandler(
    ISdkFileService fileService,
    IReleaseService releaseService,
    IReleaseFinder releaseFinder,
    ILogger logger) : IBumpSdkHandler
{
    public async Task<BumpReport> HandleAsync(BumpType bumpType, string filePath, bool securityOnly)
    {
        logger.MethodStart(nameof(BumpSdkHandler), nameof(HandleAsync));

        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var currentSdk = fileService.GetCurrentSdkVersionFromFile(filePath);
        var bumpReport = new BumpReport(currentSdk, bumpType);

        var releases = await releaseService.GetReleasesAsync().ConfigureAwait(false);
        var newRelease = releaseFinder.TryFindNewRelease(currentSdk, releases.ToList(), bumpType, securityOnly);

        if (newRelease != null)
        {
            logger.Debug(
                "New release found. Updating sdk version to {Version}",
                newRelease.LatestSdkVersion.ToString());
            fileService.UpdateSdkVersion(currentSdk.Version, newRelease.LatestSdk, filePath);
            bumpReport.ReportChanges(newRelease);
            logger.MethodReturn(nameof(BumpSdkHandler), nameof(HandleAsync), bumpReport);
            return bumpReport;
        }

        logger.Debug("No new release found. Current Sdk version {Version}", currentSdk.SemanticVersion.ToString());
        bumpReport.ReportNoSdkVersionChanges();
        logger.MethodReturn(nameof(BumpSdkHandler), nameof(HandleAsync), bumpReport);
        return bumpReport;
    }
}
