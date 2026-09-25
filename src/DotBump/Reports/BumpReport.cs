// Copyright © Roby Van Damme.

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotBump.Commands;
using DotBump.Commands.BumpPackages.DataModel;
using DotBump.Commands.BumpSdk.DataModel;
using DotBump.Commands.BumpTools.DataModel.LocalTools;
using DotBump.Common;

namespace DotBump.Reports;

internal class BumpReport
{
    private readonly List<BumpResult> _results = new();
    private readonly List<string> _errors = new();

    public BumpReport(ToolsManifest toolsManifest, BumpType bumpType)
    {
        CommandName = "tools";
        BumpType = bumpType;
        foreach (var toolManifestEntry in toolsManifest.Tools)
        {
            _results.Add(new BumpResult(toolManifestEntry.Key, toolManifestEntry.Value.Version));
        }
    }

    public BumpReport(Sdk sdk, BumpType bumpType)
    {
        CommandName = "sdk";
        BumpType = bumpType;
        _results.Add(new BumpResult("sdk", sdk.Version));
    }

    public BumpReport(PackageManifest packageManifest, BumpType bumpType)
    {
        ArgumentNullException.ThrowIfNull(packageManifest);

        CommandName = "packages";
        BumpType = bumpType;

        foreach (var packageId in packageManifest.GetPackageIds())
        {
            _results.Add(new BumpResult(packageId, GetReferenceVersion(packageManifest, packageId, useCurrentVersion: false)));
        }
    }

    public string CommandName { get; init; }

    public BumpType BumpType { get; init; }

    public DateTime TimeStamp { get; set; }

    public IReadOnlyCollection<BumpResult> Results => _results;

    public IReadOnlyCollection<string> Errors => _errors;

    [JsonIgnore]
    public bool HasChanges
    {
        get
        {
            return Results.Any(o =>
                o.NewVersion != null && !o.NewVersion.Equals(o.OldVersion, StringComparison.OrdinalIgnoreCase));
        }
    }

    public void ReportChanges(ToolsManifest toolsManifest)
    {
        foreach (var toolManifestEntry in toolsManifest.Tools)
        {
            var reportItem = _results.FirstOrDefault(o => o.Id.Equals(
                toolManifestEntry.Key,
                StringComparison.OrdinalIgnoreCase));
            if (reportItem != null)
            {
                reportItem.NewVersion = toolManifestEntry.Value.Version;
            }
        }

        TimeStamp = DateTime.UtcNow;
    }

    public void ReportChanges(Release sdkRelease)
    {
        _results[0].NewVersion = sdkRelease.LatestSdk;
        TimeStamp = DateTime.UtcNow;
    }

    public void ReportChanges(PackageManifest packageManifest)
    {
        ArgumentNullException.ThrowIfNull(packageManifest);

        foreach (var packageId in packageManifest.GetPackageIds())
        {
            var reportItem = _results.FirstOrDefault(o => o.Id.Equals(
                packageId,
                StringComparison.OrdinalIgnoreCase));
            if (reportItem != null)
            {
                reportItem.NewVersion = GetReferenceVersion(packageManifest, packageId, useCurrentVersion: true);
            }
        }

        TimeStamp = DateTime.UtcNow;
    }

    public void ReportErrors(List<ValidationResult> validationErrors)
    {
        foreach (var error in validationErrors)
        {
            if (error.ErrorMessage != null)
            {
                _errors.Add(error.ErrorMessage);
            }
        }

        TimeStamp = DateTime.UtcNow;
    }

    public void ReportNoSdkVersionChanges()
    {
        _results[0].NewVersion = _results[0].OldVersion;
        TimeStamp = DateTime.UtcNow;
    }

    public async Task WriteToFileAsync(string? outputFile)
    {
        if (!string.IsNullOrWhiteSpace(outputFile))
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            };
            await File.WriteAllTextAsync(
                outputFile,
                JsonSerializer.Serialize(this, options),
                new UTF8Encoding());
        }
    }

    /// <summary>
    /// Gets the reference version for a package: the highest semantic version across all its
    /// occurrences, falling back to the first occurrence when none is a valid semantic version.
    /// </summary>
    private static string GetReferenceVersion(
        PackageManifest packageManifest,
        string packageId,
        bool useCurrentVersion)
    {
        var entries = packageManifest.Packages
            .Where(package => string.Equals(package.PackageId, packageId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var versions = entries
            .Select(entry => new SemanticVersion(useCurrentVersion ? entry.Version : entry.OriginalVersion))
            .Where(version => version.IsValid)
            .ToList();

        if (versions.Count > 0)
        {
            return versions.OrderByDescending(version => version).First().Version;
        }

        return useCurrentVersion ? entries[0].Version : entries[0].OriginalVersion;
    }
}
