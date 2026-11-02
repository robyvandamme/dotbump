// Copyright © Roby Van Damme.

using DotBump.Commands.BumpDocker.DataModel;
using DotBump.Commands.BumpDocker.Interfaces;
using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Commands.BumpSdk.Interfaces;
using DotBump.Common;
using DotBump.Reports;
using Serilog;

namespace DotBump.Commands.BumpDocker;

internal class BumpDockerHandler(
    IDockerfileService dockerfileService,
    IReleaseService releaseService,
    IReleaseFinder releaseFinder,
    ILogger logger) : IBumpDockerHandler
{
    public async Task<BumpReport> HandleAsync(
        BumpType bumpType,
        string repositoryPath,
        IReadOnlyCollection<string>? excludedPaths = null,
        bool preRelease = false)
    {
        logger.MethodStart(nameof(BumpDockerHandler), nameof(HandleAsync), bumpType);

        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryPath);

        var manifest = dockerfileService.GetDockerfileManifest(repositoryPath, excludedPaths);
        var bumpReport = new BumpReport(manifest, bumpType);

        try
        {
            var currentEntry = manifest.Entries
                .OrderByDescending(entry => entry.ParsedVersion)
                .FirstOrDefault();

            if (currentEntry == null)
            {
                logger.Debug(
                    "No supported .NET SDK image found in {RepositoryPath}",
                    repositoryPath);
                return bumpReport;
            }

            var currentSdk = new Sdk(currentEntry.OriginalTag, string.Empty);
            var releases = await releaseService.GetReleasesAsync().ConfigureAwait(false);

            var newRelease = releaseFinder.TryFindNewRelease(
                currentSdk,
                releases.ToList(),
                bumpType,
                securityOnly: false,
                preRelease);

            if (newRelease == null)
            {
                logger.Debug(
                    "No new SDK release found for image tag {Tag}",
                    currentEntry.OriginalTag);
                return bumpReport;
            }

            var newTag = SdkImageTag.TryMapFromSdkVersion(newRelease.LatestSdk);
            if (newTag == null || !SdkImageTag.TryParse(newTag, out var newVersion))
            {
                var warning = $"Skipping SDK image bump because SDK version '{newRelease.LatestSdk}' cannot be mapped to a supported Docker image tag.";
                AddWarning(manifest, warning);
                return bumpReport;
            }

            // Never downgrade: only update occurrences below the resolved target version.
            foreach (var entry in manifest.Entries.Where(entry => newVersion > entry.ParsedVersion))
            {
                manifest.SetTag(entry, newTag);
            }

            bumpReport.ReportChanges(manifest);

            if (manifest.HasChanges)
            {
                dockerfileService.SaveDockerfileManifest(manifest);
            }

            return bumpReport;
        }
        finally
        {
            bumpReport.ReportWarnings(manifest.Warnings);
            logger.MethodReturn(nameof(BumpDockerHandler), nameof(HandleAsync), bumpReport);
        }
    }

    private void AddWarning(DockerfileManifest manifest, string warning)
    {
        logger.Warning("{Warning}", warning);
        manifest.AddWarning(warning);
    }
}
