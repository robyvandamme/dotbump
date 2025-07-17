// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.Sdk.DataModel;

namespace DotBump.Commands.Sdk;

public interface IReleaseFinder
{
    Release? TryFindNewRelease(DataModel.Sdk currentSdk, IReadOnlyList<Release> releases, BumpType bumpType);
}
