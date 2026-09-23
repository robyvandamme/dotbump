// Copyright © Roby Van Damme.

using DotBump.NuGet.DataModel.NuGetConfiguration;

namespace DotBump.NuGet.Interfaces;

internal interface INuGetConfigFileService
{
    /// <summary>
    /// Tries to read the NuGet configuration from the nugetConfigPath.
    /// If not found a default NuGet configuration is returned with the default https://api.nuget.org/v3/index.json package source.
    /// </summary>
    /// <param name="nugetConfigPath">The NuGet config file path.</param>
    /// <returns>A NuGet configuration.</returns>
    NuGetConfig GetNuGetConfiguration(string nugetConfigPath);
}
