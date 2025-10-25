// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpTools.DataModel;

namespace DotBump.Commands.BumpTools.Interfaces;

internal interface IToolFileService
{
    ToolManifest GetToolManifest();

    IEnumerable<string> GetNuGetPackageSources();
}
