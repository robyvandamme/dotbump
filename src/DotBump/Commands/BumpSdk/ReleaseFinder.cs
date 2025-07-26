// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Commands.BumpSdk.Interfaces;
using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpSdk;

internal class ReleaseFinder(ILogger logger) : IReleaseFinder
{
    public Release? TryFindNewRelease(DataModel.Sdk currentSdk, IReadOnlyList<Release> releases, BumpType bumpType)
    {
        logger.MethodStart(nameof(ReleaseFinder), nameof(TryFindNewRelease));

        ArgumentNullException.ThrowIfNull(currentSdk);
        ArgumentNullException.ThrowIfNull(releases);

        Release? relevantRelease = null;

        switch (bumpType)
        {
            case BumpType.Minor:
                relevantRelease = TryFindMinorOrPatch(currentSdk, releases);
                break;
            case BumpType.Patch:
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

        if (relevantRelease != null)
        {
            logger.MethodReturn(nameof(ReleaseFinder), nameof(TryFindNewRelease), relevantRelease);
            return relevantRelease;
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
}
