// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpTools.DataModel.LocalTools;
using DotBump.Commands.BumpTools.DataModel.NuGetConfiguration;

namespace DotBump.Commands.BumpTools.Interfaces;

internal interface IToolFileService
{
    ToolsManifest GetToolsManifest();

    NuGetConfig GetNuGetConfiguration(string nugetConfigPath);

    void SaveToolsManifest(ToolsManifest manifest);
}
