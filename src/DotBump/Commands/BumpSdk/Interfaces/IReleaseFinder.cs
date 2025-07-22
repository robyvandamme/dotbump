// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpSdk.DataModel;

namespace DotBump.Commands.BumpSdk.Interfaces;

public interface IReleaseFinder
{
    Release? TryFindNewRelease(DataModel.Sdk currentSdk, IReadOnlyList<Release> releases, BumpType bumpType);
}
