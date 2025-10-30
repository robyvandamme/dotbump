// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpTools.DataModel.NuGetClientConfiguration;

namespace DotBump.Commands.BumpTools.Interfaces;

internal interface IClientFactory
{
    HttpClient CreateHttpClient(NuGetClientConfig config);

    NuGetClient CreateNuGetClient(NuGetClientConfig config);
}
