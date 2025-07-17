// Copyright © 2025 Roby Van Damme.

namespace DotBump.Commands.Sdk;

public interface ISdkFileService
{
    void UpdateSdkVersion(string oldSdkVersion, string newSdkVersion, string filePath);

    DataModel.Sdk GetCurrentSdkVersionFromFile(string filePath);
}
