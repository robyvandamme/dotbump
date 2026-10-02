// Copyright © Roby Van Damme.

using DotBump.Commands.BumpTools.DataModel.LocalTools;

namespace DotBump.Commands.BumpTools.Interfaces;

internal interface IToolFileService
{
    ToolsManifest GetToolsManifest(string manifestPath);

    void SaveToolsManifest(ToolsManifest manifest, string manifestPath);
}
