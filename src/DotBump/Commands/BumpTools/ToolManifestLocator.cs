// Copyright © Roby Van Damme.

using DotBump.Commands.BumpTools.Interfaces;
using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpTools;

internal class ToolManifestLocator(ILogger logger) : IToolManifestLocator
{
    public string Resolve(string? explicitPath)
    {
        logger.MethodStart(nameof(ToolManifestLocator), nameof(Resolve));

        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            var explicitFullPath = Path.GetFullPath(explicitPath);
            logger.MethodReturn(nameof(ToolManifestLocator), nameof(Resolve), explicitFullPath);
            return explicitFullPath;
        }

        var currentDirectory = Directory.GetCurrentDirectory();
        var rootManifest = Path.Combine(currentDirectory, "dotnet-tools.json");
        var legacyManifest = Path.Combine(currentDirectory, ".config", "dotnet-tools.json");

        if (File.Exists(rootManifest))
        {
            logger.MethodReturn(nameof(ToolManifestLocator), nameof(Resolve), rootManifest);
            return rootManifest;
        }

        if (File.Exists(legacyManifest))
        {
            logger.MethodReturn(nameof(ToolManifestLocator), nameof(Resolve), legacyManifest);
            return legacyManifest;
        }

        throw new DotBumpException(
            $"Could not find a tools manifest. Looked for '{rootManifest}' and '{legacyManifest}'. " +
            "Use --manifest to specify its location.");
    }
}
