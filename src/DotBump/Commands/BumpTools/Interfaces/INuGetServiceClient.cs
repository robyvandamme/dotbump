// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpTools.DataModel.NuGetService;

namespace DotBump.Commands.BumpTools.Interfaces;

internal interface INuGetServiceClient
{
    Task<List<ServiceIndex>> GetServiceIndexesAsync(ICollection<string> sources);
}
