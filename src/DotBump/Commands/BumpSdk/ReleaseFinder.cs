// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Commands.BumpSdk.Interfaces;
using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpSdk;

internal class ReleaseFinder(ILogger logger) : IReleaseFinder
{
    public Release? TryFindNewRelease(
        Sdk currentSdk,
        IReadOnlyList<Release> releases,
        BumpType bumpType,
        bool security)
    {
        logger.MethodStart(nameof(ReleaseFinder), nameof(TryFindNewRelease));

        ArgumentNullException.ThrowIfNull(currentSdk);
        ArgumentNullException.ThrowIfNull(releases);

        Release? newRelease;

        switch (bumpType)
        {
            case BumpType.Minor:
                newRelease = TryFindMinorOrPatch(currentSdk, releases, security);
                break;
            case BumpType.Patch:
                newRelease = TryFindPatch(currentSdk, releases, security);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(bumpType), bumpType, null);
        }

        if (newRelease == null)
        {
            logger.Debug("No new release found");
        }

        logger.MethodReturn(nameof(ReleaseFinder), nameof(TryFindNewRelease), newRelease);
        return newRelease;
    }

    private Release? TryFindMinorOrPatch(Sdk currentSdk, IReadOnlyList<Release> releases, bool security)
    {
        logger.MethodStart(nameof(ReleaseFinder), nameof(TryFindMinorOrPatch));

        var relevantRelease =
            releases.FirstOrDefault(o =>
                o.LatestSdkVersion.Major == currentSdk.SemanticVersion.Major);

        if (relevantRelease != null)
        {
            if (relevantRelease.LatestSdkVersion.Minor > currentSdk.SemanticVersion.Minor)
            {
                if (!security || relevantRelease.Security)
                {
                    logger.Debug("Found new minor release {Release}", relevantRelease.LatestSdkVersion.ToString());
                    logger.MethodReturn(nameof(ReleaseFinder), nameof(TryFindMinorOrPatch), relevantRelease);
                    return relevantRelease;
                }
            }

            if (relevantRelease.LatestSdkVersion.Patch > currentSdk.SemanticVersion.Patch)
            {
                if (!security || relevantRelease.Security)
                {
                    logger.Debug("Found new patch release {Release}", relevantRelease.LatestSdkVersion.ToString());
                    logger.MethodReturn(nameof(ReleaseFinder), nameof(TryFindMinorOrPatch), relevantRelease);
                    return relevantRelease;
                }
            }
        }

        logger.MethodReturn(nameof(ReleaseFinder), nameof(TryFindMinorOrPatch));
        return null;
    }

    private Release? TryFindPatch(Sdk currentSdk, IReadOnlyList<Release> releases, bool security)
    {
        logger.MethodStart(nameof(ReleaseFinder), nameof(TryFindPatch));

        var relevantRelease =
            releases.FirstOrDefault(o =>
                o.LatestSdkVersion.Major == currentSdk.SemanticVersion.Major &&
                o.LatestSdkVersion.Minor == currentSdk.SemanticVersion.Minor &&
                o.LatestSdkVersion.Patch > currentSdk.SemanticVersion.Patch);

        if (relevantRelease != null)
        {
            if (!security || relevantRelease.Security)
            {
                logger.Debug("Found new patch release {Release}", relevantRelease.LatestSdkVersion.ToString());
                logger.MethodReturn(nameof(ReleaseFinder), nameof(TryFindPatch), relevantRelease);
                return relevantRelease;
            }
        }

        logger.MethodReturn(nameof(ReleaseFinder), nameof(TryFindPatch));
        return null;
    }
}
