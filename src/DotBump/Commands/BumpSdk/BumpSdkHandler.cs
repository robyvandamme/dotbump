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
            newRelease = await ResolveSecurityReleaseAsync(currentSdk, newRelease).ConfigureAwait(false);
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
    /// Resolves the candidate release to keep for a security-only bump, or <see langword="null"/> when
    /// the channel's release history does not contain a security release newer than the pinned SDK.
    /// The channel-level security flag only describes the newest release, so it cannot tell whether an
    /// earlier security release was missed.
    /// </summary>
    private async Task<Release?> ResolveSecurityReleaseAsync(Sdk currentSdk, Release newRelease)
    {
        logger.MethodStart(nameof(BumpSdkHandler), nameof(ResolveSecurityReleaseAsync));

        Release? resolved;

        if (newRelease.Security)
        {
            resolved = newRelease;
        }
        else if (!Uri.TryCreate(newRelease.ReleasesJson, UriKind.Absolute, out var channelReleasesUri))
        {
            throw new DotBumpException(
                $"Release metadata for .NET {newRelease.ChannelVersion} does not link a release history URL.");
        }
        else
        {
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
                resolved = newRelease;
            }
            else
            {
                logger.Debug(
                    "No security release newer than {CurrentVersion} was published for .NET {ChannelVersion}; not bumping",
                    currentSdk.Version,
                    newRelease.ChannelVersion);
                resolved = null;
            }
        }

        logger.MethodReturn(
            nameof(BumpSdkHandler),
            nameof(ResolveSecurityReleaseAsync),
            resolved == null ? "dropped" : "kept");
        return resolved;
    }
}
