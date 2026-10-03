// Copyright © Roby Van Damme.

namespace DotBump.Commands.BumpTools.Interfaces;

internal interface IToolManifestLocator
{
    string Resolve(string? explicitPath);
}
