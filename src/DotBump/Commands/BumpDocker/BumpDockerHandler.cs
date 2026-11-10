// Copyright © Roby Van Damme.

using System.Globalization;
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
                    "No supported .NET base image found in {RepositoryPath}",
                    repositoryPath);
                return bumpReport;
            }

            var releases = (await releaseService.GetReleasesAsync().ConfigureAwait(false)).ToList();
            var channelReleases = await GetChannelReleasesAsync(manifest, releases, bumpType)
                .ConfigureAwait(false);

            foreach (var group in GroupEntries(manifest, bumpType))
            {
                ApplyBump(group, bumpType, channelReleases, preRelease, manifest);
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

    private static string GetChannel(SemanticVersion version)
    {
        return $"{version.Major}.{version.Minor}";
    }

    private static IEnumerable<string> GetChannelsToLoad(
        DockerfileManifest manifest,
        IReadOnlyList<Release> releases,
        BumpType bumpType)
    {
        var manifestChannels = manifest.Entries
            .Select(entry => GetChannel(entry.ParsedVersion))
            .Distinct(StringComparer.Ordinal);

        if (bumpType != BumpType.Major)
        {
            return manifestChannels.OrderBy(channel => channel, StringComparer.Ordinal);
        }

        // A major bump can move images to a channel the manifest does not use yet, so the release
        // histories of newer channels are loaded as well.
        var minimumMajor = manifest.Entries.Min(entry => entry.ParsedVersion.Major);

        var newerChannels = releases
            .Where(release => GetChannelMajor(release.ChannelVersion) is { } major && major >= minimumMajor)
            .Select(release => release.ChannelVersion);

        return manifestChannels
            .Concat(newerChannels)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(channel => channel, StringComparer.Ordinal);
    }

    private static int? GetChannelMajor(string channelVersion)
    {
        var separatorIndex = channelVersion.IndexOf('.', StringComparison.Ordinal);
        var majorText = separatorIndex >= 0 ? channelVersion[..separatorIndex] : channelVersion;

        return int.TryParse(majorText, NumberStyles.None, CultureInfo.InvariantCulture, out var major)
            ? major
            : null;
    }

    /// <summary>
    /// Groups the image occurrences by the release component and the channel they belong to. A
    /// patch bump stays within the current major.minor, while minor and major bumps start from each
    /// current major and let <see cref="ReleaseSelector"/> apply the major-version cap.
    /// </summary>
    private static IEnumerable<IGrouping<ImageGroup, DockerImageEntry>> GroupEntries(
        DockerfileManifest manifest,
        BumpType bumpType)
    {
        IEnumerable<IGrouping<ImageGroup, DockerImageEntry>> groups = bumpType == BumpType.Patch
            ? manifest.Entries.GroupBy(entry => new ImageGroup(
                entry.VersionSource,
                entry.ParsedVersion.Major,
                entry.ParsedVersion.Minor))
            : manifest.Entries.GroupBy(entry => new ImageGroup(
                entry.VersionSource,
                entry.ParsedVersion.Major,
                0));

        return groups;
    }

    /// <summary>
    /// Loads the release history for every channel needed by the bump. A channel that cannot be
    /// resolved is skipped with a warning so one unavailable channel does not block the others.
    /// </summary>
    private async Task<Dictionary<string, ChannelReleases>> GetChannelReleasesAsync(
        DockerfileManifest manifest,
        IReadOnlyList<Release> releases,
        BumpType bumpType)
    {
        var channels = GetChannelsToLoad(manifest, releases, bumpType);

        var channelReleases = new Dictionary<string, ChannelReleases>(StringComparer.Ordinal);

        foreach (var channel in channels)
        {
            var release = releases.FirstOrDefault(item =>
                string.Equals(item.ChannelVersion, channel, StringComparison.Ordinal));

            if (release == null)
            {
                AddWarning(
                    manifest,
                    $"Skipping .NET {channel} images because the release index does not list that channel.");
                continue;
            }

            if (!Uri.TryCreate(release.ReleasesJson, UriKind.Absolute, out var channelReleasesUri))
            {
                AddWarning(
                    manifest,
                    $"Skipping .NET {channel} images because the release index does not link a release history URL.");
                continue;
            }

            try
            {
                channelReleases[channel] = await releaseService
                    .GetChannelReleasesAsync(channelReleasesUri)
                    .ConfigureAwait(false);
            }
#pragma warning disable CA1031
            catch (Exception exception)
#pragma warning restore CA1031
            {
                // The release history is the source of truth for every image version, so an
                // unreadable channel is skipped rather than bumped from incomplete data.
                AddWarning(
                    manifest,
                    $"Skipping .NET {channel} images because the release history could not be read: {exception.Message}");
            }
        }

        return channelReleases;
    }

    private void ApplyBump(
        IGrouping<ImageGroup, DockerImageEntry> group,
        BumpType bumpType,
        IReadOnlyDictionary<string, ChannelReleases> channelReleases,
        bool preRelease,
        DockerfileManifest manifest)
    {
        var referenceEntry = group.OrderByDescending(entry => entry.ParsedVersion).First();
        var channel = GetChannel(referenceEntry.ParsedVersion);

        if (!channelReleases.TryGetValue(channel, out var channelRelease))
        {
            // The channel was skipped while loading the release history; the warning is recorded.
            return;
        }

        // A major bump can move images to any loaded channel, which includes newer channels that
        // the manifest does not use yet.
        var candidates = bumpType == BumpType.Major
            ? channelReleases.Values.SelectMany(item => item.Releases)
            : channelRelease.Releases;

        var newRelease = ReleaseSelector.TryFindNewest(
            referenceEntry.ParsedVersion,
            candidates,
            release => ChannelReleaseVersions.GetVersion(release, group.Key.VersionSource),
            bumpType,
            preRelease);

        if (newRelease == null)
        {
            if (!candidates.Any(release =>
                    ChannelReleaseVersions.GetVersion(release, group.Key.VersionSource) != null))
            {
                AddWarning(
                    manifest,
                    $"Skipping .NET {channel} {group.Key.VersionSource} images because the release history does not contain a supported version.");
            }
            else
            {
                logger.Debug(
                    "No new .NET {Channel} release found for {VersionSource} images (current tag {Tag})",
                    channel,
                    group.Key.VersionSource,
                    referenceEntry.OriginalTag);
            }

            return;
        }

        var newVersion = ChannelReleaseVersions.GetVersion(newRelease, group.Key.VersionSource);
        if (newVersion == null)
        {
            AddWarning(
                manifest,
                $"Skipping .NET {channel} {group.Key.VersionSource} images because the selected release does not contain a supported version.");
            return;
        }

        var newTag = DotnetImageTag.TryMapFromVersion(newVersion.Version);
        if (newTag == null || !DotnetImageTag.TryParse(newTag, out var parsedNewVersion))
        {
            AddWarning(
                manifest,
                $"Skipping .NET {channel} {group.Key.VersionSource} images because version '{newVersion.Version}' cannot be mapped to a supported Docker image tag.");
            return;
        }

        logger.Debug(
            "Bumped .NET {Channel} {VersionSource} images to {Tag} (release {ReleaseVersion})",
            channel,
            group.Key.VersionSource,
            newTag,
            newRelease.ReleaseVersion ?? newVersion.Version);

        // Never downgrade: only update occurrences below the resolved target version.
        foreach (var entry in group.Where(entry => parsedNewVersion > entry.ParsedVersion))
        {
            manifest.SetTag(entry, newTag);
        }
    }

    private void AddWarning(DockerfileManifest manifest, string warning)
    {
        logger.Warning("{Warning}", warning);
        manifest.AddWarning(warning);
    }

    private readonly record struct ImageGroup(
        DotnetImageVersionSource VersionSource,
        int Major,
        int Minor);
}
