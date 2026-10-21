// Copyright © Roby Van Damme.

using DotBump.Commands.BumpFrameworks.Interfaces;
using DotBump.Commands.BumpSdk.Interfaces;
using DotBump.Common;
using DotBump.Reports;
using Serilog;

namespace DotBump.Commands.BumpFrameworks;

internal class BumpFrameworksHandler(
    IFrameworkFileService frameworkFileService,
    ISdkFileService sdkFileService,
    ILogger logger) : IBumpFrameworksHandler
{
    private const int MinimumSdkMajor = 5;

    public Task<BumpReport> HandleAsync(
        string repositoryPath,
        string globalJsonPath,
        IReadOnlyCollection<string>? excludedPaths = null)
    {
        logger.MethodStart(nameof(BumpFrameworksHandler), nameof(HandleAsync));

        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(globalJsonPath);

        var currentSdk = sdkFileService.GetCurrentSdkVersionFromFile(globalJsonPath);
        if (!currentSdk.SemanticVersion.IsValid || currentSdk.SemanticVersion.Major <= 0)
        {
            throw new DotBumpException(
                $"Failed to determine a valid target framework from SDK version '{currentSdk.Version}'.");
        }

        if (currentSdk.SemanticVersion.Major < MinimumSdkMajor)
        {
            throw new DotBumpException(
                $"The frameworks command requires .NET {MinimumSdkMajor} or later; " +
                $"SDK version '{currentSdk.Version}' cannot be mapped to a modern target framework.");
        }

        var targetFramework = $"net{currentSdk.SemanticVersion.Major}.0";
        logger.Debug("Target framework: {TargetFramework}", targetFramework);

        var manifest = frameworkFileService.GetFrameworkManifest(repositoryPath, excludedPaths);
        foreach (var framework in manifest.Frameworks)
        {
            manifest.SetFramework(framework, targetFramework + framework.Suffix);
        }

        if (manifest.HasChanges)
        {
            frameworkFileService.SaveFrameworkManifest(manifest);
        }

        var bumpReport = new BumpReport(manifest, BumpType.Major);
        bumpReport.ReportWarnings(manifest.Warnings);

        logger.MethodReturn(nameof(BumpFrameworksHandler), nameof(HandleAsync), bumpReport);
        return Task.FromResult(bumpReport);
    }
}
