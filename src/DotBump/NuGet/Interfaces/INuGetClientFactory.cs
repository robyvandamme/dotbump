// Copyright © Roby Van Damme.

using DotBump.NuGet.DataModel.NuGetClientConfiguration;

namespace DotBump.NuGet.Interfaces;

internal interface INuGetClientFactory
{
    INuGetClient CreateNuGetClient(NuGetClientConfig config);
}
