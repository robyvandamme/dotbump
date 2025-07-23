// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpSdk.DataModel;

namespace DotBump.Commands.BumpSdk.Interfaces;

internal interface IReleaseService
{
    Task<IEnumerable<Release>> GetReleasesAsync();
}
