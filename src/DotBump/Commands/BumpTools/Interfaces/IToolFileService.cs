// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpTools.DataModel.LocalTools;
using DotBump.Commands.BumpTools.DataModel.NuGetConfiguration;

namespace DotBump.Commands.BumpTools.Interfaces;

internal interface IToolFileService
{
    ToolsManifest GetToolManifest(); // TODO: rename to GetToolsManifest

    NuGetConfig GetNuGetConfiguration(string nugetConfigPath);

    void SaveToolManifest(ToolsManifest manifest); // TODO: rename to SaveToolsManifest
}
