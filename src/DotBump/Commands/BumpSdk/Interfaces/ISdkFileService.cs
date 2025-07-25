// Copyright © 2025 Roby Van Damme.

using DotBump.Commands.BumpSdk.DataModel;

namespace DotBump.Commands.BumpSdk.Interfaces;

internal interface ISdkFileService
{
    void UpdateSdkVersion(string oldSdkVersion, string newSdkVersion, string filePath);

    Sdk GetCurrentSdkVersionFromFile(string filePath);
}
