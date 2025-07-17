// Copyright © 2025 Roby Van Damme.

using System.Text.Json;
using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Common;
using Serilog;

namespace DotBump.Commands.BumpSdk;

internal class SdkFileService(ILogger logger) : ISdkFileService
{
    public DataModel.Sdk GetCurrentSdkVersionFromFile(string filePath)
    {
        logger.MethodStart(nameof(SdkFileService), nameof(GetCurrentSdkVersionFromFile));

        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
        {
            logger.Error($"File {filePath} does not exist");
            throw new DotBumpException($"File {filePath} not found.");
        }

        var json = File.ReadAllText(filePath);
        var globalJson = JsonSerializer.Deserialize<GlobalJson>(json);
        if (globalJson == null)
        {
            throw new DotBumpException($"Failed to deserialize {filePath}");
        }

        if (globalJson.Sdk == null)
        {
            throw new DotBumpException($"Failed to read SDK information from {filePath}");
        }

        if (string.IsNullOrWhiteSpace(globalJson.Sdk.Version))
        {
            throw new DotBumpException($"Failed to read version information from {filePath}");
        }

        logger.MethodReturn(nameof(SdkFileService), nameof(GetCurrentSdkVersionFromFile), globalJson.Sdk);
        return globalJson.Sdk;
    }

    public void UpdateSdkVersion(string oldSdkVersion, string newSdkVersion, string filePath)
    {
        logger.MethodStart(nameof(SdkFileService), nameof(UpdateSdkVersion));

        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
        {
            throw new ArgumentException($"File {filePath} not found.", nameof(filePath));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(oldSdkVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(newSdkVersion);

        var json = File.ReadAllText(filePath);
        var updated = json.Replace(oldSdkVersion, newSdkVersion, StringComparison.OrdinalIgnoreCase);
        File.WriteAllText(filePath, updated);

        logger.MethodReturn(nameof(SdkFileService), nameof(UpdateSdkVersion));
    }
}
