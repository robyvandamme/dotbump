// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpSdk;

public class ReleaseFinder(ILogger logger) : IReleaseFinder
{
    public Release? TryFindNewRelease(DataModel.Sdk currentSdk, IReadOnlyList<Release> releases, BumpType bumpType)
    {
        logger.MethodStart(nameof(ReleaseFinder), nameof(TryFindNewRelease));

        ArgumentNullException.ThrowIfNull(currentSdk);
        ArgumentNullException.ThrowIfNull(releases);

        var relevantRelease =
            releases.FirstOrDefault(o => o.LatestSdkVersion.Major == currentSdk.SemanticVersion.Major);

        if (relevantRelease != null)
        {
            if (relevantRelease.LatestSdkVersion.Minor > currentSdk.SemanticVersion.Minor)
            {
                logger.Debug("Found new minor release {Release}", relevantRelease.LatestSdkVersion.ToString());
                logger.MethodReturn(nameof(ReleaseFinder), nameof(TryFindNewRelease), relevantRelease);
                return relevantRelease; // new minor version
            }

            if (!relevantRelease.SupportPhase.Equals("preview", StringComparison.OrdinalIgnoreCase)) // TODO: why is this check here?
            {
                if (relevantRelease.LatestSdkVersion.Patch > currentSdk.SemanticVersion.Patch)
                {
                    logger.Debug("Found new patch release {Release}", relevantRelease.LatestSdkVersion.ToString());
                    logger.MethodReturn(nameof(ReleaseFinder), nameof(TryFindNewRelease), relevantRelease);
                    return relevantRelease; // new patch
                }
            }
        }

        logger.Debug("No new release found");
        logger.MethodReturn(nameof(ReleaseFinder), nameof(TryFindNewRelease));
        return null;
    }
}
