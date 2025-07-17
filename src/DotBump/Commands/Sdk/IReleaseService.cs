// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.Sdk.DataModel;

namespace DotBump.Commands.Sdk;

public interface IReleaseService
{
    Task<IEnumerable<Release>> GetReleasesAsync();
}
