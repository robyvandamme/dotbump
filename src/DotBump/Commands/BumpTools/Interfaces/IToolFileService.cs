// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpTools.DataModel.LocalTools;
using DotBump.Commands.BumpTools.DataModel.NuGetConfig;

namespace DotBump.Commands.BumpTools.Interfaces;

internal interface IToolFileService
{
    ToolManifest GetToolManifest();

    NuGetConfig GetNuGetConfiguration();

    void SaveToolManifest(ToolManifest manifest);
}
