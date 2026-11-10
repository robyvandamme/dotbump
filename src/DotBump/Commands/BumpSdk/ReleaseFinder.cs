// Copyright © Roby Van Damme.

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
        bool preRelease)
    {
        logger.MethodStart(nameof(ReleaseFinder), nameof(TryFindNewRelease));

        ArgumentNullException.ThrowIfNull(currentSdk);
        ArgumentNullException.ThrowIfNull(releases);

        var newRelease = ReleaseSelector.TryFindNewest(
            currentSdk.SemanticVersion,
            releases,
            release => release.LatestSdkVersion,
            bumpType,
            preRelease);

        if (newRelease == null)
        {
            logger.Debug("No new release found");
        }
        else
        {
            logger.Debug("Found new release {Release}", newRelease.LatestSdkVersion.ToString());
        }

        logger.MethodReturn(nameof(ReleaseFinder), nameof(TryFindNewRelease), newRelease);
        return newRelease;
    }
}
