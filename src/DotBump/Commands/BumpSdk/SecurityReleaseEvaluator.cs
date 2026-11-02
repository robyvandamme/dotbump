// Copyright © Roby Van Damme.

using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Commands.BumpSdk.Interfaces;
using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpSdk;

internal class SecurityReleaseEvaluator(ILogger logger) : ISecurityReleaseEvaluator
{
    public bool IncludesNewerSecurityRelease(
        SemanticVersion currentVersion,
        SemanticVersion targetVersion,
        ChannelReleases channelReleases)
    {
        logger.MethodStart(nameof(SecurityReleaseEvaluator), nameof(IncludesNewerSecurityRelease));

        ArgumentNullException.ThrowIfNull(currentVersion);
        ArgumentNullException.ThrowIfNull(targetVersion);
        ArgumentNullException.ThrowIfNull(channelReleases);

        // A channel can ship several feature bands per release, so every SDK of every security release
        // is a candidate. The newest matching SDK is reported for troubleshooting.
        var securityVersion = channelReleases.Releases
            .Where(release => release.Security)
            .SelectMany(release => release.Sdks ?? [])
            .Select(sdk => sdk.SemanticVersion)
            .Where(version => version > currentVersion && version <= targetVersion)
            .OrderByDescending(version => version)
            .FirstOrDefault();

        if (securityVersion == null)
        {
            logger.Debug(
                "No security release found between {CurrentVersion} and {TargetVersion}",
                currentVersion.ToString(),
                targetVersion.ToString());
            logger.MethodReturn(nameof(SecurityReleaseEvaluator), nameof(IncludesNewerSecurityRelease), false);
            return false;
        }

        logger.Debug(
            "Security release {SecurityVersion} found between {CurrentVersion} and {TargetVersion}",
            securityVersion.ToString(),
            currentVersion.ToString(),
            targetVersion.ToString());
        logger.MethodReturn(nameof(SecurityReleaseEvaluator), nameof(IncludesNewerSecurityRelease), true);
        return true;
    }
}
