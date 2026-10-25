// Copyright © Roby Van Damme.

using DotBump.Commands.BumpLanguage.DataModel;
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

        var targetVersion = LanguageVersion.FromSdkMajor(currentSdk.SemanticVersion.Major);
        var targetLanguageVersion = targetVersion.ToString();
        logger.Debug("Target language version: {LanguageVersion}", targetLanguageVersion);

        var manifest = languageFileService.GetLanguageManifest(repositoryPath, excludedPaths);
        foreach (var entry in manifest.Entries)
        {
            if (entry.ParsedVersion.CompareTo(targetVersion) < 0)
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
}
