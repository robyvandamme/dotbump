// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpSdk.DataModel;

namespace DotBump.Commands.BumpSdk;

public interface IReleaseService
{
    Task<IEnumerable<Release>> GetReleasesAsync();
}
