// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Commands.BumpSdk.Interfaces;
using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpSdk;

internal class ReleaseFinder(ILogger logger) : IReleaseFinder
{
    public Release? TryFindNewRelease(Sdk currentSdk, IReadOnlyList<Release> releases, BumpType bumpType)
    {
        logger.MethodStart(nameof(ReleaseFinder), nameof(TryFindNewRelease));

        ArgumentNullException.ThrowIfNull(currentSdk);
        ArgumentNullException.ThrowIfNull(releases);

        Release? newRelease = null;

        switch (bumpType)
        {
            case BumpType.Minor:
                newRelease = TryFindMinorOrPatch(currentSdk, releases);
                break;
            case BumpType.Patch:
                newRelease = TryFindPatch(currentSdk, releases);
                break;
            case BumpType.Lts:
                break;
            case BumpType.Stable:
                break;
            case BumpType.Preview:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(bumpType), bumpType, null);
        }

        if (newRelease != null)
        {
            logger.MethodReturn(nameof(ReleaseFinder), nameof(TryFindNewRelease), newRelease);
            return newRelease;
        }

        logger.Debug("No new release found");
        logger.MethodReturn(nameof(ReleaseFinder), nameof(TryFindNewRelease));
        return null;
    }

    private Release? TryFindMinorOrPatch(Sdk currentSdk, IReadOnlyList<Release> releases)
    {
        logger.MethodStart(nameof(ReleaseFinder), nameof(TryFindMinorOrPatch));

        var relevantRelease =
            releases.FirstOrDefault(o => o.LatestSdkVersion.Major == currentSdk.SemanticVersion.Major);

        if (relevantRelease != null)
        {
            if (relevantRelease.LatestSdkVersion.Minor > currentSdk.SemanticVersion.Minor)
            {
                logger.Debug("Found new minor release {Release}", relevantRelease.LatestSdkVersion.ToString());
                logger.MethodReturn(nameof(ReleaseFinder), nameof(TryFindMinorOrPatch), relevantRelease);
                return relevantRelease;
            }

            if (relevantRelease.LatestSdkVersion.Patch > currentSdk.SemanticVersion.Patch)
            {
                logger.Debug("Found new patch release {Release}", relevantRelease.LatestSdkVersion.ToString());
                logger.MethodReturn(nameof(ReleaseFinder), nameof(TryFindMinorOrPatch), relevantRelease);
                return relevantRelease;
            }
        }

        logger.MethodReturn(nameof(ReleaseFinder), nameof(TryFindMinorOrPatch));
        return null;
    }

    private Release? TryFindPatch(Sdk currentSdk, IReadOnlyList<Release> releases)
    {
        logger.MethodStart(nameof(ReleaseFinder), nameof(TryFindPatch));

        var relevantRelease =
            releases.FirstOrDefault(o =>
                o.LatestSdkVersion.Major == currentSdk.SemanticVersion.Major &&
                o.LatestSdkVersion.Minor == currentSdk.SemanticVersion.Minor);

        if (relevantRelease != null)
        {
            if (relevantRelease.LatestSdkVersion.Patch > currentSdk.SemanticVersion.Patch)
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
