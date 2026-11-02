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
            if (manifest.Entries.Count == 0)
            {
                logger.Debug(
                    "No supported .NET SDK image found in {RepositoryPath}",
                    repositoryPath);
                return bumpReport;
            }

            var releases = (await releaseService.GetReleasesAsync().ConfigureAwait(false)).ToList();

            foreach (var group in GroupEntries(manifest, bumpType))
            {
                ApplyBump(group, bumpType, releases, preRelease, manifest);
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

    /// <summary>
    /// Groups the image occurrences by the SDK channel they belong to. A patch bump stays within the
    /// current major.minor, while minor and major bumps start from each current major and let
    /// <see cref="IReleaseFinder"/> apply the major-version cap.
    /// </summary>
    private static IEnumerable<IGrouping<(int Major, int Minor), DockerImageEntry>> GroupEntries(
        DockerfileManifest manifest,
        BumpType bumpType)
    {
        IEnumerable<IGrouping<(int Major, int Minor), DockerImageEntry>> groups = bumpType == BumpType.Patch
            ? manifest.Entries.GroupBy(entry => (Major: entry.ParsedVersion.Major, Minor: entry.ParsedVersion.Minor))
            : manifest.Entries.GroupBy(entry => (Major: entry.ParsedVersion.Major, Minor: 0));

        return groups;
    }

    private void ApplyBump(
        IGrouping<(int Major, int Minor), DockerImageEntry> group,
        BumpType bumpType,
        IReadOnlyList<Release> releases,
        bool preRelease,
        DockerfileManifest manifest)
    {
        var referenceEntry = group.OrderByDescending(entry => entry.ParsedVersion).First();

        // ReleaseFinder only needs the parsed SDK version; the roll-forward setting is not used here.
        var referenceSdk = new Sdk(referenceEntry.OriginalTag, string.Empty);

        var newRelease = releaseFinder.TryFindNewRelease(
            referenceSdk,
            releases,
            bumpType,
            preRelease);

        if (newRelease == null)
        {
            logger.Debug(
                "No new SDK release found for .NET {Major}.{Minor} images (current tag {Tag})",
                group.Key.Major,
                group.Key.Minor,
                referenceEntry.OriginalTag);
            return;
        }

        var newTag = SdkImageTag.TryMapFromSdkVersion(newRelease.LatestSdk);
        if (newTag == null || !SdkImageTag.TryParse(newTag, out var newVersion))
        {
            var warning = $"Skipping .NET {group.Key.Major}.{group.Key.Minor} images because SDK version '{newRelease.LatestSdk}' cannot be mapped to a supported Docker image tag.";
            AddWarning(manifest, warning);
            return;
        }

        // Never downgrade: only update occurrences below the resolved target version.
        foreach (var entry in group.Where(entry => newVersion > entry.ParsedVersion))
        {
            manifest.SetTag(entry, newTag);
        }
    }

    private void AddWarning(DockerfileManifest manifest, string warning)
    {
        logger.Warning("{Warning}", warning);
        manifest.AddWarning(warning);
    }
}
