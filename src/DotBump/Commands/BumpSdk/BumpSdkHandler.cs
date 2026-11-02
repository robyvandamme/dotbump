// Copyright © Roby Van Damme.

using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Commands.BumpSdk.Interfaces;
using DotBump.Common;
using DotBump.Reports;
using Serilog;

namespace DotBump.Commands.BumpSdk;

internal class BumpSdkHandler(
    ISdkFileService fileService,
    IReleaseService releaseService,
    IReleaseFinder releaseFinder,
    ISecurityReleaseEvaluator securityReleaseEvaluator,
    ILogger logger) : IBumpSdkHandler
{
    public async Task<BumpReport> HandleAsync(BumpType bumpType, string filePath, bool securityOnly, bool preRelease)
    {
        logger.MethodStart(nameof(BumpSdkHandler), nameof(HandleAsync));

        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var currentSdk = fileService.GetCurrentSdkVersionFromFile(filePath);
        var bumpReport = new BumpReport(currentSdk, bumpType);

        var releases = await releaseService.GetReleasesAsync().ConfigureAwait(false);
        var newRelease =
            releaseFinder.TryFindNewRelease(currentSdk, releases.ToList(), bumpType, preRelease);

        if (newRelease != null && securityOnly)
        {
            newRelease = await TryKeepSecurityReleaseAsync(currentSdk, newRelease).ConfigureAwait(false);
        }

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

    /// <summary>
    /// Keeps the candidate release only when the channel's release history contains a security
    /// release newer than the pinned SDK. The channel-level security flag only describes the newest
    /// release, so it cannot tell whether an earlier security release was missed.
    /// </summary>
    private async Task<Release?> TryKeepSecurityReleaseAsync(Sdk currentSdk, Release newRelease)
    {
        if (newRelease.Security)
        {
            return newRelease;
        }

        if (!Uri.TryCreate(newRelease.ReleasesJson, UriKind.Absolute, out var channelReleasesUri))
        {
            throw new DotBumpException(
                $"Release metadata for .NET {newRelease.ChannelVersion} does not link a release history URL.");
        }

        var channelReleases =
            await releaseService.GetChannelReleasesAsync(channelReleasesUri).ConfigureAwait(false);

        if (securityReleaseEvaluator.IncludesNewerSecurityRelease(
                currentSdk.SemanticVersion,
                newRelease.LatestSdkVersion,
                channelReleases))
        {
            logger.Debug(
                "Bumping to {TargetVersion} includes a security release newer than {CurrentVersion}",
                newRelease.LatestSdk,
                currentSdk.Version);
            return newRelease;
        }

        logger.Debug(
            "No security release newer than {CurrentVersion} was published for .NET {ChannelVersion}; not bumping",
            currentSdk.Version,
            newRelease.ChannelVersion);
        return null;
    }
}
