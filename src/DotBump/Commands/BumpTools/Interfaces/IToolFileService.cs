// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpTools.DataModel.LocalTools;
using DotBump.Commands.BumpTools.DataModel.NuGetConfiguration;

namespace DotBump.Commands.BumpTools.Interfaces;

internal interface IToolFileService
{
    ToolsManifest GetToolManifest();

    NuGetConfig GetNuGetConfiguration();

    void SaveToolManifest(ToolsManifest manifest);
}
