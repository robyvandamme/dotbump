// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpTools.DataModel.LocalTools;

namespace DotBump.Commands.BumpTools.Interfaces;

internal interface IToolFileService
{
    ToolManifest GetToolManifest();

    IEnumerable<string> GetNuGetPackageSources();
}
