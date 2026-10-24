// Copyright © Roby Van Damme.

using System.Globalization;
using DotBump.Commands.BumpLanguage.Interfaces;
using DotBump.Commands.BumpSdk.Interfaces;
using DotBump.Common;
using DotBump.Reports;
using Serilog;

namespace DotBump.Commands.BumpLanguage;

internal class BumpLanguageHandler(
    ILanguageFileService languageFileService,
    ISdkFileService sdkFileService,
    ILogger logger) : IBumpLanguageHandler
{
    private const int MinimumSdkMajor = 5;
    private const int LanguageVersionMajorOffset = 4;

    public Task<BumpReport> HandleAsync(
        string repositoryPath,
        string globalJsonPath,
        IReadOnlyCollection<string>? excludedPaths = null)
    {
        logger.MethodStart(nameof(BumpLanguageHandler), nameof(HandleAsync));

        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(globalJsonPath);

        var currentSdk = sdkFileService.GetCurrentSdkVersionFromFile(globalJsonPath);
        if (!currentSdk.SemanticVersion.IsValid || currentSdk.SemanticVersion.Major <= 0)
        {
            throw new DotBumpException(
                $"Failed to determine a valid C# language version from SDK version '{currentSdk.Version}'.");
        }

        if (currentSdk.SemanticVersion.Major < MinimumSdkMajor)
        {
            throw new DotBumpException(
                $"The language command requires .NET {MinimumSdkMajor} or later; " +
                $"SDK version '{currentSdk.Version}' cannot be mapped to a C# language version.");
        }

        var targetVersion = new Version(
            currentSdk.SemanticVersion.Major + LanguageVersionMajorOffset,
            0);
        var targetLanguageVersion = $"{targetVersion.Major}.0";
        logger.Debug("Target language version: {LanguageVersion}", targetLanguageVersion);

        var manifest = languageFileService.GetLanguageManifest(repositoryPath, excludedPaths);
        foreach (var entry in manifest.Entries)
        {
            if (TryParseVersion(entry.OriginalVersion, out var currentVersion) && currentVersion < targetVersion)
            {
                manifest.SetVersion(entry, targetLanguageVersion);
            }
        }

        if (manifest.HasChanges)
        {
            languageFileService.SaveLanguageManifest(manifest);
        }

        var bumpReport = new BumpReport(manifest, BumpType.Major);
        bumpReport.ReportWarnings(manifest.Warnings);

        logger.MethodReturn(nameof(BumpLanguageHandler), nameof(HandleAsync), bumpReport);
        return Task.FromResult(bumpReport);
    }

    private static bool TryParseVersion(string value, out Version version)
    {
        version = new Version(0, 0);

        var parts = value.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is < 1 or > 2
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var major))
        {
            return false;
        }

        var minor = 0;
        if (parts.Length == 2
            && !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out minor))
        {
            return false;
        }

        version = new Version(major, minor);
        return true;
    }
}
