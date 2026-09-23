// Copyright © Roby Van Damme.

using DotBump.Commands.BumpTools.DataModel.LocalTools;

namespace DotBump.Commands.BumpTools.Interfaces;

internal interface IToolFileService
{
    ToolsManifest GetToolsManifest();

    void SaveToolsManifest(ToolsManifest manifest);
}
